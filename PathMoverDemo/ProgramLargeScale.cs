using System;
using PathMover;

namespace PathMoverTest
{
    public class ProgramLargeScale
    {
        public static void RunLargeScaleSimulation(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("PathMover - Large-Scale Network Simulation");
            Console.WriteLine("========================================\n");

            // Path to JSON file (adjust if needed)
            string jsonPath = @"c:\repos\PathMover\control_points_v16.json";
            string routingTablePath = @"c:\repos\PathMover\routing_table.json";

            if (!System.IO.File.Exists(jsonPath))
            {
                Console.WriteLine($"ERROR: JSON file not found at: {jsonPath}");
                Console.WriteLine("Please update the path in ProgramLargeScale.cs");
                return;
            }

            if (!System.IO.File.Exists(routingTablePath))
            {
                Console.WriteLine($"ERROR: Routing table not found at: {routingTablePath}");
                Console.WriteLine("Please generate routing table first using RoutingTableBuilder");
                return;
            }

            try
            {
                // Configurable simulation parameters
                int numberOfAGVs = 10;
                double agvSpeed = 2.0; // m/s
                int randomSeed = 42;
                int simulationMinutes = 15;
                
                // Create simulator with large network and routing table
                string logPath = @"c:\repos\PathMover\Visualization\simulation_log.csv";
                LargeScaleSimulator sim = new LargeScaleSimulator(
                    jsonPath, routingTablePath, logPath,
                    numberOfAGVs, agvSpeed, randomSeed, simulationMinutes);

                // Run simulation
                Console.WriteLine($"Starting simulation ({simulationMinutes} minutes simulated time)...\n");
                sim.Run(sim.SimulationDuration);

                // Print results
                Console.WriteLine("\n========================================");
                Console.WriteLine("Simulation Complete!");
                Console.WriteLine("========================================");
                Console.WriteLine($"AGVs Requested Entry: {sim.NumberRequest}");
                Console.WriteLine($"AGVs Entered Network: {sim.NumberEnter}");
                Console.WriteLine($"AGVs Exited Network:  {sim.NumberOut}");
                Console.WriteLine("========================================");
                
                // Print performance metrics
                sim.PrintPerformanceMetrics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
