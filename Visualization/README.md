# PathMover Network Visualization

Visualize the warehouse network and AGV trajectories.

## Setup

1. Install Python dependencies:
```bash
pip install -r requirements.txt
```

## Usage

### View the Network Map

```bash
python visualize_network.py
```

This will display and save the warehouse network with:
- Control points colored by region
- Entry/exit points marked with red stars
- Network structure

### View AGV Trajectories

To visualize AGV paths, first generate a simulation log from PathMoverDemo.
Then run the visualization with the log file.

The script looks for `simulation_log.csv` with format:
```
timestamp,vehicle_id,control_point_id
0.0,AGV-0001,OR_R142_S02_0012
1.5,AGV-0001,OR_R143_S02_0012
...
```

## Output

- `network_map.png` - Network structure visualization
- `trajectories.png` - AGV trajectories overlaid on network
