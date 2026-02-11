# PathMover - AGV Path Movement Simulation Library

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/PathMover)](https://www.nuget.org/packages/PathMover/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

PathMover is a discrete-event simulation library built on the O2DESNet framework for modeling and simulating Automated Guided Vehicle (AGV) movements in warehouse and logistics networks.

## Features

- **Discrete-Event Simulation**: Built on O2DESNet framework for accurate time-based simulation
- **Path-Based Movement**: Models AGV movement along predefined paths between control points
- **Capacity Management**: Tracks path occupancy and prevents collisions
- **Routing Table Support**: Precomputed routing tables for efficient path finding
- **Large-Scale Networks**: Supports networks with thousands of control points
- **Performance Metrics**: Tracks utilization, throughput, and waiting times
- **Visualization Tools**: Python-based visualization of network and trajectories

## Project Structure

```
PathMover/
├── PathMoverLibrary/          # Core library (NuGet package)
│   ├── PathMover.cs          # Main simulation engine
│   ├── Vehicle.cs            # AGV vehicle interface and implementation
│   ├── ControlPoint.cs       # Network control points
│   ├── PathMoverStatics.cs   # Static network configuration
│   ├── PmPath.cs             # Path between control points
│   └── IdMapper.cs           # ID to name mapping
├── PathMoverDemo/            # Demo applications
│   ├── Program.cs            # Main demo program
│   ├── Simulator.cs          # Simple 6-node demo
│   ├── LargeScaleSimulator.cs # Large-scale simulation
│   ├── NetworkLoader.cs      # JSON network loader
│   └── SimulationLogger.cs   # Simulation logging
├── PathMoverRoutingGenerator/ # Routing table generation
│   ├── RoutingTableGenerator.cs
│   ├── AStarPathfinder.cs
│   └── DijkstraPathfinder.cs
├── RoutingTableBuilder/      # Routing table builder tool
├── NUnitTest_PM/             # Unit tests
├── Visualization/            # Python visualization tools
└── layout_gen/               # Network layout generation
```

## Installation

### NuGet Package
```bash
dotnet add package PathMover
```

### From Source
```bash
git clone https://github.com/chen-tianhao/PathMover.git
cd PathMover
dotnet build
```

## Quick Start

### Basic Usage

```csharp
using PathMover;
using O2DESNet;

// Create network statics
var statics = new PathMoverStatics();

// Add control points
var A = new ControlPoint(0);
var B = new ControlPoint(1);
var C = new ControlPoint(2);

// Add paths
statics.AddPath(A.Id, B.Id, new PmPath(A, B));
statics.AddPath(B.Id, C.Id, new PmPath(B, C));

// Create PathMover instance
var pathMover = new PathMover(statics, smoothFactor: 1.0, coldStartDelay: 0.0);

// Create AGV
var agv = new Vehicle
{
    Name = "AGV-001",
    Speed = 1.0,
    CapacityNeeded = 1
};

// Request entry
pathMover.RequestToEnter(agv, A);
```

### Running the Demo

```bash
cd PathMoverDemo
dotnet run
```

Choose simulation mode:
1. **Simple Demo**: 6 control points, 5 AGVs
2. **Large-Scale Network**: 14,944 control points from JSON data

## Core Concepts

### Control Points
Network nodes where AGVs can enter, exit, or change direction.

### Paths
Connections between control points with capacity constraints.

### Vehicles (AGVs)
- Move along paths between control points
- Have speed and capacity requirements
- Follow routing tables to reach destinations

### Routing Tables
Precomputed shortest paths between all control point pairs.

## Advanced Features

### Large-Scale Networks
Load network data from JSON files:
```csharp
var networkLoader = new NetworkLoader();
var statics = networkLoader.LoadFromJson("network.json");
```

### Performance Metrics
- Path occupancy rates
- Vehicle waiting times
- Throughput statistics
- Utilization metrics

### Visualization
Use the Python visualization tools:
```bash
cd Visualization
pip install -r requirements.txt
python visualize_network.py
python visualize_trajectories.py
```

## API Reference

### PathMover Class
- `RequestToEnter(IVehicle vehicle, ControlPoint cp)`: Request AGV entry
- `AttemptToDepart(IVehicle vehicle)`: Attempt to depart from current path
- `ReadyToExit(IVehicle vehicle, PmPath path)`: Signal ready to exit path

### IVehicle Interface
- `Name`: Vehicle identifier
- `Speed`: Movement speed
- `CapacityNeeded`: Path capacity required
- `CurrentPath`: Currently occupied path
- `TargetList`: Destination control points

## Examples

### Simple Network Simulation
See `PathMoverDemo/Simulator.cs` for a complete example with 6 control points and multiple AGVs.

### Custom Vehicle Implementation
```csharp
public class CustomVehicle : IVehicle
{
    public string Name { get; set; }
    public double Speed { get; set; }
    public int CapacityNeeded { get; set; }
    public PmPath CurrentPath { get; set; }
    public PmPath? PengingPath { get; set; }
    public bool IsStoped { get; set; }
    public List<ControlPoint> TargetList { get; set; }
    public PathMoverStatics PathMoverStatics { get; set; }

    // Implement interface methods
    public void RemoveTarget(ushort controlPointId) { ... }
    public PmPath NextPath(ushort currentPointId) { ... }
}
```

## Development

### Building from Source
```bash
dotnet build PathMover.sln
```

### Running Tests
```bash
cd NUnitTest_PM
dotnet test
```

### Creating NuGet Package
```bash
cd PathMoverLibrary
dotnet pack --configuration Release
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built on [O2DESNet](https://github.com/O2DESNet/O2DESNet) framework
- Developed at National University of Singapore (NUS)

## Support

For issues and questions, please use the [GitHub Issues](https://github.com/chen-tianhao/PathMover/issues) page.

## Change Log

### Version 2.0.0
- Performance optimization: Improved path finding algorithm for large-scale networks, enhancing simulation speed
- New feature: Added dynamic routing support, allowing vehicles to adjust paths based on real-time network status
- API improvements: Simplified vehicle interface with more flexible configuration options
- Visualization enhancement: Improved Python visualization tools with real-time simulation monitoring support

### Version 1.0.4
- Model revise: Since overtake is not allowed, only the 1st vehicle in OutPengList can be added to relevant InPendingList.

### Version 1.0.2
- Bug fix: Add PengingPath to interface IVehicle, to manage item inside path.InPendingList in event AttemptToDepart.

### Version 1.0.1
- Bug fix: Attempt to depart the specific vehicle in currentPath.InPendingList (rather than the 1st one in previousPath.OutPendingList) when release of capacity propagate backward.

### Version 1.0.0
- Initial release