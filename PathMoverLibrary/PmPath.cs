
using System;
using System.Collections.Generic;
using O2DESNet;

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
        public List<Vehicle> OutPendingList { get; set; } = new List<Vehicle>(); //P.Q
        public List<(Vehicle, PmPath)> InPendingList { get; set; } = new List<(Vehicle, PmPath)>(); //CP.Q
        public PmPath(ControlPoint start, ControlPoint end, int capacity = 20, double length = 100, int numberOfLane = 1)
        {
            StartPoint = start;
            EndPoint = end;
            TotalCapacity = capacity;
            RemainingCapacity = capacity;
            Length = length;
            NumberOfLane = numberOfLane;
        }
        /*
        public bool operator ==(PmPath p1, PmPath p2)
        {
            if (p1.StartPoint == p2.StartPoint && p1.EndPoint == p2.EndPoint)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool operator !=(PmPath p1, PmPath p2)
        {
            if (p1.StartPoint == p2.StartPoint && p1.EndPoint == p2.EndPoint)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        */
    }

}