using System;
using PathMoverRoutingGenerator;

namespace RoutingTableBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("PathMover Routing Table Builder");
            Console.WriteLine("========================================\n");

            // Configuration
            string jsonFilePath = @"c:\repos\PathMover\control_points_v16.json";
            string outputPath = @"c:\repos\PathMover\routing_table.json";
            int numRoutes = 100;
            bool completeMode = false;
            int? seed = 42;

            // Collect positional arguments (non-flag arguments)
            var positionalArgs = new List<string>();

            // Parse command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--complete" || args[i] == "-c")
                {
                    completeMode = true;
                }
                else if (args[i] == "--help" || args[i] == "-h")
                {
                    PrintUsage();
                    return;
                }
                else if (args[i] == "--seed" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int s))
                        seed = s;
                }
                else if (!args[i].StartsWith("-"))
                {
                    positionalArgs.Add(args[i]);
                }
            }

            // Process positional arguments
            if (positionalArgs.Count >= 1)
                jsonFilePath = positionalArgs[0];
            if (positionalArgs.Count >= 2)
                outputPath = positionalArgs[1];
            if (positionalArgs.Count >= 3 && int.TryParse(positionalArgs[2], out int n))
                numRoutes = n;
            if (positionalArgs.Count >= 4 && int.TryParse(positionalArgs[3], out int s2))
                seed = s2;

            // Set default output path for complete mode if not explicitly provided
            if (completeMode && positionalArgs.Count < 2)
                outputPath = @"c:\repos\PathMover\routing_table_complete.json";

            try
            {
                Console.WriteLine($"Input JSON: {jsonFilePath}");
                Console.WriteLine($"Output File: {outputPath}");
                
                if (completeMode)
                {
                    Console.WriteLine($"Mode: COMPLETE (all entry/exit pairs using Dijkstra)");
                }
                else
                {
                    Console.WriteLine($"Mode: Random sampling");
                    Console.WriteLine($"Number of Routes: {numRoutes}");
                }

                // Create generator
                var generator = new RoutingTableGenerator(jsonFilePath);

                RoutingTable routingTable;

                if (completeMode)
                {
                    // Generate COMPLETE routing table using Dijkstra
                    Console.WriteLine("\nStarting Dijkstra-based complete table generation...");
                    routingTable = generator.GenerateCompleteRoutingTable();
                }
                else
                {
                    // Generate random routes using A*
                    Console.WriteLine("\nStarting A* pathfinding...");
                    var startTime = DateTime.Now;
                    routingTable = generator.GenerateRandomRoutes(numRoutes, seed: seed);
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"\nPathfinding completed in {elapsed.TotalSeconds:F2} seconds");
                }

                // Save to file
                Console.WriteLine();
                generator.SaveRoutingTable(routingTable, outputPath);

                Console.WriteLine("\n========================================");
                Console.WriteLine("SUCCESS!");
                Console.WriteLine("========================================");
                Console.WriteLine($"Routing table saved to: {outputPath}");
                Console.WriteLine($"Total routing entries: {routingTable.Routes.Count:N0}");
                
                // Calculate file size
                var fileInfo = new System.IO.FileInfo(outputPath);
                Console.WriteLine($"File size: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");

                Console.WriteLine("\nYou can now use this routing table in PathMover simulation.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  RoutingTableBuilder [options] [jsonPath] [outputPath] [numRoutes] [seed]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --complete, -c    Generate COMPLETE routing table (all entry/exit pairs)");
            Console.WriteLine("                    Uses Dijkstra algorithm - MUCH faster than A* per pair");
            Console.WriteLine("  --seed N          Random seed for route generation (default: 42)");
            Console.WriteLine("  --help, -h        Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Generate 100 random routes (default)");
            Console.WriteLine("  RoutingTableBuilder");
            Console.WriteLine();
            Console.WriteLine("  # Generate complete routing table");
            Console.WriteLine("  RoutingTableBuilder --complete");
            Console.WriteLine();
            Console.WriteLine("  # Generate 500 random routes with custom seed");
            Console.WriteLine("  RoutingTableBuilder input.json output.json 500 123");
            Console.WriteLine();
            Console.WriteLine("Performance comparison:");
            Console.WriteLine("  Random (100 routes):  ~1 second,  ~10K entries,  ~1 MB");
            Console.WriteLine("  Complete (5596 dest): ~5 minutes, ~3M entries,   ~300 MB");
        }
    }
}
