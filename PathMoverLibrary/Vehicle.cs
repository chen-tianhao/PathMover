
using System.Collections.Generic;
using System.Text;

namespace PathMover
{
    public interface IVehicle
    {
        string Name { get; set; }
        PathMoverStatics PathMoverStatics { get; set; }
        double Speed { get; set; }
        int CapacityNeeded { get; set; }
        PmPath CurrentPath { get; set; }
        PmPath? PengingPath { get; set; } // vehicle is waiting in InPengingList of this path.
        bool IsStoped { get; set; }
        List<ControlPoint> TargetList { get; set; }
        void RemoveTarget(ushort controlPointId);
        PmPath NextPath(ushort currentPointId);
    }
    public class Vehicle : IVehicle
    {
        public string Name { get; set; }
        public PathMoverStatics PathMoverStatics { get; set; }
        public double Length { get; set; } = 4;
        public double Speed { get; set; }
        public int CapacityNeeded { get; set; } = 1;
        public PmPath CurrentPath { get; set; }
        public PmPath? PengingPath { get; set; }
        public bool IsStoped { get; set; } = false;
        public List<ControlPoint> TargetList { get; set; }

        public Vehicle(string name)
        {
            Name = name;
        }
        
        public Vehicle(string name, double speed, List<ControlPoint> targetLists, PathMoverStatics pathMoverStatics)
        {
            Name = name;
            Speed = speed;
            TargetList = new List<ControlPoint>(targetLists);
            PathMoverStatics = pathMoverStatics;
        }

        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}] TargetList: ", Name);
            foreach (var cp in TargetList)
            {
                sb.AppendFormat(" -> {0}", cp.Id);
            }
            return sb.ToString();
        }

        public void RemoveTarget(ushort controlPointId)
        {
            if (TargetList.Count > 0 && TargetList[0].Id.Equals(controlPointId))
            { 
                TargetList.RemoveAt(0);
            }
        }
        
        public PmPath NextPath(ushort currentPointId)
        {
            if (TargetList.Count == 0) return null;
            while(TargetList[0].Id.Equals(currentPointId))
            {
                TargetList.RemoveAt(0);
                if (TargetList.Count == 0) return null;
            }
            PmPath nextPath = PathMoverStatics.GetPath(currentPointId, TargetList[0].Id);
            return nextPath;
        }
    }

}
