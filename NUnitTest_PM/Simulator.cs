using System;
using System.Collections.Generic;
using O2DESNet;
using PathMover;

namespace NUnitTest_PM
{
    public class Simulator : Sandbox
    {
        #region Statics
        PathMoverStatics PathMoverStatics { get; set; }
        #endregion

        #region Dynamic
        PathMover.PathMover PathMover { get; set; }
        private string _Result4UnitTest;
        public string Result4UnitTest { get { return _Result4UnitTest; } }
        #endregion
        public Simulator()
        {
            PathMoverStatics = new PathMoverStatics();

            //准备路由表
            Dictionary<(ControlPoint, ControlPoint), ControlPoint> routeTable = new Dictionary<(ControlPoint, ControlPoint), ControlPoint>();
            ControlPoint A = new ControlPoint(0);
            ControlPoint B = new ControlPoint(1);
            ControlPoint C = new ControlPoint(2);
            ControlPoint D = new ControlPoint(3);
            ControlPoint E = new ControlPoint(4);
            ControlPoint F = new ControlPoint(5);
            
            // Register names for debugging
            PathMoverStatics.IdMapper.Register(0, "A");
            PathMoverStatics.IdMapper.Register(1, "B");
            PathMoverStatics.IdMapper.Register(2, "C");
            PathMoverStatics.IdMapper.Register(3, "D");
            PathMoverStatics.IdMapper.Register(4, "E");
            PathMoverStatics.IdMapper.Register(5, "F");
            
            PathMoverStatics.AddPath(A.Id, F.Id, new PmPath(A, B));
            PathMoverStatics.AddPath(B.Id, F.Id, new PmPath(B, C));
            PathMoverStatics.AddPath(C.Id, F.Id, new PmPath(C, F));

            PathMoverStatics.AddPath(A.Id, E.Id, new PmPath(A, D));
            PathMoverStatics.AddPath(D.Id, E.Id, new PmPath(D, E));

            PathMoverStatics.AddPath(E.Id, F.Id, new PmPath(E, D));
            PathMoverStatics.AddPath(D.Id, F.Id, new PmPath(D, C));

            PathMoverStatics.AddPath(E.Id, C.Id, new PmPath(E, D));
            PathMoverStatics.AddPath(D.Id, C.Id, new PmPath(D, C));

            PathMoverStatics.AddPath(B.Id, C.Id, new PmPath(B, C));

            //创建车辆(及目的地序列)
            double agvSpeed = 1d;
            List<ControlPoint> targetList1 = new List<ControlPoint>();
            targetList1.Add(A);
            targetList1.Add(E);
            targetList1.Add(F); 
            Vehicle v1 = new Vehicle("AGV-1", agvSpeed, targetList1, PathMoverStatics);
            Console.WriteLine(v1.ToString());

            List<ControlPoint> targetList2 = new List<ControlPoint>();
            targetList2.Add(C);
            targetList2.Add(F);
            Vehicle v2 = new Vehicle("AGV-2", agvSpeed, targetList2, PathMoverStatics);
            Console.WriteLine(v2.ToString());

            List<ControlPoint> targetList3 = new List<ControlPoint>();
            targetList3.Add(D);
            targetList3.Add(C);
            targetList3.Add(F);
            Vehicle v3 = new Vehicle("AGV-3", agvSpeed, targetList3, PathMoverStatics);
            Console.WriteLine(v3.ToString());
            Vehicle v3_1 = new Vehicle("AGV-4", agvSpeed, new List<ControlPoint>(targetList3), PathMoverStatics);
            Console.WriteLine(v3_1.ToString());

            List<ControlPoint> targetList4 = new List<ControlPoint>();
            targetList4.Add(E);
            targetList4.Add(C);
            targetList4.Add(F);
            Vehicle v4 = new Vehicle("AGV-5", agvSpeed, targetList4, PathMoverStatics);
            Console.WriteLine(v4.ToString());

            //创建PM
            PathMover = AddChild(new PathMover.PathMover(PathMoverStatics, 0, 0));
            PathMover.RequestToEnter(v1, A);
            PathMover.RequestToEnter(v2, B);
            PathMover.RequestToEnter(v3, D);
            PathMover.RequestToEnter(v3_1, D);
            PathMover.RequestToEnter(v4, E);
            PathMover.OnReadyToExit += ShowExit;
            OnThen += PathMover.Exit;
        }

        void ShowExit(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"{v.Name} exit from: {cpName}\n");
            _Result4UnitTest = $"{v.Name} exit from: {cpName}";
            OnThen.Invoke(v, cp);
        }

        public event Action<IVehicle, ControlPoint> OnThen = (v, cp) => { };
    }

}
