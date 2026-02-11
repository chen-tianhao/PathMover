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

            PmPath AB = new PmPath(A, B);
            PmPath BC = new PmPath(B, C);
            PmPath CF = new PmPath(C, F);
            PmPath AD = new PmPath(A, D);
            PmPath DE = new PmPath(D, E);
            PmPath ED = new PmPath(E, D);
            PmPath DC = new PmPath(D, C);

            PathMoverStatics.AddPath(A.Id, F.Id, AB);
            PathMoverStatics.AddPath(B.Id, F.Id, BC);
            PathMoverStatics.AddPath(C.Id, F.Id, CF);

            PathMoverStatics.AddPath(A.Id, E.Id, AD);
            PathMoverStatics.AddPath(D.Id, E.Id, DE);

            PathMoverStatics.AddPath(E.Id, F.Id, ED);
            PathMoverStatics.AddPath(D.Id, F.Id, DC);

            PathMoverStatics.AddPath(E.Id, C.Id, ED);
            PathMoverStatics.AddPath(D.Id, C.Id, DC);

            PathMoverStatics.AddPath(B.Id, C.Id, BC);

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
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"{v.Name} enter from: {cpName}");
            NumberEnter++;
        }

        void ShowExit(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"{v.Name} exit from: {cpName}");
            NumberOut++;
            PathMover.Exit(v, cp);
        }
    }

}
