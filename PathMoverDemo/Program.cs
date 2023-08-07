using PathMover;

namespace PathMoverTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Simulator sim = new Simulator();
            sim.Run(TimeSpan.FromMinutes(1));
        }
    }

}
