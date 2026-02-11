"""
PathMover Network Visualization
Displays the warehouse network and AGV trajectories
"""

import json
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.animation import FuncAnimation
from collections import defaultdict
import numpy as np

class NetworkVisualizer:
    def __init__(self, json_path):
        """Load and parse the network JSON file"""
        print(f"Loading network from: {json_path}")
        with open(json_path, 'r') as f:
            data = json.load(f)
        
        self.points = data['points']
        print(f"Loaded {len(self.points)} control points")
        
        # Build node lookup and edge list
        self.nodes = {}
        self.edges = []
        self.regions = defaultdict(list)
        self.entry_exit_points = []
        
        # Find max Y to shift coordinates to positive values
        max_y = max(point['y'] for point in self.points)
        
        for point in self.points:
            node_id = point['id']
            x, y = point['x'], point['y']
            region = point['region']
            inout = point.get('inout', False)
            
            # Keep original coordinate system (already top-left origin)
            # Just store as-is since JSON already has correct orientation
            self.nodes[node_id] = {'x': x, 'y': y, 'region': region, 'inout': inout}
            self.regions[region].append(node_id)
            
            if inout:
                self.entry_exit_points.append(node_id)
            
            # Build edges from 'next' field
            if 'next' in point and point['next']:
                for next_id in point['next']:
                    self.edges.append((node_id, next_id))
        
        print(f"Found {len(self.edges)} directed edges")
        print(f"Found {len(self.entry_exit_points)} entry/exit points")
        print(f"Regions: {list(self.regions.keys())}")
    
    def plot_network(self, show_all_edges=False, highlight_entry_exit=True):
        """Plot the warehouse network"""
        fig, ax = plt.subplots(figsize=(20, 14))
        
        # Don't plot edges - just show the node structure clearly
        print("Plotting control points...")
        xs = [node['x'] for node in self.nodes.values()]
        ys = [node['y'] for node in self.nodes.values()]
        ax.scatter(xs, ys, c='#333333', s=2, alpha=0.5, 
                  label=f'Control Points ({len(self.nodes)})', zorder=2)
        
        # Highlight entry/exit points with larger red stars
        if highlight_entry_exit:
            xs = [self.nodes[nid]['x'] for nid in self.entry_exit_points]
            ys = [self.nodes[nid]['y'] for nid in self.entry_exit_points]
            ax.scatter(xs, ys, c='#e74c3c', s=40, marker='*', 
                      label=f'Entry/Exit Points ({len(self.entry_exit_points)})', 
                      edgecolors='none', linewidths=0, zorder=10, alpha=0.8)
        
        ax.set_xlabel('X Position (units)', fontsize=12)
        ax.set_ylabel('Y Position (units)', fontsize=12)
        ax.set_title(f'PathMover Warehouse Network - {len(self.nodes):,} Control Points', 
                    fontsize=14, weight='bold')
        ax.legend(loc='upper right', markerscale=3, fontsize=11, framealpha=0.95)
        ax.grid(True, alpha=0.2, linestyle='--')
        ax.set_aspect('equal')
        ax.set_facecolor('white')
        ax.invert_yaxis()  # Top-left origin: Y increases downward
        
        plt.tight_layout()
        return fig, ax
    
    def load_simulation_log(self, log_path):
        """Load AGV trajectory data from simulation log"""
        # Format: timestamp, vehicle_id, control_point_id
        trajectories = defaultdict(list)
        
        try:
            with open(log_path, 'r') as f:
                # Skip header line
                header = f.readline()
                
                for line in f:
                    line = line.strip()
                    if not line:
                        continue
                        
                    parts = line.split(',')
                    if len(parts) >= 3:
                        try:
                            timestamp = float(parts[0])
                            vehicle_id = parts[1]
                            point_id = parts[2]
                            
                            if point_id in self.nodes:
                                x = self.nodes[point_id]['x']
                                y = self.nodes[point_id]['y']
                                trajectories[vehicle_id].append((timestamp, x, y))
                        except ValueError:
                            continue  # Skip invalid lines
        except FileNotFoundError:
            print(f"Log file not found: {log_path}")
            return None
        
        print(f"Loaded trajectories for {len(trajectories)} vehicles")
        return trajectories
    
    def plot_trajectories(self, trajectories):
        """Plot AGV trajectories on the network with minimal background clutter"""
        fig, ax = plt.subplots(figsize=(24, 14))
        
        # Plot only control points (no arrows for clarity)
        print("Plotting control points as background...")
        xs = [node['x'] for node in self.nodes.values()]
        ys = [node['y'] for node in self.nodes.values()]
        ax.scatter(xs, ys, c='lightgray', s=1, alpha=0.2, zorder=1)
        
        # Highlight entry/exit points
        xs_entry = [self.nodes[nid]['x'] for nid in self.entry_exit_points]
        ys_entry = [self.nodes[nid]['y'] for nid in self.entry_exit_points]
        ax.scatter(xs_entry, ys_entry, c='gray', s=30, marker='*', 
                  alpha=0.4, edgecolors='none', zorder=2,
                  label=f'Entry/Exit Points ({len(self.entry_exit_points)})')
        
        # Collect all edges used in trajectories to highlight them
        trajectory_edges = set()
        for vehicle_id, path in trajectories.items():
            if len(path) > 1:
                timestamps, xs, ys = zip(*path)
                for i in range(len(path) - 1):
                    x1, y1 = xs[i], ys[i]
                    x2, y2 = xs[i+1], ys[i+1]
                    trajectory_edges.add((x1, y1, x2, y2))
        
        # Plot only the edges used by trajectories
        print(f"Plotting {len(trajectory_edges)} edges used by trajectories...")
        for x1, y1, x2, y2 in trajectory_edges:
            ax.plot([x1, x2], [y1, y2], color='lightblue', 
                   linewidth=0.3, alpha=0.3, zorder=3)
        
        # Color palette for AGVs - use distinct bright colors
        colors = plt.cm.tab20(np.linspace(0, 1, 20))
        
        # Plot each AGV's trajectory with prominent styling
        print(f"\nPlotting trajectories for {len(trajectories)} vehicles...")
        for idx, (vehicle_id, path) in enumerate(trajectories.items()):
            if len(path) > 1:
                timestamps, xs, ys = zip(*path)
                color = colors[idx % 20]
                
                # Plot trajectory line
                ax.plot(xs, ys, color=color, linewidth=4, 
                       alpha=0.9, label=vehicle_id, zorder=20 + idx,
                       solid_capstyle='round')
                
                # Mark start with large circle
                ax.scatter(xs[0], ys[0], color=color, s=300, 
                          marker='o', edgecolors='white', linewidths=3, 
                          zorder=100)
                
                # Mark end with large square
                ax.scatter(xs[-1], ys[-1], color=color, s=300, 
                          marker='s', edgecolors='white', linewidths=3, 
                          zorder=100)
                
                # Add text labels for start and end
                ax.text(xs[0], ys[0], vehicle_id.split('-')[1], 
                       fontsize=8, ha='center', va='center',
                       color='white', weight='bold', zorder=101)
                
                print(f"  {vehicle_id}: {len(path)} waypoints, {timestamps[-1]-timestamps[0]:.1f}s duration")
        
        ax.set_xlabel('X Position (units)', fontsize=14, weight='bold')
        ax.set_ylabel('Y Position (units)', fontsize=14, weight='bold')
        ax.set_title(f'PathMover AGV Trajectories - {len(trajectories)} Vehicles', 
                    fontsize=16, weight='bold', pad=20)
        ax.legend(loc='center left', bbox_to_anchor=(1.01, 0.5), 
                 fontsize=10, framealpha=0.95, edgecolor='gray')
        ax.grid(True, alpha=0.2, linestyle='--')
        ax.set_aspect('equal')
        ax.set_facecolor('#f8f8f8')
        ax.invert_yaxis()  # Top-left origin: Y increases downward
        
        plt.tight_layout()
        return fig, ax


def main():
    """Main visualization function"""
    import os
    
    # Path to the network JSON
    json_path = r"c:\repos\PathMover\control_points_v16.json"
    
    if not os.path.exists(json_path):
        print(f"ERROR: Network JSON not found at {json_path}")
        return
    
    # Create visualizer
    viz = NetworkVisualizer(json_path)
    
    # Plot the network
    print("\nGenerating network visualization...")
    fig, ax = viz.plot_network(show_all_edges=False, highlight_entry_exit=True)
    
    # Save the figure
    output_path = r"c:\repos\PathMover\Visualization\network_map.png"
    fig.savefig(output_path, dpi=200, bbox_inches='tight')
    print(f"Saved network map to: {output_path}")
    
    # Show the plot
    plt.show()
    
    # Check if simulation log exists
    log_path = r"c:\repos\PathMover\Visualization\simulation_log.csv"
    if os.path.exists(log_path):
        print(f"\nLoading simulation trajectories from: {log_path}")
        trajectories = viz.load_simulation_log(log_path)
        
        if trajectories:
            fig2, ax2 = viz.plot_trajectories(trajectories)
            output_path2 = r"c:\repos\PathMover\Visualization\trajectories.png"
            fig2.savefig(output_path2, dpi=200, bbox_inches='tight')
            print(f"Saved trajectories to: {output_path2}")
            plt.show()
    else:
        print(f"\nNo simulation log found at: {log_path}")
        print("To visualize trajectories, run the simulation with logging enabled.")


if __name__ == '__main__':
    main()
