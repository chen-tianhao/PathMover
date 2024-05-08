using System;
using System.Collections.Generic;

namespace PathMover
{
    public class PmPath
    {
        public ControlPoint StartPoint { get; set; }
        public ControlPoint EndPoint { get; set; }
        public int TotalCapacity { get; set; }
        public int RemainingCapacity { get; set; }
        public double Length { get; set; }
        public int NumberOfLane { get; set; }
        public bool IsCongestion { get; set; } = false;
        public DateTime DepartTimeStamp { get; set; }
        public DateTime EnterTimeStamp { get; set; }
        public List<IVehicle> OutPendingList { get; set; } = new List<IVehicle>(); //P.Q
        public List<(IVehicle, PmPath)> InPendingList { get; set; } = new List<(IVehicle, PmPath)>(); //CP.Q

        public PmPath(ControlPoint start, ControlPoint end, int capacity = 20, double length = 100, int numberOfLane = 1)
        {
            StartPoint = start;
            EndPoint = end;
            TotalCapacity = capacity;
            RemainingCapacity = capacity;
            Length = length;
            NumberOfLane = numberOfLane;
        }

        public static bool operator ==(PmPath p1, PmPath p2)
        {
            if (ReferenceEquals(p1, p2))
            {
                return true;
            }
            if (ReferenceEquals(p1, null) || ReferenceEquals(p2, null))
            {
                return false;
            }
            return p1.StartPoint == p2.StartPoint && p1.EndPoint == p2.EndPoint;
        }

        public static bool operator !=(PmPath p1, PmPath p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            var path = obj as PmPath;
            return path != null && StartPoint == path.StartPoint && EndPoint == path.EndPoint;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartPoint, EndPoint);
        }
    }
}
