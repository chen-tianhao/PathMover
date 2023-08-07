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
            ControlPoint A = new ControlPoint("A");
            ControlPoint B = new ControlPoint("B");
            ControlPoint C = new ControlPoint("C");
            ControlPoint D = new ControlPoint("D");
            ControlPoint E = new ControlPoint("E");
            ControlPoint F = new ControlPoint("F");
            PathMoverStatics.AddPath(A.Tag, F.Tag, new PmPath(A, B));
            PathMoverStatics.AddPath(B.Tag, F.Tag, new PmPath(B, C));
            PathMoverStatics.AddPath(C.Tag, F.Tag, new PmPath(C, F));

            PathMoverStatics.AddPath(A.Tag, E.Tag, new PmPath(A, D));
            PathMoverStatics.AddPath(D.Tag, E.Tag, new PmPath(D, E));

            PathMoverStatics.AddPath(E.Tag, F.Tag, new PmPath(E, D));
            PathMoverStatics.AddPath(D.Tag, F.Tag, new PmPath(D, C));

            PathMoverStatics.AddPath(E.Tag, C.Tag, new PmPath(E, D));
            PathMoverStatics.AddPath(D.Tag, C.Tag, new PmPath(D, C));

            PathMoverStatics.AddPath(B.Tag, C.Tag, new PmPath(B, C));

            //创建车辆(及目的地序列)
            double agvSpeed = 1d;
            List<ControlPoint> targetList1 = new List<ControlPoint>();
            targetList1.Add(A);
            targetList1.Add(E);
            targetList1.Add(F); 
            Vehicle v1 = new Vehicle("AGV-01", agvSpeed, targetList1, PathMoverStatics);
            Console.WriteLine(v1.ToString());

            List<ControlPoint> targetList2 = new List<ControlPoint>();
            targetList2.Add(C);
            targetList2.Add(F);
            Vehicle v2 = new Vehicle("AGV-02", agvSpeed, targetList2, PathMoverStatics);
            Console.WriteLine(v2.ToString());

            List<ControlPoint> targetList3 = new List<ControlPoint>();
            targetList3.Add(D);
            targetList3.Add(C);
            targetList3.Add(F);
            Vehicle v3 = new Vehicle("AGV-03", agvSpeed, targetList3, PathMoverStatics);
            Console.WriteLine(v3.ToString());
            Vehicle v3_1 = new Vehicle("AGV-03_1", agvSpeed, new List<ControlPoint>(targetList3), PathMoverStatics);
            Console.WriteLine(v3_1.ToString());

            List<ControlPoint> targetList4 = new List<ControlPoint>();
            targetList4.Add(E);
            targetList4.Add(C);
            targetList4.Add(F);
            Vehicle v4 = new Vehicle("AGV-04", agvSpeed, targetList4, PathMoverStatics);
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

        void ShowExit(Vehicle v, ControlPoint cp)
        {
            Console.WriteLine($"{v.Name} get out from CP: {cp.Tag}\n");
            _Result4UnitTest += string.Format($"{v.Name} get out from CP: {cp.Tag}\n");
            OnThen.Invoke(v, cp);
        }

        public event Action<Vehicle, ControlPoint> OnThen = (v, cp) => { };
    }

}
