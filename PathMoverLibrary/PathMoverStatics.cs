using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PathMover
{
    public class VehicleControlPointPair
    {
        public Vehicle Vehicle { get; set; }
        public ControlPoint ControlPoint { get; set; }
        public VehicleControlPointPair(Vehicle vehicle, ControlPoint controlPoint)
        {
            Vehicle = vehicle;
            ControlPoint = controlPoint;
        }
    }
    public class VehiclePathPair
    {
        public Vehicle Vehicle { get; set; }
        public PmPath Path { get; set; }
        public VehiclePathPair(Vehicle vehicle, PmPath path)
        {
            Vehicle = vehicle;
            Path = path;
        }
    }
    public class PathMoverStatics
    {
        #region Statics
        // (current, target) -> NextHop
        private Dictionary<(string, string), PmPath> _routeTable = new Dictionary<(string, string), PmPath>();
        private Dictionary<string, ControlPoint> _routePoints = new Dictionary<string, ControlPoint>();
        public List<(string, string)> PathList = new List<(string, string)>();
        //private readonly StreamWriter _swPathMoverStatics;
        #endregion

        public PathMoverStatics()
        {
            _routeTable = new Dictionary<(string, string), PmPath>();
            //_swPathMoverStatics = new StreamWriter($"RT.csv");
        }

        public void AddPath(PmPath path)
        {
            AddPath(path.StartPoint.Tag, path.EndPoint.Tag, path);
        }

        public void AddPath(string from, string to, PmPath nextHop)
        {
            // update route table
            if (!_routeTable.ContainsKey((from, to)))
            {
                _routeTable.Add((from, to), nextHop);
            }
            // update control point list
            if (!_routePoints.ContainsKey(from))
            {
                _routePoints.Add(from, nextHop.StartPoint);
            }
            if (!_routePoints.ContainsKey(to))
            {
                _routePoints.Add(to, nextHop.EndPoint);
            }
            //update path list
            if (!PathList.Contains((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag)))
            {
                PathList.Add((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag));
                // Since we need HC_PathMap to record every path, need to add every path into _routeTable
                if (!_routeTable.ContainsKey((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag)))
                {
                    _routeTable.Add((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag), nextHop);
                }
            }
        }

        public PmPath GetPath(string from, string to)
        {
            return _routeTable[(from, to)];
        }

        public ControlPoint GetControlPoint(string tag)
        {
            if (_routePoints.ContainsKey(tag))
            { 
                return _routePoints[tag];
            }
            else
            {
                return null;
            }
        }

        public void PrintRouteTable()
        {
            foreach (var kvp in _routeTable)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}-{1},{2},{3},{4}", kvp.Key.Item1, kvp.Key.Item2, kvp.Value.Length, kvp.Value.NumberOfLane, kvp.Value.RemainingCapacity);
            }
        }
    }
}
