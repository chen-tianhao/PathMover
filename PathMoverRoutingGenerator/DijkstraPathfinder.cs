using System;
using System.Collections.Generic;
using System.Linq;

namespace PathMoverRoutingGenerator
{
    /// <summary>
    /// Dijkstra pathfinding for efficient complete routing table generation.
    /// Uses reverse Dijkstra from each destination to find shortest paths from ALL nodes at once.
    /// This is O(D * (V + E) log V) vs O(D * S * (V + E) log V) for A* per pair,
    /// where D=destinations, S=sources, V=vertices, E=edges.
    /// Uses integer IDs for maximum performance and minimal memory usage.
    /// </summary>
    public class DijkstraPathfinder
    {
        private Dictionary<ushort, ControlPointData> _pointsById;
        private Dictionary<ushort, List<ushort>> _forwardAdjacency;  // node -> next nodes
        private Dictionary<ushort, List<ushort>> _reverseAdjacency;  // node -> previous nodes (for reverse Dijkstra)
        private Dictionary<string, ushort> _nameToId;

        public DijkstraPathfinder(List<ControlPointData> points, Dictionary<string, ushort> nameToId)
        {
            _nameToId = nameToId;
            _pointsById = new Dictionary<ushort, ControlPointData>();
            _forwardAdjacency = new Dictionary<ushort, List<ushort>>();
            _reverseAdjacency = new Dictionary<ushort, List<ushort>>();

            // Build adjacency lists using integer IDs
            foreach (var point in points)
            {
                ushort id = nameToId[point.id];
                _pointsById[id] = point;

                if (!_forwardAdjacency.ContainsKey(id))
                    _forwardAdjacency[id] = new List<ushort>();

                if (point.next != null && point.next.Count > 0)
                {
                    foreach (var nextName in point.next)
                    {
                        ushort nextId = nameToId[nextName];
                        _forwardAdjacency[id].Add(nextId);

                        // Build reverse adjacency (for reverse Dijkstra)
                        if (!_reverseAdjacency.ContainsKey(nextId))
                            _reverseAdjacency[nextId] = new List<ushort>();
                        _reverseAdjacency[nextId].Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Run reverse Dijkstra from a destination to find shortest paths from ALL nodes to that destination.
        /// Returns a dictionary mapping each reachable node to its next hop toward the destination.
        /// </summary>
        /// <param name="destination">The destination node ID</param>
        /// <returns>Dictionary of (nodeId -> nextHopTowardDestination)</returns>
        public Dictionary<ushort, ushort> ComputeRoutesToDestination(ushort destination)
        {
            if (!_pointsById.ContainsKey(destination))
                return new Dictionary<ushort, ushort>();

            // Distance from each node to destination (reverse direction)
            var dist = new Dictionary<ushort, double>();
            // Next hop toward destination (the node you go to from current node)
            var nextHop = new Dictionary<ushort, ushort>();
            // Priority queue: (distance, nodeId)
            var pq = new SortedSet<(double dist, ushort id)>(
                Comparer<(double dist, ushort id)>.Create((a, b) =>
                {
                    int cmp = a.dist.CompareTo(b.dist);
                    return cmp != 0 ? cmp : a.id.CompareTo(b.id);
                }));

            // Initialize destination
            dist[destination] = 0;
            pq.Add((0, destination));

            while (pq.Count > 0)
            {
                var (currentDist, currentId) = pq.Min;
                pq.Remove(pq.Min);

                // Skip if we've already found a better path
                if (dist.ContainsKey(currentId) && currentDist > dist[currentId])
                    continue;

                // Explore predecessors (reverse edges)
                if (_reverseAdjacency.ContainsKey(currentId))
                {
                    foreach (var predId in _reverseAdjacency[currentId])
                    {
                        // Edge weight is the Euclidean distance from pred to current
                        double edgeWeight = GetDistance(predId, currentId);
                        double newDist = currentDist + edgeWeight;

                        if (!dist.ContainsKey(predId) || newDist < dist[predId])
                        {
                            // Remove old entry if exists
                            if (dist.ContainsKey(predId))
                                pq.Remove((dist[predId], predId));

                            dist[predId] = newDist;
                            // From predId, the next hop toward destination is currentId
                            nextHop[predId] = currentId;
                            pq.Add((newDist, predId));
                        }
                    }
                }
            }

            return nextHop;
        }

        /// <summary>
        /// Generate complete routing table for all entry/exit point destinations.
        /// Much faster than A* per pair: O(D * graph traversal) vs O(D * S * graph traversal)
        /// Uses integer IDs for maximum performance.
        /// </summary>
        /// <param name="entryExitPointNames">List of entry/exit point names (destinations)</param>
        /// <param name="progressCallback">Optional callback for progress reporting</param>
        /// <returns>Dictionary of routing entries ((from, dest) -> nextHop) using integer IDs</returns>
        public Dictionary<(ushort, ushort), ushort> GenerateCompleteRoutingTable(
            List<string> entryExitPointNames,
            Action<int, int, int>? progressCallback = null)
        {
            var routingTable = new Dictionary<(ushort, ushort), ushort>();
            int totalDestinations = entryExitPointNames.Count;
            int processed = 0;

            foreach (var destinationName in entryExitPointNames)
            {
                ushort destination = _nameToId[destinationName];
                
                // Run reverse Dijkstra from this destination
                var routesToDest = ComputeRoutesToDestination(destination);

                // Add all routes to this destination
                foreach (var kvp in routesToDest)
                {
                    ushort from = kvp.Key;
                    ushort nextHop = kvp.Value;
                    routingTable[(from, destination)] = nextHop;
                }

                processed++;

                // Report progress every 100 destinations or at milestones
                if (progressCallback != null && (processed % 100 == 0 || processed == totalDestinations))
                {
                    progressCallback(processed, totalDestinations, routingTable.Count);
                }
            }

            return routingTable;
        }

        /// <summary>
        /// Generate routing table for specific origin-destination pairs (for random sampling mode)
        /// Uses Dijkstra for individual pairs
        /// </summary>
        public List<ushort>? FindPath(ushort startId, ushort goalId)
        {
            if (startId == goalId)
                return new List<ushort> { startId };

            if (!_pointsById.ContainsKey(startId) || !_pointsById.ContainsKey(goalId))
                return null;

            var dist = new Dictionary<ushort, double>();
            var prev = new Dictionary<ushort, ushort>();
            var pq = new SortedSet<(double dist, ushort id)>(
                Comparer<(double dist, ushort id)>.Create((a, b) =>
                {
                    int cmp = a.dist.CompareTo(b.dist);
                    return cmp != 0 ? cmp : a.id.CompareTo(b.id);
                }));

            dist[startId] = 0;
            pq.Add((0, startId));

            while (pq.Count > 0)
            {
                var (currentDist, currentId) = pq.Min;
                pq.Remove(pq.Min);

                if (currentId == goalId)
                {
                    // Reconstruct path
                    var path = new List<ushort>();
                    ushort node = goalId;
                    while (true)
                    {
                        path.Add(node);
                        if (!prev.ContainsKey(node))
                            break;
                        node = prev[node];
                    }
                    path.Reverse();
                    return path;
                }

                if (currentDist > dist[currentId])
                    continue;

                if (_forwardAdjacency.ContainsKey(currentId))
                {
                    foreach (var nextId in _forwardAdjacency[currentId])
                    {
                        double edgeWeight = GetDistance(currentId, nextId);
                        double newDist = currentDist + edgeWeight;

                        if (!dist.ContainsKey(nextId) || newDist < dist[nextId])
                        {
                            if (dist.ContainsKey(nextId))
                                pq.Remove((dist[nextId], nextId));

                            dist[nextId] = newDist;
                            prev[nextId] = currentId;
                            pq.Add((newDist, nextId));
                        }
                    }
                }
            }

            return null; // No path found
        }

        private double GetDistance(ushort fromId, ushort toId)
        {
            var from = _pointsById[fromId];
            var to = _pointsById[toId];
            double dx = to.x - from.x;
            double dy = to.y - from.y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
