using O2DESNet;
using PathMover;
using PathMoverRoutingGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PathMoverTest
{
    /// <summary>
    /// 大规模AGV（自动引导车）仿真器
    /// 继承自O2DESNet的Sandbox类，用于模拟大规模路径网络中多辆AGV的运行
    /// </summary>
    public class LargeScaleSimulator : Sandbox
    {
        #region Statics（静态配置）
        /// <summary>路径移动器的静态配置信息</summary>
        PathMoverStatics PathMoverStatics { get; set; }
        /// <summary>网络加载器，用于从JSON文件加载路径网络</summary>
        NetworkLoader NetworkLoader { get; set; }
        /// <summary>路由表，存储各控制点之间的路由信息</summary>
        RoutingTable RoutingTable { get; set; }
        /// <summary>仿真日志记录器</summary>
        SimulationLogger Logger { get; set; }
        /// <summary>仿真开始时间</summary>
        DateTime SimulationStartTime { get; set; }

        // 可配置参数
        /// <summary>AGV数量，默认10辆</summary>
        public int NumberOfAGVs { get; set; } = 10;
        /// <summary>AGV速度（米/秒），默认2.0 m/s</summary>
        public double AGVSpeed { get; set; } = 2.0;
        /// <summary>随机数种子，默认42，用于确保仿真可重复性</summary>
        public int RandomSeed { get; set; } = 42;
        /// <summary>仿真持续时间，默认15分钟</summary>
        public TimeSpan SimulationDuration { get; set; } = TimeSpan.FromMinutes(15);
        #endregion

        #region Dynamic（动态运行时数据）
        /// <summary>路径移动器仿真引擎实例</summary>
        PathMover.PathMover PathMover { get; set; }
        /// <summary>已发出的进入请求总数</summary>
        public int NumberRequest { get; private set; } = 0;
        /// <summary>已成功进入网络的AGV数量</summary>
        public int NumberEnter { get; private set; } = 0;
        /// <summary>已到达目标并退出网络的AGV数量</summary>
        public int NumberOut { get; private set; } = 0;

        // 性能指标数据
        /// <summary>各AGV进入网络的时间记录</summary>
        private Dictionary<string, DateTime> _vehicleEnterTimes = new Dictionary<string, DateTime>();
        /// <summary>各AGV退出网络的时间记录</summary>
        private Dictionary<string, DateTime> _vehicleExitTimes = new Dictionary<string, DateTime>();
        /// <summary>各AGV的等待时间记录列表</summary>
        private Dictionary<string, List<double>> _vehicleWaitingTimes = new Dictionary<string, List<double>>();
        /// <summary>各AGV经过的航路点数量</summary>
        private Dictionary<string, int> _vehicleWaypointCounts = new Dictionary<string, int>();
        #endregion

        /// <summary>
        /// 构造函数：初始化大规模仿真器
        /// </summary>
        /// <param name="jsonFilePath">路径网络JSON文件路径</param>
        /// <param name="routingTablePath">路由表文件路径</param>
        /// <param name="logPath">日志输出路径（可选）</param>
        /// <param name="numberOfAGVs">AGV数量</param>
        /// <param name="agvSpeed">AGV速度（米/秒）</param>
        /// <param name="randomSeed">随机种子</param>
        /// <param name="simulationMinutes">仿真时长（分钟）</param>
        public LargeScaleSimulator(string jsonFilePath, string routingTablePath, string logPath = null,
            int numberOfAGVs = 10, double agvSpeed = 2.0, int randomSeed = 42, int simulationMinutes = 15)
        {
            // 设置可配置参数
            NumberOfAGVs = numberOfAGVs;
            AGVSpeed = agvSpeed;
            RandomSeed = randomSeed;
            SimulationDuration = TimeSpan.FromMinutes(simulationMinutes);

            Console.WriteLine("Initializing Large-Scale Simulator...");
            Console.WriteLine($"  AGVs: {NumberOfAGVs}, Speed: {AGVSpeed} m/s, Seed: {RandomSeed}, Duration: {simulationMinutes} min");

            // 记录仿真启动时间
            SimulationStartTime = DateTime.Now;

            // 如果提供了日志路径，则初始化日志记录器
            if (!string.IsNullOrEmpty(logPath))
            {
                Logger = new SimulationLogger(logPath);
                Console.WriteLine($"Logging simulation to: {logPath}");
            }

            // 从JSON文件加载路径网络拓扑
            NetworkLoader = new NetworkLoader(jsonFilePath);
            PathMoverStatics = NetworkLoader.GetPathMoverStatics();

            // 打印网络统计信息（节点数、路径数等）
            NetworkLoader.PrintStatistics();

            // 从文件加载预计算的路由表
            Console.WriteLine($"\nLoading routing table from: {routingTablePath}");
            RoutingTable = RoutingTableGenerator.LoadRoutingTable(routingTablePath);
            Console.WriteLine($"Routing table loaded with {RoutingTable.Routes.Count} entries");

            // 创建PathMover仿真引擎，设置平滑因子和冷启动延迟
            Console.WriteLine("Creating PathMover simulation engine...");
            PathMover = AddChild(new PathMover.PathMover(PathMoverStatics, smoothFactor: 0.5, coldStartDelay: 0.1));

            // 从路由表中随机选择路线并分配给AGV
            Console.WriteLine($"\nAssigning first {NumberOfAGVs} routes from routing table to AGVs...");
            CreateVehiclesWithRandomRoutes();

            // 注册事件处理器
            PathMover.OnEnter += ShowEnter;         // AGV进入网络事件
            PathMover.OnReadyToExit += ShowExit;     // AGV准备退出网络事件
            PathMover.OnArrive += LogArrive;         // AGV到达路径段终点事件

            Console.WriteLine("Initialization complete!\n");
        }

        /// <summary>
        /// 创建AGV并为其分配随机路线
        /// 从路由表中随机选取指定数量的路线，为每条路线创建一辆AGV
        /// </summary>
        private void CreateVehiclesWithRandomRoutes()
        {
            // 预构建路径查找索引，加速寻路过程
            Console.WriteLine("Building path lookup index...");
            SimpleVehicle.BuildPathLookup(PathMoverStatics);

            // 获取路由表中所有可用路线的键（起点-终点对）
            var allRouteKeys = RoutingTable.Routes.Keys.ToList();
            var random = new Random(RandomSeed);

            if (allRouteKeys.Count == 0)
            {
                Console.WriteLine("No routes available in routing table");
                return;
            }

            // 随机打乱路线顺序，取前N条作为AGV的路线
            var selectedRoutes = allRouteKeys.OrderBy(x => random.Next()).Take(NumberOfAGVs).ToList();

            Console.WriteLine($"Randomly selected {selectedRoutes.Count} routes from {allRouteKeys.Count} available routes");

            for (int i = 0; i < selectedRoutes.Count; i++)
            {
                // 获取路线键，包含起点ID和终点ID
                var routeKey = selectedRoutes[i];
                ushort fromId = routeKey.Item1;
                ushort toId = routeKey.Item2;

                // 获取起点和终点的控制点对象
                var controlPoints = NetworkLoader.GetControlPoints();
                if (!controlPoints.ContainsKey(fromId) || !controlPoints.ContainsKey(toId))
                {
                    // 如果控制点无效，跳过该路线
                    string fromName = PathMoverStatics.IdMapper.GetName(fromId);
                    string toName = PathMoverStatics.IdMapper.GetName(toId);
                    Console.WriteLine($"  Skipping route with invalid control points: {fromName} => {toName}");
                    continue;
                }

                var startPoint = controlPoints[fromId];
                var endPoint = controlPoints[toId];

                // 创建AGV车辆实例，设置目标控制点
                var targetList = new List<ControlPoint> { endPoint };
                var vehicle = new SimpleVehicle($"AGV-{i + 1:D4}", AGVSpeed, targetList, PathMoverStatics, RoutingTable);

                // 初始化该AGV的性能指标跟踪
                _vehicleWaitingTimes[vehicle.Name] = new List<double>();
                _vehicleWaypointCounts[vehicle.Name] = 0;

                string fromNameLog = PathMoverStatics.IdMapper.GetName(fromId);
                string toNameLog = PathMoverStatics.IdMapper.GetName(toId);
                Console.WriteLine($"  {vehicle.Name}: {fromNameLog} → {toNameLog}");

                // 向PathMover引擎发送AGV进入请求
                PathMover.RequestToEnter(vehicle, startPoint);
                NumberRequest++;
            }
        }

        /// <summary>
        /// AGV进入网络时的事件处理器
        /// 记录进入时间和位置日志
        /// </summary>
        void ShowEnter(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"[{ClockTime:hh\\:mm\\:ss}] {v.Name} entered at: {cpName}");
            NumberEnter++;
            // 记录该AGV的进入时间，用于后续计算行驶时间
            _vehicleEnterTimes[v.Name] = DateTime.Now;
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, cpName);
        }

        /// <summary>
        /// AGV准备退出网络时的事件处理器
        /// 记录退出时间，并执行退出操作
        /// </summary>
        void ShowExit(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"[{ClockTime:hh\\:mm\\:ss}] {v.Name} exited at: {cpName}");
            NumberOut++;
            // 记录该AGV的退出时间，用于后续计算行驶时间
            _vehicleExitTimes[v.Name] = DateTime.Now;
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, cpName);
            // 通知PathMover引擎该AGV已退出
            PathMover.Exit(v, cp);
        }

        /// <summary>
        /// AGV到达路径段终点时的事件处理器
        /// 记录到达位置和航路点计数
        /// </summary>
        void LogArrive(IVehicle v, PmPath path)
        {
            // 记录AGV到达路径段终点的位置
            string endName = PathMoverStatics.IdMapper.GetName(path.EndPoint.Id);
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, endName);

            // 累计该AGV经过的航路点数量
            if (_vehicleWaypointCounts.ContainsKey(v.Name))
            {
                _vehicleWaypointCounts[v.Name]++;
            }
        }

        /// <summary>
        /// 打印仿真性能指标汇总
        /// 包括每辆AGV的行驶时间、航路点数量，以及整体统计数据
        /// </summary>
        public void PrintPerformanceMetrics()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("Performance Metrics");
            Console.WriteLine("========================================\n");

            if (_vehicleEnterTimes.Count == 0)
            {
                Console.WriteLine("No metrics collected.");
                return;
            }

            // 计算每辆AGV的行驶时间（从进入到退出的时间差）
            var travelTimes = new List<double>();
            foreach (var vehicleName in _vehicleEnterTimes.Keys)
            {
                if (_vehicleExitTimes.ContainsKey(vehicleName))
                {
                    var travelTime = (_vehicleExitTimes[vehicleName] - _vehicleEnterTimes[vehicleName]).TotalSeconds;
                    travelTimes.Add(travelTime);
                    Console.WriteLine($"{vehicleName}:");
                    Console.WriteLine($"  Travel Time: {travelTime:F1}s");
                    Console.WriteLine($"  Waypoints: {_vehicleWaypointCounts[vehicleName]}");
                }
            }

            // 输出汇总统计信息
            if (travelTimes.Count > 0)
            {
                Console.WriteLine($"\nAggregate Statistics:");
                Console.WriteLine($"  Average Travel Time: {travelTimes.Average():F1}s");   // 平均行驶时间
                Console.WriteLine($"  Min Travel Time: {travelTimes.Min():F1}s");           // 最短行驶时间
                Console.WriteLine($"  Max Travel Time: {travelTimes.Max():F1}s");           // 最长行驶时间
                Console.WriteLine($"  Total Vehicles Completed: {travelTimes.Count}/{NumberOfAGVs}"); // 完成数/总数

                var avgWaypoints = _vehicleWaypointCounts.Values.Average();
                Console.WriteLine($"  Average Waypoints per Vehicle: {avgWaypoints:F1}");   // 平均航路点数
            }
        }
    }

    /// <summary>
    /// 简单车辆实现类，用于大规模路径网络仿真
    /// 实现IVehicle接口，支持基于路由表的寻路和BFS备选寻路
    /// </summary>
    public class SimpleVehicle : IVehicle
    {
        /// <summary>路径查找索引，静态共享，按起点ID索引所有可用路径</summary>
        private static Dictionary<ushort, List<PmPath>> _pathLookup;
        /// <summary>路由表引用，用于快速查找下一跳</summary>
        private RoutingTable _routingTable;

        /// <summary>车辆名称标识</summary>
        public string Name { get; set; }
        /// <summary>路径网络的静态配置信息</summary>
        public PathMoverStatics PathMoverStatics { get; set; }
        /// <summary>车辆行驶速度（米/秒）</summary>
        public double Speed { get; set; }
        /// <summary>车辆所需容量单位，默认1</summary>
        public int CapacityNeeded { get; set; } = 1;
        /// <summary>当前正在行驶的路径段</summary>
        public PmPath CurrentPath { get; set; }
        /// <summary>等待进入的下一条路径段</summary>
        public PmPath? PengingPath { get; set; }
        /// <summary>车辆是否已停止</summary>
        public bool IsStoped { get; set; } = false;
        /// <summary>目标控制点列表</summary>
        public List<ControlPoint> TargetList { get; set; }

        /// <summary>
        /// 构建路径查找索引（静态方法）
        /// 将所有路径按起点ID分组，便于快速查找从某个控制点出发的所有路径
        /// </summary>
        /// <param name="pathMoverStatics">路径网络静态配置</param>
        public static void BuildPathLookup(PathMoverStatics pathMoverStatics)
        {
            _pathLookup = new Dictionary<ushort, List<PmPath>>();
            foreach (var path in pathMoverStatics.PathList)
            {
                if (!_pathLookup.ContainsKey(path.StartPoint.Id))
                {
                    _pathLookup[path.StartPoint.Id] = new List<PmPath>();
                }
                _pathLookup[path.StartPoint.Id].Add(path);
            }
        }

        /// <summary>
        /// 构造函数：创建一辆简单AGV车辆
        /// </summary>
        /// <param name="name">车辆名称</param>
        /// <param name="speed">行驶速度（米/秒）</param>
        /// <param name="targetList">目标控制点列表</param>
        /// <param name="pathMoverStatics">路径网络静态配置</param>
        /// <param name="routingTable">路由表（可选）</param>
        public SimpleVehicle(string name, double speed, List<ControlPoint> targetList, PathMoverStatics pathMoverStatics, RoutingTable routingTable = null)
        {
            Name = name;
            Speed = speed;
            TargetList = new List<ControlPoint>(targetList);
            PathMoverStatics = pathMoverStatics;
            _routingTable = routingTable;
        }

        /// <summary>
        /// 从目标列表中移除指定控制点
        /// 当AGV到达某个目标控制点后调用
        /// </summary>
        /// <param name="controlPointId">要移除的控制点ID</param>
        public void RemoveTarget(ushort controlPointId)
        {
            TargetList.RemoveAll(cp => cp.Id == controlPointId);
        }

        /// <summary>
        /// 计算从当前控制点出发的下一条路径
        /// 严格使用路由表查找下一跳，不进行回退寻路
        /// </summary>
        /// <param name="currentPointId">当前所在控制点ID</param>
        /// <returns>下一条要行驶的路径段，若已到达目标则返回null</returns>
        public PmPath NextPath(ushort currentPointId)
        {
            // 目标列表为空，说明已到达所有目标
            if (TargetList.Count == 0)
            {
                return null;
            }

            var nextTargetId = TargetList[0].Id;

            // 当前位置就是目标位置，无需移动
            if (currentPointId == nextTargetId)
            {
                return null;
            }

            // 严格使用路由表进行寻路，不使用备选方案
            if (_routingTable != null)
            {
                // 从路由表中查找下一跳控制点
                var nextHop = _routingTable.GetNextHop(currentPointId, nextTargetId);
                if (nextHop.HasValue && PathMoverStatics.PathExists(currentPointId, nextHop.Value))
                {
                    // 返回从当前点到下一跳的路径段
                    return PathMoverStatics.GetPath(currentPointId, nextHop.Value);
                }
                else
                {
                    // 路由表中没有对应的路由记录，无法继续行驶
                    string currentName = PathMoverStatics.IdMapper.GetName(currentPointId);
                    string targetName = PathMoverStatics.IdMapper.GetName(nextTargetId);
                    Console.WriteLine($"WARNING: No route in routing table from {currentName} to {targetName} for {Name}");
                    return null;
                }
            }

            // 没有可用的路由表，无法进行寻路
            Console.WriteLine($"ERROR: No routing table available for {Name}");
            return null;
        }

        /// <summary>
        /// BFS（广度优先搜索）寻路算法
        /// 使用预构建的路径查找索引在网络中寻找最短路径，返回第一跳路径
        /// 注意：当前版本中此方法作为备选方案保留，实际寻路严格使用路由表
        /// </summary>
        /// <param name="start">起点控制点ID</param>
        /// <param name="target">目标控制点ID</param>
        /// <returns>从起点出发的第一跳路径，若无路径则返回null</returns>
        private PmPath FindNextHopBFS(ushort start, ushort target)
        {
            // 使用BFS在路径网络图中搜索最短路径
            var queue = new Queue<ushort>();          // BFS搜索队列
            var visited = new HashSet<ushort>();       // 已访问的控制点集合
            var parent = new Dictionary<ushort, (ushort node, PmPath path)>(); // 回溯用的父节点记录

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == target)
                {
                    // 找到目标，回溯路径找到第一跳
                    var node = target;
                    while (parent.ContainsKey(node))
                    {
                        var (prev, path) = parent[node];
                        if (prev == start)
                        {
                            return path; // 返回从起点出发的第一跳路径
                        }
                        node = prev;
                    }
                    return null;
                }

                // 使用路径查找索引获取相邻控制点并扩展搜索
                if (_pathLookup != null && _pathLookup.ContainsKey(current))
                {
                    foreach (var path in _pathLookup[current])
                    {
                        if (!visited.Contains(path.EndPoint.Id))
                        {
                            visited.Add(path.EndPoint.Id);
                            parent[path.EndPoint.Id] = (current, path);
                            queue.Enqueue(path.EndPoint.Id);
                        }
                    }
                }
            }

            return null; // 未找到从起点到目标的路径
        }

        /// <summary>
        /// 返回车辆的字符串表示，包含名称和剩余目标列表
        /// </summary>
        public override string ToString()
        {
            return $"[{Name}] Targets: {string.Join(" → ", TargetList.Select(cp => PathMoverStatics.IdMapper.GetName(cp.Id)))}";
        }
    }
}
