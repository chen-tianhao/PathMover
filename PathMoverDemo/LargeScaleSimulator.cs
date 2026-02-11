using O2DESNet;
using PathMover;
using PathMoverRoutingGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PathMoverTest
{
    public class LargeScaleSimulator : Sandbox
    {
        #region Statics
        PathMoverStatics PathMoverStatics { get; set; }
        NetworkLoader NetworkLoader { get; set; }
        RoutingTable RoutingTable { get; set; }
        SimulationLogger Logger { get; set; }
        DateTime SimulationStartTime { get; set; }
        
        // Configurable parameters
        public int NumberOfAGVs { get; set; } = 10;
        public double AGVSpeed { get; set; } = 2.0; // meters per second
        public int RandomSeed { get; set; } = 42;
        public TimeSpan SimulationDuration { get; set; } = TimeSpan.FromMinutes(15);
        #endregion

        #region Dynamic
        PathMover.PathMover PathMover { get; set; }
        public int NumberRequest { get; private set; } = 0;
        public int NumberEnter { get; private set; } = 0;
        public int NumberOut { get; private set; } = 0;
        
        // Performance metrics
        private Dictionary<string, DateTime> _vehicleEnterTimes = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> _vehicleExitTimes = new Dictionary<string, DateTime>();
        private Dictionary<string, List<double>> _vehicleWaitingTimes = new Dictionary<string, List<double>>();
        private Dictionary<string, int> _vehicleWaypointCounts = new Dictionary<string, int>();
        #endregion

        public LargeScaleSimulator(string jsonFilePath, string routingTablePath, string logPath = null, 
            int numberOfAGVs = 10, double agvSpeed = 2.0, int randomSeed = 42, int simulationMinutes = 15)
        {
            // Set configurable parameters
            NumberOfAGVs = numberOfAGVs;
            AGVSpeed = agvSpeed;
            RandomSeed = randomSeed;
            SimulationDuration = TimeSpan.FromMinutes(simulationMinutes);
            
            Console.WriteLine("Initializing Large-Scale Simulator...");
            Console.WriteLine($"  AGVs: {NumberOfAGVs}, Speed: {AGVSpeed} m/s, Seed: {RandomSeed}, Duration: {simulationMinutes} min");
            
            SimulationStartTime = DateTime.Now;
            
            // Initialize logger if path provided
            if (!string.IsNullOrEmpty(logPath))
            {
                Logger = new SimulationLogger(logPath);
                Console.WriteLine($"Logging simulation to: {logPath}");
            }
            
            // Load network from JSON
            NetworkLoader = new NetworkLoader(jsonFilePath);
            PathMoverStatics = NetworkLoader.GetPathMoverStatics();
            
            // Print statistics
            NetworkLoader.PrintStatistics();

            // Load routing table
            Console.WriteLine($"\nLoading routing table from: {routingTablePath}");
            RoutingTable = RoutingTableGenerator.LoadRoutingTable(routingTablePath);
            Console.WriteLine($"Routing table loaded with {RoutingTable.Routes.Count} entries");

            // Create PathMover engine
            Console.WriteLine("Creating PathMover simulation engine...");
            PathMover = AddChild(new PathMover.PathMover(PathMoverStatics, smoothFactor: 0.5, coldStartDelay: 0.1));

            // Assign routes from routing table to AGVs
            Console.WriteLine($"\nAssigning first {NumberOfAGVs} routes from routing table to AGVs...");
            CreateVehiclesWithRandomRoutes();

            // Hook up event handlers
            PathMover.OnEnter += ShowEnter;
            PathMover.OnReadyToExit += ShowExit;
            PathMover.OnArrive += LogArrive;
            // Removed OnDepart logging to avoid duplicate/backward position logging

            Console.WriteLine("Initialization complete!\n");
        }

        private void CreateVehiclesWithRandomRoutes()
        {
            // Pre-build path lookup for faster pathfinding
            Console.WriteLine("Building path lookup index...");
            SimpleVehicle.BuildPathLookup(PathMoverStatics);

            // Randomly select N routes from routing table
            var allRouteKeys = RoutingTable.Routes.Keys.ToList();
            var random = new Random(RandomSeed);
            
            if (allRouteKeys.Count == 0)
            {
                Console.WriteLine("No routes available in routing table");
                return;
            }

            // Randomly shuffle and take first N routes
            var selectedRoutes = allRouteKeys.OrderBy(x => random.Next()).Take(NumberOfAGVs).ToList();
            
            Console.WriteLine($"Randomly selected {selectedRoutes.Count} routes from {allRouteKeys.Count} available routes");

            for (int i = 0; i < selectedRoutes.Count; i++)
            {
                // Get route key as (ushort, ushort) tuple
                var routeKey = selectedRoutes[i];
                ushort fromId = routeKey.Item1;
                ushort toId = routeKey.Item2;

                // Get control points
                var controlPoints = NetworkLoader.GetControlPoints();
                if (!controlPoints.ContainsKey(fromId) || !controlPoints.ContainsKey(toId))
                {
                    string fromName = PathMoverStatics.IdMapper.GetName(fromId);
                    string toName = PathMoverStatics.IdMapper.GetName(toId);
                    Console.WriteLine($"  Skipping route with invalid control points: {fromName} => {toName}");
                    continue;
                }

                var startPoint = controlPoints[fromId];
                var endPoint = controlPoints[toId];

                // Create vehicle with destination
                var targetList = new List<ControlPoint> { endPoint };
                var vehicle = new SimpleVehicle($"AGV-{i + 1:D4}", AGVSpeed, targetList, PathMoverStatics, RoutingTable);
                
                // Initialize metrics tracking
                _vehicleWaitingTimes[vehicle.Name] = new List<double>();
                _vehicleWaypointCounts[vehicle.Name] = 0;

                string fromNameLog = PathMoverStatics.IdMapper.GetName(fromId);
                string toNameLog = PathMoverStatics.IdMapper.GetName(toId);
                Console.WriteLine($"  {vehicle.Name}: {fromNameLog} → {toNameLog}");
                
                // Request to enter at start point
                PathMover.RequestToEnter(vehicle, startPoint);
                NumberRequest++;
            }
        }

        void ShowEnter(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"[{ClockTime:hh\\:mm\\:ss}] {v.Name} entered at: {cpName}");
            NumberEnter++;
            _vehicleEnterTimes[v.Name] = DateTime.Now;
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, cpName);
        }

        void ShowExit(IVehicle v, ControlPoint cp)
        {
            string cpName = PathMoverStatics.IdMapper.GetName(cp.Id);
            Console.WriteLine($"[{ClockTime:hh\\:mm\\:ss}] {v.Name} exited at: {cpName}");
            NumberOut++;
            _vehicleExitTimes[v.Name] = DateTime.Now;
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, cpName);
            PathMover.Exit(v, cp);
        }

        void LogArrive(IVehicle v, PmPath path)
        {
            // Log when vehicle arrives at the end of a path segment
            string endName = PathMoverStatics.IdMapper.GetName(path.EndPoint.Id);
            Logger?.LogVehiclePosition((ClockTime - SimulationStartTime).TotalSeconds, v.Name, endName);
            
            // Track waypoint count
            if (_vehicleWaypointCounts.ContainsKey(v.Name))
            {
                _vehicleWaypointCounts[v.Name]++;
            }
        }
        
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
            
            // Calculate travel times
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
            
            if (travelTimes.Count > 0)
            {
                Console.WriteLine($"\nAggregate Statistics:");
                Console.WriteLine($"  Average Travel Time: {travelTimes.Average():F1}s");
                Console.WriteLine($"  Min Travel Time: {travelTimes.Min():F1}s");
                Console.WriteLine($"  Max Travel Time: {travelTimes.Max():F1}s");
                Console.WriteLine($"  Total Vehicles Completed: {travelTimes.Count}/{NumberOfAGVs}");
                
                var avgWaypoints = _vehicleWaypointCounts.Values.Average();
                Console.WriteLine($"  Average Waypoints per Vehicle: {avgWaypoints:F1}");
            }
        }
    }

    /// <summary>
    /// Simple vehicle implementation for large-scale network
    /// Uses BFS pathfinding with optimized lookup
    /// </summary>
    public class SimpleVehicle : IVehicle
    {
        private static Dictionary<ushort, List<PmPath>> _pathLookup;
        private RoutingTable _routingTable;

        public string Name { get; set; }
        public PathMoverStatics PathMoverStatics { get; set; }
        public double Speed { get; set; }
        public int CapacityNeeded { get; set; } = 1;
        public PmPath CurrentPath { get; set; }
        public PmPath? PengingPath { get; set; }
        public bool IsStoped { get; set; } = false;
        public List<ControlPoint> TargetList { get; set; }

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

        public SimpleVehicle(string name, double speed, List<ControlPoint> targetList, PathMoverStatics pathMoverStatics, RoutingTable routingTable = null)
        {
            Name = name;
            Speed = speed;
            TargetList = new List<ControlPoint>(targetList);
            PathMoverStatics = pathMoverStatics;
            _routingTable = routingTable;
        }

        public void RemoveTarget(ushort controlPointId)
        {
            TargetList.RemoveAll(cp => cp.Id == controlPointId);
        }

        public PmPath NextPath(ushort currentPointId)
        {
            if (TargetList.Count == 0)
            {
                return null; // Reached destination
            }

            var nextTargetId = TargetList[0].Id;

            // Already at destination - no next path needed
            if (currentPointId == nextTargetId)
            {
                return null;
            }

            // STRICTLY use routing table only - no fallback pathfinding
            if (_routingTable != null)
            {
                var nextHop = _routingTable.GetNextHop(currentPointId, nextTargetId);
                if (nextHop.HasValue && PathMoverStatics.PathExists(currentPointId, nextHop.Value))
                {
                    return PathMoverStatics.GetPath(currentPointId, nextHop.Value);
                }
                else
                {
                    // No route in routing table - cannot proceed
                    string currentName = PathMoverStatics.IdMapper.GetName(currentPointId);
                    string targetName = PathMoverStatics.IdMapper.GetName(nextTargetId);
                    Console.WriteLine($"WARNING: No route in routing table from {currentName} to {targetName} for {Name}");
                    return null;
                }
            }

            // No routing table available - cannot proceed
            Console.WriteLine($"ERROR: No routing table available for {Name}");
            return null;
        }

        private PmPath FindNextHopBFS(ushort start, ushort target)
        {
            // BFS to find shortest path with optimized lookup
            var queue = new Queue<ushort>();
            var visited = new HashSet<ushort>();
            var parent = new Dictionary<ushort, (ushort node, PmPath path)>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == target)
                {
                    // Reconstruct path and return first hop
                    var node = target;
                    while (parent.ContainsKey(node))
                    {
                        var (prev, path) = parent[node];
                        if (prev == start)
                        {
                            return path; // This is the first hop
                        }
                        node = prev;
                    }
                    return null;
                }

                // Explore neighbors using optimized lookup
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

            return null; // No path found
        }

        public override string ToString()
        {
            return $"[{Name}] Targets: {string.Join(" → ", TargetList.Select(cp => PathMoverStatics.IdMapper.GetName(cp.Id)))}";
        }
    }
}
