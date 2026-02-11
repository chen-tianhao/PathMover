using System;
using System.IO;
using System.Collections.Generic;
using PathMover;

namespace PathMoverTest
{
    /// <summary>
    /// Logs vehicle movements during simulation for visualization
    /// </summary>
    public class SimulationLogger
    {
        private StreamWriter _writer;
        private string _logPath;

        public SimulationLogger(string logPath)
        {
            _logPath = logPath;
            _writer = new StreamWriter(logPath);
            _writer.WriteLine("timestamp,vehicle_id,control_point_id");
            _writer.Flush();
        }

        public void LogVehiclePosition(double timestamp, string vehicleId, string controlPointId)
        {
            _writer.WriteLine($"{timestamp:F2},{vehicleId},{controlPointId}");
            _writer.Flush();
        }

        public void Close()
        {
            _writer?.Close();
        }
    }
}
