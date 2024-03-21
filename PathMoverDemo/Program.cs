using PathMover;

namespace PathMoverTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Simulator sim = new Simulator();
            sim.Run(TimeSpan.FromMinutes(1000));

            Console.WriteLine($"Number of AGV Request: {sim.NumberRequest}");
            Console.WriteLine($"Number of AGV Enter: {sim.NumberEnter}");
            Console.WriteLine($"Number of AGV Exit: {sim.NumberOut}");
        }
    }

}
