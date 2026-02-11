using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PathMover;

namespace PathMoverTest
{
    public class NetworkLoader
    {
        private PathMoverStatics _pathMoverStatics;
        private Dictionary<ushort, ControlPoint> _controlPoints;
        private Dictionary<string, ushort> _nameToId;
        private NetworkData _networkData;

        public NetworkLoader(string jsonFilePath)
        {
            _pathMoverStatics = new PathMoverStatics();
            _controlPoints = new Dictionary<ushort, ControlPoint>();
            _nameToId = new Dictionary<string, ushort>();
            LoadFromJson(jsonFilePath);
        }

        public PathMoverStatics GetPathMoverStatics()
        {
            return _pathMoverStatics;
        }

        public NetworkData GetNetworkData()
        {
            return _networkData;
        }

        public Dictionary<ushort, ControlPoint> GetControlPoints()
        {
            return _controlPoints;
        }

        public ushort GetId(string name)
        {
            return _nameToId[name];
        }

        private void LoadFromJson(string jsonFilePath)
        {
            Console.WriteLine($"Loading network from: {jsonFilePath}");
            
            // Read and parse JSON
            string jsonContent = File.ReadAllText(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _networkData = JsonSerializer.Deserialize<NetworkData>(jsonContent, options);

            if (_networkData == null || _networkData.points == null)
            {
                throw new Exception("Failed to parse JSON or no points found");
            }

            Console.WriteLine($"Found {_networkData.points.Count} control points");

            // Step 1: Build ID mapping (assign ushort IDs to string names)
            Console.WriteLine("Building ID mapping...");
            ushort nextId = 0;
            foreach (var pointData in _networkData.points)
            {
                if (!_nameToId.ContainsKey(pointData.id))
                {
                    _nameToId[pointData.id] = nextId;
                    _pathMoverStatics.IdMapper.Register(nextId, pointData.id);
                    nextId++;
                }
            }
            Console.WriteLine($"Mapped {_nameToId.Count} control points to IDs 0-{nextId-1}");

            // Step 2: Create all ControlPoint objects with ushort IDs
            Console.WriteLine("Creating control points...");
            foreach (var pointData in _networkData.points)
            {
                ushort id = _nameToId[pointData.id];
                if (!_controlPoints.ContainsKey(id))
                {
                    var cp = new ControlPoint(id);
                    _controlPoints.Add(id, cp);
                }
            }
            Console.WriteLine($"Created {_controlPoints.Count} control points");

            // Step 3: Create paths based on "next" connections
            Console.WriteLine("Creating paths...");
            int pathCount = 0;
            foreach (var pointData in _networkData.points)
            {
                if (pointData.next == null || pointData.next.Count == 0)
                {
                    continue;
                }

                ushort startId = _nameToId[pointData.id];
                var startPoint = _controlPoints[startId];

                foreach (var nextName in pointData.next)
                {
                    if (_nameToId.ContainsKey(nextName))
                    {
                        ushort endId = _nameToId[nextName];
                        var endPoint = _controlPoints[endId];

                        // Calculate path length using coordinates
                        double length = CalculateDistance(pointData, GetPointData(nextName));

                        // Create path with capacity 1 (strict single-vehicle only)
                        var path = new PmPath(startPoint, endPoint, capacity: 1, length: length, numberOfLane: 1);

                        // Add direct path (for physical connection)
                        _pathMoverStatics.AddPath(startId, endId, path);
                        pathCount++;
                    }
                }
            }
            Console.WriteLine($"Created {pathCount} paths");
        }

        private ControlPointData GetPointData(string id)
        {
            return _networkData.points.Find(p => p.id == id);
        }

        private double CalculateDistance(ControlPointData from, ControlPointData to)
        {
            if (from == null || to == null)
            {
                return 1.0; // Default distance
            }

            double dx = to.x - from.x;
            double dy = to.y - from.y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Return at least 0.1 to avoid zero-length paths
            return Math.Max(distance, 0.1);
        }

        public void BuildRoutingTable()
        {
            Console.WriteLine("Building routing table using Floyd-Warshall algorithm...");
            
            // Note: For a graph with 14,944 nodes, Floyd-Warshall would be extremely slow
            // (O(n³) = ~3.3 trillion operations)
            // For production use, consider Dijkstra's algorithm or precomputed routes
            
            Console.WriteLine("WARNING: Full routing table generation for 14,944 nodes is computationally expensive.");
            Console.WriteLine("Consider using on-demand pathfinding (Dijkstra/A*) instead.");
            Console.WriteLine("Current setup only includes direct connections. For multi-hop routing,");
            Console.WriteLine("implement Vehicle.NextPath() to use a pathfinding algorithm.");
        }

        public void PrintStatistics()
        {
            Console.WriteLine("\n=== Network Statistics ===");
            Console.WriteLine($"Total Control Points: {_controlPoints.Count}");
            Console.WriteLine($"Total Direct Paths: {_pathMoverStatics.PathList.Count}");
            
            // Count by region
            var regionCount = new Dictionary<string, int>();
            foreach (var pointData in _networkData.points)
            {
                if (!regionCount.ContainsKey(pointData.region))
                {
                    regionCount[pointData.region] = 0;
                }
                regionCount[pointData.region]++;
            }

            Console.WriteLine("\nControl Points by Region:");
            foreach (var kvp in regionCount)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }

            // Count by kind
            var kindCount = new Dictionary<string, int>();
            foreach (var pointData in _networkData.points)
            {
                var kind = pointData.meta?.kind ?? "unknown";
                if (!kindCount.ContainsKey(kind))
                {
                    kindCount[kind] = 0;
                }
                kindCount[kind]++;
            }

            Console.WriteLine("\nControl Points by Kind:");
            foreach (var kvp in kindCount)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }

            // Count entry/exit points
            int inoutCount = _networkData.points.Count(p => p.inout);
            Console.WriteLine($"\nEntry/Exit Points: {inoutCount}");
            Console.WriteLine("========================\n");
        }

        public List<ControlPoint> GetEntryExitPoints()
        {
            return _networkData.points
                .Where(p => p.inout)
                .Select(p => _controlPoints[_nameToId[p.id]])
                .ToList();
        }
    }
}
