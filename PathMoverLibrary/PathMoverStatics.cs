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
        public Dictionary<(ushort, ushort), PmPath> RouteTable { get; private set; } = new Dictionary<(ushort, ushort), PmPath>();
        public Dictionary<ushort, ControlPoint> AllControlPoints { get; private set; } = new Dictionary<ushort, ControlPoint>();
        public List<PmPath> PathList { get; private set; } = new List<PmPath>();
        public IdMapper IdMapper { get; private set; } = new IdMapper();
        //private readonly StreamWriter _swPathMoverStatics;
        #endregion

        public PathMoverStatics()
        {
            RouteTable = new Dictionary<(ushort, ushort), PmPath>();
            //_swPathMoverStatics = new StreamWriter($"RT.csv");
        }

        public void AddPath(PmPath path)
        {
            AddPath(path.StartPoint.Id, path.EndPoint.Id, path);
        }

        public void AddPath(ushort from, ushort to, PmPath nextHop)
        {
            // Console.WriteLine(from + to);
            // update route table
            if (!RouteTable.ContainsKey((from, to)))
            {
                RouteTable.Add((from, to), nextHop);
            }
            // update control point list
            if (!AllControlPoints.ContainsKey(nextHop.StartPoint.Id))
            {
                AllControlPoints.Add(nextHop.StartPoint.Id, nextHop.StartPoint);
            }
            if (!AllControlPoints.ContainsKey(nextHop.EndPoint.Id))
            {
                AllControlPoints.Add(nextHop.EndPoint.Id, nextHop.EndPoint);
            }
            //update path list // DOTO: need to merge 2 path object with same start/end point
            if (!PathList.Contains((nextHop)))
            {
                PathList.Add((nextHop));
                // Since we need HC_PathMap to record every path, need to add every path into _routeTable
                if (!RouteTable.ContainsKey((nextHop.StartPoint.Id, nextHop.EndPoint.Id)))
                {
                    RouteTable.Add((nextHop.StartPoint.Id, nextHop.EndPoint.Id), nextHop);
                }
            }
        }

        public PmPath GetPath(ushort from, ushort to)
        {
            return RouteTable[(from, to)];
        }

        public bool PathExists(ushort from, ushort to)
        {
            return PathList.Any(path => path.StartPoint.Id == from && path.EndPoint.Id == to);
        }

        public ControlPoint GetControlPoint(ushort id)
        {
            if (AllControlPoints.ContainsKey(id))
            {
                return AllControlPoints[id];
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
