using O2DESNet;
using PathMover;

namespace PathMoverTest
{
    public class Simulator : Sandbox
    {
        #region Statics
        PathMoverStatics PathMoverStatics { get; set; }
        int _numberOfAGV = 5;
        #endregion

        #region Dynamic
        PathMover.PathMover PathMover { get; set; }
        public int NumberRequest { get; private set; } = 0;
        public int NumberEnter { get; private set; } = 0;
        public int NumberOut { get; private set; } = 0;
        #endregion
        public Simulator()
        {
            PathMoverStatics = new PathMoverStatics();

            //准备路由表
            //Dictionary<(ControlPoint, ControlPoint), ControlPoint> routeTable = new Dictionary<(ControlPoint, ControlPoint), ControlPoint>();
            ControlPoint A = new ControlPoint("A");
            ControlPoint B = new ControlPoint("B");
            ControlPoint C = new ControlPoint("C");
            ControlPoint D = new ControlPoint("D");
            ControlPoint E = new ControlPoint("E");
            ControlPoint F = new ControlPoint("F");

            PmPath AB = new PmPath(A, B);
            PmPath BC = new PmPath(B, C);
            PmPath CF = new PmPath(C, F);
            PmPath AD = new PmPath(A, D);
            PmPath DE = new PmPath(D, E);
            PmPath ED = new PmPath(E, D);
            PmPath DC = new PmPath(D, C);

            PathMoverStatics.AddPath(A.Tag, F.Tag, AB);
            PathMoverStatics.AddPath(B.Tag, F.Tag, BC);
            PathMoverStatics.AddPath(C.Tag, F.Tag, CF);

            PathMoverStatics.AddPath(A.Tag, E.Tag, AD);
            PathMoverStatics.AddPath(D.Tag, E.Tag, DE);

            PathMoverStatics.AddPath(E.Tag, F.Tag, ED);
            PathMoverStatics.AddPath(D.Tag, F.Tag, DC);

            PathMoverStatics.AddPath(E.Tag, C.Tag, ED);
            PathMoverStatics.AddPath(D.Tag, C.Tag, DC);

            PathMoverStatics.AddPath(B.Tag, C.Tag, BC);

            //创建车辆(及目的地序列)
            double agvSpeed = 1d;

            List<List<ControlPoint>> targetLists = new List<List<ControlPoint>> { 
                new List<ControlPoint> { A, E, F },
                new List<ControlPoint> { B, C, F },
                new List<ControlPoint> { D, C, F },
                new List<ControlPoint> { D, C, F },
                new List<ControlPoint> { E, C, F }
            };

            List<Vehicle> vehicles = new List<Vehicle>();
            for (int i = 0; i < _numberOfAGV; i++)
            {
                vehicles.Add(new Vehicle($"AGV-{i + 1}", agvSpeed, targetLists[i % targetLists.Count], PathMoverStatics));
            }

            //创建PM
            PathMover = AddChild(new PathMover.PathMover(PathMoverStatics, 0, 0));
            foreach (Vehicle vehicle in vehicles)
            {
                Console.WriteLine(vehicle.ToString());
                PathMover.RequestToEnter(vehicle, vehicle.TargetList[0]);
                NumberRequest++;
            }
            PathMover.OnEnter += ShowEnter;
            PathMover.OnReadyToExit += ShowExit;
        }

        void ShowEnter(IVehicle v, ControlPoint cp)
        {
            Console.WriteLine($"{v.Name} exter from: {cp.Tag}");
            NumberEnter++;
        }

        void ShowExit(IVehicle v, ControlPoint cp)
        {
            Console.WriteLine($"{v.Name} exit from: {cp.Tag}");
            NumberOut++;
            PathMover.Exit(v, cp);
        }
    }

}
