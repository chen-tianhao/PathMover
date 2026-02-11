using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace PathMoverRoutingGenerator
{
    /// <summary>
    /// Generates routing tables for PathMover using A* or Dijkstra pathfinding
    /// </summary>
    public class RoutingTableGenerator
    {
        private NetworkData _networkData;
        private AStarPathfinder _pathfinder;
        private DijkstraPathfinder _dijkstraPathfinder;
        private Dictionary<string, ushort> _nameToId;
        private Dictionary<ushort, string> _idToName;

        public RoutingTableGenerator(string jsonFilePath)
        {
            LoadNetwork(jsonFilePath);
            BuildIdMapping();
            _pathfinder = new AStarPathfinder(_networkData.points, _nameToId);
            _dijkstraPathfinder = new DijkstraPathfinder(_networkData.points, _nameToId);
        }

        private void LoadNetwork(string jsonFilePath)
        {
            Console.WriteLine($"Loading network from: {jsonFilePath}");
            string jsonContent = File.ReadAllText(jsonFilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            _networkData = JsonSerializer.Deserialize<NetworkData>(jsonContent, options);
            Console.WriteLine($"Loaded {_networkData.points.Count} control points");
        }

        private void BuildIdMapping()
        {
            _nameToId = new Dictionary<string, ushort>();
            _idToName = new Dictionary<ushort, string>();
            
            ushort id = 0;
            foreach (var point in _networkData.points)
            {
                _nameToId[point.id] = id;
                _idToName[id] = point.id;
                id++;
            }
            
            Console.WriteLine($"Built ID mapping for {_nameToId.Count} control points");
        }

        /// <summary>
        /// Generate N random routes between entry/exit points
        /// </summary>
        public RoutingTable GenerateRandomRoutes(int numRoutes, int? seed = null)
        {
            var entryExitPoints = _networkData.points
                .Where(p => p.inout)
                .Select(p => p.id)
                .ToList();

            Console.WriteLine($"Found {entryExitPoints.Count} entry/exit points");
            Console.WriteLine($"Generating {numRoutes} routes...");

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var routingTable = new RoutingTable();
            var processedRoutes = new HashSet<(ushort, ushort)>();

            int successCount = 0;
            int attemptCount = 0;
            int maxAttempts = numRoutes * 3; // Allow multiple attempts

            while (successCount < numRoutes && attemptCount < maxAttempts)
            {
                attemptCount++;

                // Pick random start and end
                var startName = entryExitPoints[random.Next(entryExitPoints.Count)];
                var endName = entryExitPoints[random.Next(entryExitPoints.Count)];
                var startId = _nameToId[startName];
                var endId = _nameToId[endName];

                if (startId == endId || processedRoutes.Contains((startId, endId)))
                    continue;

                processedRoutes.Add((startId, endId));

                // Find path using A*
                var path = _pathfinder.FindPath(startId, endId);

                if (path != null && path.Count >= 2)
                {
                    // Build routing entries for this path
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var from = path[i];
                        var nextHop = path[i + 1];

                        // Store the next hop for this (current, destination) pair
                        if (!routingTable.Routes.ContainsKey((from, endId)))
                        {
                            routingTable.Routes[(from, endId)] = nextHop;
                        }
                    }

                    successCount++;
                    if (successCount % 10 == 0 || successCount == numRoutes)
                    {
                        Console.WriteLine($"  Progress: {successCount}/{numRoutes} routes generated");
                    }
                }
            }

            Console.WriteLine($"Successfully generated {successCount} routes");
            return routingTable;
        }

        /// <summary>
        /// Generate routing table for specific origin-destination pairs
        /// </summary>
        public RoutingTable GenerateSpecificRoutes(List<(string origin, string destination)> odPairs)
        {
            Console.WriteLine($"Generating routes for {odPairs.Count} O-D pairs...");

            var routingTable = new RoutingTable();
            int successCount = 0;

            for (int i = 0; i < odPairs.Count; i++)
            {
                var (originName, destinationName) = odPairs[i];
                var originId = _nameToId[originName];
                var destinationId = _nameToId[destinationName];

                var path = _pathfinder.FindPath(originId, destinationId);

                if (path != null && path.Count >= 2)
                {
                    // Build routing entries for this path
                    for (int j = 0; j < path.Count - 1; j++)
                    {
                        var from = path[j];
                        var nextHop = path[j + 1];

                        if (!routingTable.Routes.ContainsKey((from, destinationId)))
                        {
                            routingTable.Routes[(from, destinationId)] = nextHop;
                        }
                    }

                    successCount++;
                }

                if ((i + 1) % 100 == 0 || (i + 1) == odPairs.Count)
                {
                    Console.WriteLine($"  Progress: {i + 1}/{odPairs.Count} processed, {successCount} successful");
                }
            }

            Console.WriteLine($"Successfully generated {successCount} routes");
            return routingTable;
        }

        /// <summary>
        /// Save routing table to binary file (6 bytes per entry: from + dest + next)
        /// Format: [count:4bytes][from:2bytes][dest:2bytes][next:2bytes] repeated
        /// </summary>
        public void SaveRoutingTable(RoutingTable routingTable, string outputPath)
        {
            Console.WriteLine($"Saving routing table to: {outputPath}");
            Console.WriteLine($"Writing {routingTable.Routes.Count:N0} routing entries in binary format...");
            
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1048576);
            using var writer = new BinaryWriter(fileStream);
            
            // Write count
            writer.Write(routingTable.Routes.Count);
            
            int count = 0;
            int total = routingTable.Routes.Count;
            int lastPercent = -1;
            
            foreach (var kvp in routingTable.Routes)
            {
                writer.Write(kvp.Key.Item1);  // from (2 bytes)
                writer.Write(kvp.Key.Item2);  // destination (2 bytes)
                writer.Write(kvp.Value);       // nextHop (2 bytes)
                
                count++;
                int percent = (int)(count * 100.0 / total);
                if (percent != lastPercent && percent % 10 == 0)
                {
                    Console.WriteLine($"  Writing: {percent}% ({count:N0} / {total:N0})");
                    lastPercent = percent;
                }
            }
            
            writer.Flush();
            
            long fileSize = fileStream.Length;
            Console.WriteLine($"Saved {routingTable.Routes.Count:N0} routing entries");
            Console.WriteLine($"File size: {fileSize / (1024.0 * 1024.0):F2} MB");
        }

        /// <summary>
        /// Generate COMPLETE routing table for ALL entry/exit point pairs using Dijkstra.
        /// This is MUCH faster than A* per pair: O(D * graph) vs O(D * S * graph)
        /// where D = destinations, S = sources.
        /// </summary>
        public RoutingTable GenerateCompleteRoutingTable()
        {
            var entryExitPoints = _networkData.points
                .Where(p => p.inout)
                .Select(p => p.id)
                .ToList();

            Console.WriteLine($"Found {entryExitPoints.Count} entry/exit points");
            Console.WriteLine($"Generating COMPLETE routing table using Dijkstra...");
            Console.WriteLine($"This will process {entryExitPoints.Count} destinations (much faster than {(long)entryExitPoints.Count * entryExitPoints.Count:N0} A* calls!)");

            var stopwatch = Stopwatch.StartNew();

            var routes = _dijkstraPathfinder.GenerateCompleteRoutingTable(
                entryExitPoints,
                (processed, total, entries) =>
                {
                    double elapsed = stopwatch.Elapsed.TotalSeconds;
                    double rate = processed / elapsed;
                    double remaining = (total - processed) / rate;
                    Console.WriteLine($"  Progress: {processed}/{total} destinations ({100.0 * processed / total:F1}%) - {entries:N0} entries - ETA: {remaining:F0}s");
                });

            stopwatch.Stop();

            var routingTable = new RoutingTable { Routes = routes };

            Console.WriteLine($"\nComplete routing table generated in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
            Console.WriteLine($"Total routing entries: {routingTable.Routes.Count:N0}");

            return routingTable;
        }

        /// <summary>
        /// Load routing table from binary file
        /// </summary>
        public static RoutingTable LoadRoutingTable(string filePath)
        {
            Console.WriteLine($"Loading routing table from: {filePath}");
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1048576);
            using var reader = new BinaryReader(fileStream);
            
            int count = reader.ReadInt32();
            Console.WriteLine($"Reading {count:N0} routing entries...");
            
            var routingTable = new RoutingTable();
            
            int lastPercent = -1;
            for (int i = 0; i < count; i++)
            {
                ushort from = reader.ReadUInt16();
                ushort dest = reader.ReadUInt16();
                ushort next = reader.ReadUInt16();
                
                routingTable.Routes[(from, dest)] = next;
                
                int percent = (int)((i + 1) * 100.0 / count);
                if (percent != lastPercent && percent % 10 == 0)
                {
                    Console.WriteLine($"  Loading: {percent}% ({i + 1:N0} / {count:N0})");
                    lastPercent = percent;
                }
            }
            
            Console.WriteLine($"Loaded {routingTable.Routes.Count:N0} routing entries");
            
            return routingTable;
        }
    }

    /// <summary>
    /// Routing table data structure using integer IDs for memory efficiency
    /// </summary>
    public class RoutingTable
    {
        public Dictionary<(ushort, ushort), ushort> Routes { get; set; } = new Dictionary<(ushort, ushort), ushort>();
        
        /// <summary>
        /// Get next hop from current node toward destination
        /// </summary>
        public ushort? GetNextHop(ushort from, ushort destination)
        {
            var key = (from, destination);
            return Routes.ContainsKey(key) ? Routes[key] : (ushort?)null;
        }
    }
}
