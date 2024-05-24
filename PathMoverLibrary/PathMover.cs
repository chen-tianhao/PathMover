using System;
using System.Collections.Generic;
using System.IO;
using O2DESNet;

namespace PathMover
{
    public class PathMover : Sandbox
    {
        #region Statics
        private PathMoverStatics _pathMoverStatics;
        private double _smoothFactor;
        private double _coldStartDelay;
        #endregion

        #region Dynamics
        public Dictionary<ControlPoint, List<IVehicle>> VehiclePendingToEnterMapByCP { get; private set; } = new Dictionary<ControlPoint, List<IVehicle>>();
        public List<VehiclePathPair> _readyToExitList { get; private set; } = new List<VehiclePathPair>();
        public Dictionary<(string, string), double> MaxAgvInPathMap { get; private set; }
        public Dictionary<PmPath, HourCounter> HC_PathOccupied { get; private set; }
        public Dictionary<PmPath, HourCounter> HC_PathInPending { get; private set; }
        public Dictionary<ControlPoint, HourCounter> HC_PendingListByCP { get; private set; }
        #endregion

        public PathMover(PathMoverStatics pathMoverStatics, double smoothFactor, double coldStartDelay)
        {
            _smoothFactor = smoothFactor;
            _coldStartDelay = coldStartDelay;
            MaxAgvInPathMap = new Dictionary<(string, string), double>();
            HC_PathOccupied = new Dictionary<PmPath, HourCounter>();
            HC_PathInPending = new Dictionary<PmPath, HourCounter>();
            HC_PendingListByCP = new Dictionary<ControlPoint, HourCounter>();
            _pathMoverStatics = pathMoverStatics;
            foreach (PmPath path in pathMoverStatics.PathList)
            {
                HC_PathOccupied.Add(path, AddHourCounter());

                MaxAgvInPathMap.Add((path.StartPoint.Tag, path.EndPoint.Tag), 0d);

                HC_PathInPending.Add(pathMoverStatics.GetPath(path.StartPoint.Tag, path.EndPoint.Tag), AddHourCounter());

            }
            /*          foreach (var entry in HC_PathPendingMap)
                        {
                            Console.WriteLine($"from: {entry.Key.StartPoint.Tag}, Last Count: {entry.Key.EndPoint.Tag}");
                        }*/
        }

        #region Input Events
        public void RequestToEnter(IVehicle vehicle, ControlPoint cp)
        {
            bool sameInSameOut = true;
            foreach (ControlPoint targetCp in vehicle.TargetList)
            {
                if (!targetCp.Tag.Equals(cp.Tag))
                {
                    sameInSameOut = false;
                }
            }
            if (sameInSameOut)
            {
                OnReadyToExit.Invoke(vehicle, cp);
                return;
            }

            if (!VehiclePendingToEnterMapByCP.ContainsKey(cp))
            {
                VehiclePendingToEnterMapByCP.Add(cp, new List<IVehicle>());
                HC_PendingListByCP.Add(cp, AddHourCounter());
            }
            VehiclePendingToEnterMapByCP[cp].Add(vehicle);
            HC_PendingListByCP[cp].ObserveChange(1); // 增加等待进入的车辆计数
            Schedule(() => AttemptToEnter(cp), TimeSpan.FromMilliseconds(1));
        }

        public event Action<IVehicle, ControlPoint> OnEnter = (v, cp) => { };
        #endregion

        #region Internal Events
        void AttemptToEnter(ControlPoint cp)
        {
            if (!VehiclePendingToEnterMapByCP.ContainsKey(cp) || VehiclePendingToEnterMapByCP[cp].Count == 0) return;
            foreach (IVehicle vehicle in VehiclePendingToEnterMapByCP[cp])
            {
                ControlPoint controlPoint = cp;
                PmPath nextPath = vehicle.NextPath(controlPoint.Tag);
                if (nextPath != null)
                {
                    double deltaTime = Math.Round(ClockTime.Subtract(nextPath.EnterTimeStamp).TotalSeconds, 6);
                    if (nextPath.RemainingCapacity >= vehicle.CapacityNeeded)
                    {
                        if (deltaTime < _smoothFactor)
                        {
                            Schedule(() => AttemptToEnter(cp), TimeSpan.FromSeconds(_smoothFactor - deltaTime));
                        }
                        else
                        {
                            Enter(vehicle, nextPath, cp);
                        }
                        break;
                    }
                }
                else
                {
                    // TODO
                    //path.OutPendingList.Remove(vehicle);
                    ////Schedule(() => ReadyToExit(vehicle, path), TimeSpan.FromMilliseconds(1));
                    //ReadyToExit(vehicle, path);
                }
            }
        }
        void Enter(IVehicle vehicle, PmPath path, ControlPoint controlPoint)
        {
            path.EnterTimeStamp = ClockTime;
            OnEnter.Invoke(vehicle, controlPoint);
            VehiclePendingToEnterMapByCP[controlPoint].Remove(vehicle);
            HC_PendingListByCP[controlPoint].ObserveChange(-1);
            vehicle.IsStoped = true;
            Arrive(vehicle, path);
        }
        public event Action<IVehicle, PmPath> OnArrive = (v, cp) => { };
        void Arrive(IVehicle vehicle, PmPath path)
        {
            OnArrive.Invoke(vehicle, path);
            vehicle.CurrentPath = path;
            vehicle.RemoveTarget(vehicle.CurrentPath.StartPoint.Tag);
            path.RemainingCapacity -= vehicle.CapacityNeeded;
            HC_PathOccupied[path].ObserveChange(vehicle.CapacityNeeded);
            if (HC_PathOccupied[path].LastCount > MaxAgvInPathMap[(path.StartPoint.Tag, path.EndPoint.Tag)])
            {
                MaxAgvInPathMap[(path.StartPoint.Tag, path.EndPoint.Tag)] = HC_PathOccupied[path].LastCount;
            }
            double timeDelay = path.Length / vehicle.Speed;
            if (vehicle.IsStoped == true)
            {
                vehicle.IsStoped = false;
                timeDelay += _coldStartDelay;
            }
            Schedule(() => Complete(vehicle, path), TimeSpan.FromSeconds(timeDelay));
        }
        public event Action<IVehicle, PmPath> OnComplete = (v, cp) => { };
        void Complete(IVehicle vehicle, PmPath path)
        {
            try
            {
                path.OutPendingList.Add(vehicle);
                OnComplete.Invoke(vehicle, path);
                Schedule(() => AttemptToDepart(path), TimeSpan.FromMilliseconds(1));
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志文件
                Console.WriteLine("error_log.txt", $"Error in Complete: {ex.Message} at {DateTime.Now}\n");
                // 可选：重新抛出异常或进行错误处理
                throw;
            }

        }
        void AttemptToDepart(PmPath path, IVehicle? vehicle = null)
        {
            try
            {
                if (path.OutPendingList.Count == 0) //没有车需要从此路段出去
                {
                    return;
                }
                if (vehicle == null)
                {
                    vehicle = path.OutPendingList[0];
                }
                else if (!path.OutPendingList.Contains(vehicle)) //这辆车不在这段路上
                {
                    return;
                }
                if (path.IsCongestion)
                {
                    vehicle.IsStoped = true;
                }
                else
                {
                    vehicle.IsStoped = false;
                }
                PmPath nextPath = vehicle.NextPath(path.EndPoint.Tag);
                if (nextPath != null) //需要进入下一段路
                {
                    double deltaTime = Math.Round(ClockTime.Subtract(nextPath.DepartTimeStamp).TotalSeconds, 6);
                    if (nextPath.RemainingCapacity >= vehicle.CapacityNeeded)
                    {
                        if (deltaTime < _smoothFactor)
                        {
                            path.IsCongestion = true;
                            Schedule(() => AttemptToDepart(path, vehicle), TimeSpan.FromSeconds(_smoothFactor - deltaTime));
                        }
                        else
                        {
                            path.IsCongestion = false;
                            path.OutPendingList.Remove(vehicle);
                            //Schedule(() => Depart(vehicle, path), TimeSpan.FromMilliseconds(1)); //还没有离开当前路段，所以仍传递当前路段
                            Depart(vehicle, path);
                            nextPath.DepartTimeStamp = ClockTime;
                        }
                    }
                    else //下一段路还在堵
                    {

                        nextPath.InPendingList.Add((vehicle, path));
                        foreach (var entry in HC_PathInPending)
                        {
                            // Console.WriteLine($"from: {entry.Key.StartPoint.Tag}, to: {entry.Key.EndPoint.Tag}");
                        }
                        // Console.WriteLine($"from: {nextPath.StartPoint.Tag},to: {nextPath.EndPoint.Tag}");
                        HC_PathInPending[nextPath].ObserveChange(vehicle.CapacityNeeded);
                    }
                }
                else //车已到达目的地
                {
                    path.OutPendingList.Remove(vehicle);
                    //Schedule(() => ReadyToExit(vehicle, path), TimeSpan.FromMilliseconds(1));
                    ReadyToExit(vehicle, path);
                }
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志文件
                Console.WriteLine($"Error in AttemptToDepart: {ex.Message} at {DateTime.Now}");
                // 可选：重新抛出异常或进行错误处理
                throw;
            }

        }
        public event Action<IVehicle, PmPath> OnDepart = (v, cp) => { };
        void Depart(IVehicle vehicle, PmPath path) // vehicle depart from path
        {
            OnDepart.Invoke(vehicle, path);
            path.RemainingCapacity += vehicle.CapacityNeeded;
            HC_PathOccupied[path].ObserveChange(-1 * vehicle.CapacityNeeded);
            PmPath nextPath = vehicle.NextPath(path.EndPoint.Tag);
            if (nextPath != null && nextPath.RemainingCapacity >= vehicle.CapacityNeeded) //this condition is sure to satissfy, because already check in AttemptToDepart
            {
                //Schedule(() => Arrive(vehicle, nextPath), TimeSpan.FromMilliseconds(1));
                Arrive(vehicle, nextPath);
                // 如果当前路段有闲置容量了，那么尝试从上游路段中放车进来
                if (path.InPendingList.Count > 0)
                {
                    PmPath formerPath = path.InPendingList[0].Item2;
                    Schedule(() => AttemptToDepart(formerPath, path.InPendingList[0].Item1), TimeSpan.FromMilliseconds(1));
                    path.InPendingList.RemoveAt(0);
                    HC_PathInPending[path].ObserveChange(-1 * vehicle.CapacityNeeded);
                }
                Schedule(() => AttemptToEnter(path.StartPoint), TimeSpan.FromMilliseconds(1));
            }
        }
        void ReadyToExit(IVehicle vehicle, PmPath path)
        {
            _readyToExitList.Add(new VehiclePathPair(vehicle, path));
            OnReadyToExit.Invoke(vehicle, path.EndPoint);
        }
        #endregion

        #region Output Events
        public void Exit(IVehicle vehicle, ControlPoint cp)
        {
            foreach (VehiclePathPair pair in _readyToExitList)
            {
                if (pair.Vehicle == vehicle && pair.Path.EndPoint.Tag.Equals(cp.Tag))
                {
                    vehicle.CurrentPath.RemainingCapacity += vehicle.CapacityNeeded;
                    HC_PathOccupied[vehicle.CurrentPath].ObserveChange(-1 * vehicle.CapacityNeeded);
                    if (pair.Path.InPendingList.Count > 0)
                    {
                        PmPath path = pair.Path.InPendingList[0].Item2;
                        Schedule(() => AttemptToDepart(path, pair.Path.InPendingList[0].Item1), TimeSpan.FromMilliseconds(1));
                        pair.Path.InPendingList.RemoveAt(0);
                        HC_PathInPending[pair.Path].ObserveChange(-1 * vehicle.CapacityNeeded);
                    }
                    Schedule(() => AttemptToEnter(pair.Path.StartPoint), TimeSpan.FromMilliseconds(1));
                    _readyToExitList.Remove(pair);
                    break;
                }
            }
            return;
        }

        public event Action<IVehicle, ControlPoint> OnReadyToExit = (v, cp) => { };
        #endregion

        #region Common Functions
        #endregion
    }
}
