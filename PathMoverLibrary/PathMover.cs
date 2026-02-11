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
        public Dictionary<(ushort, ushort), double> MaxAgvInPathMap { get; private set; }
        public Dictionary<PmPath, HourCounter> HC_PathOccupied { get; private set; }
        public Dictionary<PmPath, HourCounter> HC_PathInPending { get; private set; }
        public Dictionary<ControlPoint, HourCounter> HC_PendingListByCP { get; private set; }
        #endregion

        public PathMover(PathMoverStatics pathMoverStatics, double smoothFactor, double coldStartDelay)
        {
            _smoothFactor = Math.Round(smoothFactor, 6);
            _coldStartDelay = Math.Round(coldStartDelay, 6);
            MaxAgvInPathMap = new Dictionary<(ushort, ushort), double>();
            HC_PathOccupied = new Dictionary<PmPath, HourCounter>();
            HC_PathInPending = new Dictionary<PmPath, HourCounter>();
            HC_PendingListByCP = new Dictionary<ControlPoint, HourCounter>();
            _pathMoverStatics = pathMoverStatics;
            foreach (PmPath path in pathMoverStatics.PathList)
            {
                HC_PathOccupied.Add(path, AddHourCounter());

                MaxAgvInPathMap.Add((path.StartPoint.Id, path.EndPoint.Id), 0d);

                HC_PathInPending.Add(pathMoverStatics.GetPath(path.StartPoint.Id, path.EndPoint.Id), AddHourCounter());

            }
            /*          foreach (var entry in HC_PathPendingMap)
                        {
                            Console.WriteLine($"from: {entry.Key.StartPoint.Id}, Last Count: {entry.Key.EndPoint.Id}");
                        }*/
        }

        #region Input Events
        public void RequestToEnter(IVehicle vehicle, ControlPoint cp)
        {
            bool sameInSameOut = true;
            foreach (ControlPoint targetCp in vehicle.TargetList)
            {
                if (!targetCp.Id.Equals(cp.Id))
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
                PmPath nextPath = vehicle.NextPath(controlPoint.Id);
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
            vehicle.RemoveTarget(vehicle.CurrentPath.StartPoint.Id);
            path.RemainingCapacity -= vehicle.CapacityNeeded;
            HC_PathOccupied[path].ObserveChange(vehicle.CapacityNeeded);
            if (HC_PathOccupied[path].LastCount > MaxAgvInPathMap[(path.StartPoint.Id, path.EndPoint.Id)])
            {
                MaxAgvInPathMap[(path.StartPoint.Id, path.EndPoint.Id)] = HC_PathOccupied[path].LastCount;
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
                PmPath nextPath = vehicle.NextPath(path.EndPoint.Id);
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
                            if (path.OutPendingList.Count > 0)
                            {
                                var nextPendingPath = path.OutPendingList[0].NextPath(path.EndPoint.Id); // 假设改用 GetNextPath 方法以避免与现有的 nextPath 冲突
                                if (nextPendingPath != null)
                                {
                                    nextPendingPath.InPendingList.Add((path.OutPendingList[0], path));
                                    HC_PathInPending[nextPendingPath].ObserveChange(vehicle.CapacityNeeded);
                                }
                            }


                            if (vehicle.PengingPath != null && vehicle != null)
                            {
                                vehicle.PengingPath.InPendingList.RemoveAt(0);
                                HC_PathInPending[vehicle.PengingPath].ObserveChange(-1 * vehicle.CapacityNeeded);
                                vehicle.PengingPath = null;
                               
                            }
                            //Schedule(() => Depart(vehicle, path), TimeSpan.FromMilliseconds(1)); //还没有离开当前路段，所以仍传递当前路段
                            Depart(vehicle, path);
                            nextPath.DepartTimeStamp = ClockTime;
                        }
                    }
                    else // 下一段路还在堵
                    {
                        // 这里的本意是考虑到车辆顺序（不能超车）的因素，只有OutPendingList[0]才能被放进对应的InPendingList。
                        // 如果是 Complete 触发了 AttemptToDepart，那么path.OutPendingList.Count == 1和“当前车辆是第一辆车”是等价的。
                        // 如果是 Depart 或者 Exit 触发了 AttemptToDepart，那么调用时必然已经带了vehicle参数，且该参数是从对应的InPendingList中拿出来的，所以就不应该再加入一遍了。
                        if (path.OutPendingList.Count == 1 ) 
                        {
                            nextPath.InPendingList.Add((vehicle, path));
                            HC_PathInPending[nextPath].ObserveChange(vehicle.CapacityNeeded);
                            vehicle.PengingPath = nextPath;
                        }
                       
                        foreach (var entry in HC_PathInPending)
                        {
                            // Console.WriteLine($"from: {entry.Key.StartPoint.Id}, to: {entry.Key.EndPoint.Id}");
                        }
                        // Console.WriteLine($"from: {nextPath.StartPoint.Id},to: {nextPath.EndPoint.Id}");
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
            PmPath nextPath = vehicle.NextPath(path.EndPoint.Id);
            //Schedule(() => Arrive(vehicle, nextPath), TimeSpan.FromMilliseconds(1));
            Arrive(vehicle, nextPath);
            // 如果当前路段有闲置容量了，那么尝试从上游路段中放车进来
            Schedule(() => AttemptToDepart(path), TimeSpan.FromMilliseconds(1));
            if (path.InPendingList.Count > 0)
            {
                PmPath formerPath = path.InPendingList[0].Item2;
                IVehicle v = path.InPendingList[0].Item1;
                Schedule(() => AttemptToDepart(formerPath, v), TimeSpan.FromMilliseconds(1));
                
                //path.InPendingList.RemoveAt(0);
                //HC_PathInPending[path].ObserveChange(-1 * vehicle.CapacityNeeded);
            }
            Schedule(() => AttemptToEnter(path.StartPoint), TimeSpan.FromMilliseconds(1));
            
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
                if (pair.Vehicle == vehicle && pair.Path.EndPoint.Id.Equals(cp.Id))
                {
                    vehicle.CurrentPath.RemainingCapacity += vehicle.CapacityNeeded;
                    HC_PathOccupied[vehicle.CurrentPath].ObserveChange(-1 * vehicle.CapacityNeeded);
                    if (pair.Path.InPendingList.Count > 0)
                    {
                        PmPath path = pair.Path.InPendingList[0].Item2;
                        IVehicle v = pair.Path.InPendingList[0].Item1;
                        Schedule(() => AttemptToDepart(path, v), TimeSpan.FromMilliseconds(1));
                        //pair.Path.InPendingList.RemoveAt(0);
                        //HC_PathInPending[pair.Path].ObserveChange(-1 * vehicle.CapacityNeeded);
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
