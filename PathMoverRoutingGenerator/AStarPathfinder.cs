using System;
using System.Collections.Generic;
using System.Linq;

namespace PathMoverRoutingGenerator
{
    /// <summary>
    /// A* pathfinding algorithm implementation for routing table generation
    /// Now uses integer IDs for improved performance
    /// </summary>
    public class AStarPathfinder
    {
        private class Node : IComparable<Node>
        {
            public ushort Id { get; set; }
            public double G { get; set; } // Cost from start
            public double H { get; set; } // Heuristic to goal
            public double F => G + H; // Total cost
            public ushort Parent { get; set; }
            public ushort ParentEdge { get; set; } // The edge (from->to) used to reach this node

            public int CompareTo(Node other)
            {
                // Compare by F first, then by Id to ensure unique ordering
                // This prevents SortedSet from treating nodes with same F as duplicates
                int fCompare = F.CompareTo(other.F);
                if (fCompare != 0) return fCompare;
                return Id.CompareTo(other.Id);
            }
        }

        private Dictionary<ushort, ControlPointData> _pointsById;
        private Dictionary<ushort, List<ushort>> _adjacencyList;
        private Dictionary<string, ushort> _nameToId;

        public AStarPathfinder(List<ControlPointData> points, Dictionary<string, ushort> nameToId)
        {
            _nameToId = nameToId;
            _pointsById = new Dictionary<ushort, ControlPointData>();
            _adjacencyList = new Dictionary<ushort, List<ushort>>();

            // Build adjacency list using integer IDs
            foreach (var point in points)
            {
                ushort id = nameToId[point.id];
                _pointsById[id] = point;

                if (point.next != null && point.next.Count > 0)
                {
                    _adjacencyList[id] = point.next.Select(n => nameToId[n]).ToList();
                }
            }
        }

        /// <summary>
        /// Find shortest path from start to goal using A*
        /// Returns list of node IDs representing the complete path
        /// </summary>
        public List<ushort> FindPath(ushort startId, ushort goalId)
        {
            if (startId == goalId)
                return new List<ushort> { startId };

            if (!_pointsById.ContainsKey(startId) || !_pointsById.ContainsKey(goalId))
                return null;

            var openSet = new SortedSet<Node>();
            var closedSet = new HashSet<ushort>();
            var nodeDict = new Dictionary<ushort, Node>();

            var startNode = new Node
            {
                Id = startId,
                G = 0,
                H = Heuristic(startId, goalId),
                Parent = 0
            };

            openSet.Add(startNode);
            nodeDict[startId] = startNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);

                if (current.Id == goalId)
                {
                    return ReconstructPath(current, nodeDict);
                }

                closedSet.Add(current.Id);

                // Explore neighbors
                if (_adjacencyList.ContainsKey(current.Id))
                {
                    foreach (var neighborId in _adjacencyList[current.Id])
                    {
                        if (closedSet.Contains(neighborId))
                            continue;

                        double tentativeG = current.G + GetDistance(current.Id, neighborId);

                        if (!nodeDict.ContainsKey(neighborId))
                        {
                            var neighborNode = new Node
                            {
                                Id = neighborId,
                                G = tentativeG,
                                H = Heuristic(neighborId, goalId),
                                Parent = current.Id,
                                ParentEdge = neighborId
                            };
                            nodeDict[neighborId] = neighborNode;
                            openSet.Add(neighborNode);
                        }
                        else if (tentativeG < nodeDict[neighborId].G)
                        {
                            var neighborNode = nodeDict[neighborId];
                            openSet.Remove(neighborNode);
                            neighborNode.G = tentativeG;
                            neighborNode.Parent = current.Id;
                            neighborNode.ParentEdge = neighborId;
                            openSet.Add(neighborNode);
                        }
                    }
                }
            }

            return null; // No path found
        }

        /// <summary>
        /// Get the next hop from start toward goal
        /// Returns the first step in the optimal path
        /// </summary>
        public ushort? GetNextHop(ushort startId, ushort goalId)
        {
            var path = FindPath(startId, goalId);
            if (path == null || path.Count < 2)
                return null;

            return path[1]; // Return the next node after start
        }

        private List<ushort> ReconstructPath(Node goalNode, Dictionary<ushort, Node> nodeDict)
        {
            var path = new List<ushort>();
            var current = goalNode;

            while (current != null)
            {
                path.Add(current.Id);
                current = current.Parent != 0 ? nodeDict[current.Parent] : null;
            }

            path.Reverse();
            return path;
        }

        private double Heuristic(ushort fromId, ushort toId)
        {
            // Euclidean distance heuristic using coordinates
            var from = _pointsById[fromId];
            var to = _pointsById[toId];

            double dx = to.x - from.x;
            double dy = to.y - from.y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double GetDistance(ushort fromId, ushort toId)
        {
            // Actual edge cost (Euclidean distance)
            return Heuristic(fromId, toId);
        }
    }
}
