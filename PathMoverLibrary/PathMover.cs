
using System;
using System.Collections.Generic;
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
        private Dictionary<ControlPoint, List<Vehicle>> _pendingListByCP = new Dictionary<ControlPoint, List<Vehicle>>();
        private List<VehiclePathPair> _readyToExitList = new List<VehiclePathPair>();
        public Dictionary<(string,string), HourCounter> HC_PathMap { get; set; }
        public Dictionary<(string, string), double> MaxAgvInPathMap { get; set; }
        public Dictionary<PmPath, HourCounter> HC_PathPendingMap { get; set; }
        #endregion

        public PathMover(PathMoverStatics pathMoverStatics, double smoothFactor, double coldStartDelay)
        {
            _smoothFactor = smoothFactor;
            _coldStartDelay = coldStartDelay;
            HC_PathMap = new Dictionary<(string,string), HourCounter>();
            MaxAgvInPathMap = new Dictionary<(string, string), double>();
            HC_PathPendingMap = new Dictionary<PmPath, HourCounter>();
            _pathMoverStatics = pathMoverStatics;
            foreach ((string,string) path in pathMoverStatics.PathList)
            {
                HC_PathMap.Add((path.Item1, path.Item2), AddHourCounter());
                MaxAgvInPathMap.Add((path.Item1, path.Item2), 0d);
                HC_PathPendingMap.Add(pathMoverStatics.GetPath(path.Item1, path.Item2), AddHourCounter());
            }
        }

        #region Input Events
        public void RequestToEnter(Vehicle vehicle, ControlPoint cp)
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

            if (!_pendingListByCP.ContainsKey(cp))
            {
                _pendingListByCP.Add(cp, new List<Vehicle>());
            }
            _pendingListByCP[cp].Add(vehicle);
            Schedule(() => AttemptToEnter(cp), TimeSpan.FromMilliseconds(1));
        }

        public event Action<Vehicle, ControlPoint> OnEnter = (v, cp) => { };
        #endregion

        #region Internal Events
        void AttemptToEnter(ControlPoint cp)
        {
            if (!_pendingListByCP.ContainsKey(cp) || _pendingListByCP[cp].Count == 0) return;
            foreach (Vehicle vehicle in _pendingListByCP[cp])
            {
                ControlPoint controlPoint = cp;
                PmPath nextPath = vehicle.NextPath(controlPoint.Tag);
                if (nextPath != null)
                {
                    double deltaTime = ClockTime.Subtract(nextPath.EnterTimeStamp).Seconds;
                    if (nextPath.RemainingCapacity >= vehicle.CapacityNeeded)
                    {
                        if (deltaTime >= _smoothFactor)
                        {
                            Enter(vehicle, nextPath, cp);
                        }
                        else
                        {
                            Schedule(() => AttemptToEnter(cp), TimeSpan.FromSeconds(_smoothFactor - deltaTime));
                        }
                        break;
                    }
                    else
                    {
                        // Console.WriteLine(_pathMoverStatics.PrintRouteTable());
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
        void Enter(Vehicle vehicle, PmPath path, ControlPoint controlPoint)
        {
            path.EnterTimeStamp = ClockTime;
            OnEnter.Invoke(vehicle, controlPoint);
            _pendingListByCP[controlPoint].Remove(vehicle);
            vehicle.IsStoped = true;
            Arrive(vehicle, path);
        }
        void Arrive(Vehicle vehicle, PmPath path)
        {
            vehicle.CurrentPath = path;
            vehicle.RemoveTarget(vehicle.CurrentPath.StartPoint.Tag);
            path.RemainingCapacity -= vehicle.CapacityNeeded;
            HC_PathMap[(path.StartPoint.Tag, path.EndPoint.Tag)].ObserveChange(vehicle.CapacityNeeded);
            if (HC_PathMap[(path.StartPoint.Tag, path.EndPoint.Tag)].LastCount > MaxAgvInPathMap[(path.StartPoint.Tag, path.EndPoint.Tag)])
            {
                MaxAgvInPathMap[(path.StartPoint.Tag, path.EndPoint.Tag)] = HC_PathMap[(path.StartPoint.Tag, path.EndPoint.Tag)].LastCount;
            }
            double timeDelay = path.Length / vehicle.Speed;
            if (vehicle.IsStoped == true)
            {
                vehicle.IsStoped = false;
                timeDelay += _coldStartDelay;
            }
            Schedule(() => Complete(vehicle, path), TimeSpan.FromSeconds(timeDelay));
        }
        void Complete(Vehicle vehicle, PmPath path)
        {
            path.OutPendingList.Add(vehicle);
            Schedule(() => AttemptToDepart(path), TimeSpan.FromMilliseconds(1));
        }
        void AttemptToDepart(PmPath path)
        {
            if (path.OutPendingList.Count == 0) //没有车需要从此路段出去
            {
                return;
            }
            Vehicle vehicle = path.OutPendingList[0];
            if (path.IsCongestion)
            {
                vehicle.IsStoped = true;
            }
            else
            {
                vehicle.IsStoped = false;
            }
            PmPath nextPath = vehicle.NextPath(path.EndPoint.Tag);
            if(nextPath != null) //需要进入下一段路
            {
                double deltaTime = ClockTime.Subtract(nextPath.DepartTimeStamp).Seconds;
                if (nextPath.RemainingCapacity >= vehicle.CapacityNeeded)
                {
                    if (deltaTime >= _smoothFactor)
                    {
                        path.IsCongestion = false;
                        path.OutPendingList.Remove(vehicle);
                        //Schedule(() => Depart(vehicle, path), TimeSpan.FromMilliseconds(1)); //还没有离开当前路段，所以仍传递当前路段
                        Depart(vehicle, path);
                        nextPath.DepartTimeStamp = ClockTime;
                    }
                    else
                    {
                        path.IsCongestion = true;
                        Schedule(() => AttemptToDepart(path), TimeSpan.FromSeconds(_smoothFactor - deltaTime));
                    }
                }
                else //下一段路还在堵
                {
                    nextPath.InPendingList.Add((vehicle, path));
                    HC_PathPendingMap[nextPath].ObserveChange(vehicle.CapacityNeeded);
                }
            }
            else //车已到达目的地
            {
                path.OutPendingList.Remove(vehicle);
                //Schedule(() => ReadyToExit(vehicle, path), TimeSpan.FromMilliseconds(1));
                ReadyToExit(vehicle, path);
            }
        }
        void Depart(Vehicle vehicle, PmPath path) // vehicle depart from path
        {
            path.RemainingCapacity += vehicle.CapacityNeeded;
            HC_PathMap[(path.StartPoint.Tag, path.EndPoint.Tag)].ObserveChange(-1 * vehicle.CapacityNeeded);
            PmPath nextPath = vehicle.NextPath(path.EndPoint.Tag);
            if (nextPath != null && nextPath.RemainingCapacity >= vehicle.CapacityNeeded) //this condition is sure to satissfy, because already check in AttemptToDepart
            {
                //Schedule(() => Arrive(vehicle, nextPath), TimeSpan.FromMilliseconds(1));
                Arrive(vehicle, nextPath);
                // 如果当前路段有闲置容量了，那么尝试从上游路段中放车进来
                if (path.InPendingList.Count > 0)
                {
                    PmPath formerPath = path.InPendingList[0].Item2;
                    Schedule(() => AttemptToDepart(formerPath), TimeSpan.FromMilliseconds(1));
                    path.InPendingList.RemoveAt(0);
                    HC_PathPendingMap[path].ObserveChange(-1 * vehicle.CapacityNeeded);
                }
                Schedule(() => AttemptToEnter(path.StartPoint), TimeSpan.FromMilliseconds(1));
            }
        }
        void ReadyToExit(Vehicle vehicle, PmPath path)
        {
            _readyToExitList.Add(new VehiclePathPair(vehicle, path));
            OnReadyToExit.Invoke(vehicle, path.EndPoint);
        }
        #endregion

        #region Output Events
        public void Exit(Vehicle vehicle, ControlPoint cp)
        {
            foreach(VehiclePathPair pair in _readyToExitList)
            {
                if (pair.Vehicle == vehicle && pair.Path.EndPoint.Tag.Equals(cp.Tag))
                {
                    vehicle.CurrentPath.RemainingCapacity += vehicle.CapacityNeeded;
                    HC_PathMap[(vehicle.CurrentPath.StartPoint.Tag, vehicle.CurrentPath.EndPoint.Tag)].ObserveChange(-1 * vehicle.CapacityNeeded);
                    if (pair.Path.InPendingList.Count > 0)
                    {
                        PmPath path = pair.Path.InPendingList[0].Item2;
                        Schedule(() => AttemptToDepart(path), TimeSpan.FromMilliseconds(1));
                        pair.Path.InPendingList.RemoveAt(0);
                        HC_PathPendingMap[pair.Path].ObserveChange(-1 * vehicle.CapacityNeeded);
                    }
                    Schedule(() => AttemptToEnter(pair.Path.StartPoint), TimeSpan.FromMilliseconds(1));
                    _readyToExitList.Remove(pair);
                    break;
                }
            }
            return;
        }

        public event Action<Vehicle, ControlPoint> OnReadyToExit = (v, cp) => { };
        #endregion

        #region Common Functions
        #endregion
    }
}
