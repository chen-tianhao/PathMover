using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PathMover
{
    public class VehiclePathPair
    {
        public IVehicle Vehicle { get; set; }
        public PmPath Path { get; set; }
        public VehiclePathPair(IVehicle vehicle, PmPath path)
        {
            Vehicle = vehicle;
            Path = path;
        }
    }
    public class PathMoverStatics
    {
        #region Statics
        // (current, target) -> NextHop
        public Dictionary<(string, string), PmPath> RouteTable { get; private set; } = new Dictionary<(string, string), PmPath>();
        public Dictionary<string, ControlPoint> AllControlPoints { get; private set; } = new Dictionary<string, ControlPoint>();
        public List<PmPath> PathList { get; private set; } = new List<PmPath>();
        //private readonly StreamWriter _swPathMoverStatics;
        #endregion

        public PathMoverStatics()
        {
            RouteTable = new Dictionary<(string, string), PmPath>();
            //_swPathMoverStatics = new StreamWriter($"RT.csv");
        }

        public void AddPath(PmPath path)
        {
            AddPath(path.StartPoint.Tag, path.EndPoint.Tag, path);
        }

        public void AddPath(string from, string to, PmPath nextHop)
        {
            // Console.WriteLine(from + to);
            // update route table
            if (!RouteTable.ContainsKey((from, to)))
            {
                RouteTable.Add((from, to), nextHop);
            }
            // update control point list
            if (!AllControlPoints.ContainsKey(nextHop.StartPoint.Tag))
            {
                AllControlPoints.Add(nextHop.StartPoint.Tag, nextHop.StartPoint);
            }
            if (!AllControlPoints.ContainsKey(nextHop.EndPoint.Tag))
            {
                AllControlPoints.Add(nextHop.EndPoint.Tag, nextHop.EndPoint);
            }
            //update path list // DOTO: need to merge 2 path object with same start/end point
            if (!PathList.Contains((nextHop)))
            {
                PathList.Add((nextHop));
                // Since we need HC_PathMap to record every path, need to add every path into _routeTable
                if (!RouteTable.ContainsKey((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag)))
                {
                    RouteTable.Add((nextHop.StartPoint.Tag, nextHop.EndPoint.Tag), nextHop);
                }
            }
        }

        public PmPath GetPath(string from, string to)
        {
            return RouteTable[(from, to)];
        }

        public bool PathExists(string from, string to)
        {
            return PathList.Any(path => path.StartPoint.Tag == from && path.EndPoint.Tag == to);
        }

        public ControlPoint GetControlPoint(string tag)
        {
            if (AllControlPoints.ContainsKey(tag))
            {
                return AllControlPoints[tag];
            }
            else
            {
                return null;
            }
        }

        public void PrintRouteTable()
        {
            foreach (var kvp in RouteTable)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}-{1},{2},{3},{4}", kvp.Key.Item1, kvp.Key.Item2, kvp.Value.Length, kvp.Value.NumberOfLane, kvp.Value.RemainingCapacity);
            }
        }
    }
}
