using PathMover;

namespace PathMoverTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Select simulation mode:");
            Console.WriteLine("1. Simple Demo (6 control points)");
            Console.WriteLine("2. Large-Scale Network (14,944 control points from JSON)");
            Console.Write("Enter choice (1 or 2): ");

            string choice = Console.ReadLine();

            if (choice == "2")
            {
                // Run large-scale simulation
                ProgramLargeScale.RunLargeScaleSimulation(args);
            }
            else
            {
                // Run simple demo (default)
                Console.WriteLine("\nRunning Simple Demo...\n");
                Simulator sim = new Simulator();
                sim.Run(TimeSpan.FromMinutes(1000));

                Console.WriteLine($"\nNumber of AGV Request: {sim.NumberRequest}");
                Console.WriteLine($"Number of AGV Enter: {sim.NumberEnter}");
                Console.WriteLine($"Number of AGV Exit: {sim.NumberOut}");
            }
        }
    }

}
