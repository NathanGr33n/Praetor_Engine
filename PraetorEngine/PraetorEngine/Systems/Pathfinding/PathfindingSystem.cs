using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PraetorEngine.Core.Memory;
using PraetorEngine.World;

namespace PraetorEngine.Systems.Pathfinding
{
    /// <summary>
    /// Node used in A* pathfinding algorithm.
    /// </summary>
    internal class PathNode
    {
        public HexCoord Coord;
        public PathNode? Parent;
        public int GCost; // Distance from start
        public int HCost; // Heuristic distance to goal
        public int FCost => GCost + HCost;

        public void Reset()
        {
            Coord = default;
            Parent = null;
            GCost = 0;
            HCost = 0;
        }
    }

    /// <summary>
    /// A* pathfinding system optimized for hexagonal grids.
    /// </summary>
    public class PathfindingSystem
    {
        private readonly ChunkManager _chunkManager;
        private readonly ObjectPool<PathRequest> _requestPool;
        private readonly ObjectPool<PathNode> _nodePool;
        
        // Reusable collections to avoid allocations
        private readonly List<PathNode> _openSet;
        private readonly Dictionary<HexCoord, PathNode> _allNodes;
        private readonly HashSet<HexCoord> _closedSet;

        public PathfindingSystem(ChunkManager chunkManager)
        {
            _chunkManager = chunkManager;
            
            // Pool requests to avoid allocations
            _requestPool = new ObjectPool<PathRequest>(
                () => new PathRequest(),
                req => req.Reset(),
                preAllocate: 8
            );

            // Pool nodes for A* algorithm
            _nodePool = new ObjectPool<PathNode>(
                () => new PathNode(),
                node => node.Reset(),
                preAllocate: 256
            );

            // Reusable collections
            _openSet = new List<PathNode>(256);
            _allNodes = new Dictionary<HexCoord, PathNode>(256);
            _closedSet = new HashSet<HexCoord>();
        }

        /// <summary>
        /// Creates a pooled path request.
        /// Must be returned via ReturnRequest after use.
        /// </summary>
        public PathRequest CreateRequest()
        {
            return _requestPool.Rent();
        }

        /// <summary>
        /// Returns a request to the pool.
        /// </summary>
        public void ReturnRequest(PathRequest request)
        {
            _requestPool.Return(request);
        }

        /// <summary>
        /// Executes pathfinding request using A* algorithm.
        /// </summary>
        public void FindPath(PathRequest request)
        {
            // Clear reusable collections
            _openSet.Clear();
            _allNodes.Clear();
            _closedSet.Clear();

            // Return nodes from previous search to pool
            foreach (var node in _allNodes.Values)
            {
                _nodePool.Return(node);
            }

            request.Success = false;
            request.NodesExplored = 0;
            request.Path.Clear();

            // Validate start and goal
            if (request.Start == request.Goal)
            {
                request.Path.Add(request.Start);
                request.Success = true;
                return;
            }

            // Check if goal is passable
            if (!request.IgnoreTerrain && !IsPassable(request.Goal))
            {
                return; // Cannot path to impassable tile
            }

            // Initialize start node
            var startNode = GetOrCreateNode(request.Start);
            startNode.GCost = 0;
            startNode.HCost = HexGrid.Distance(request.Start, request.Goal);
            _openSet.Add(startNode);

            while (_openSet.Count > 0)
            {
                // Find node with lowest F cost
                var current = GetLowestFCostNode();
                _openSet.Remove(current);
                _closedSet.Add(current.Coord);
                request.NodesExplored++;

                // Check if we reached the goal
                if (current.Coord == request.Goal)
                {
                    ReconstructPath(current, request);
                    request.Success = true;
                    return;
                }

                // Stop if we've searched too far
                if (request.NodesExplored >= request.MaxSearchDepth)
                {
                    return; // Path too long or doesn't exist
                }

                // Check all neighbors
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighborCoord = HexGrid.GetNeighbor(current.Coord, dir);

                    // Skip if already evaluated
                    if (_closedSet.Contains(neighborCoord))
                        continue;

                    // Skip if impassable
                    if (!request.IgnoreTerrain && !IsPassable(neighborCoord))
                        continue;

                    // Calculate costs
                    int movementCost = GetMovementCost(neighborCoord, request.IgnoreTerrain);
                    int tentativeGCost = current.GCost + movementCost;

                    var neighbor = GetOrCreateNode(neighborCoord);
                    bool isInOpenSet = _openSet.Contains(neighbor);

                    // If this path to neighbor is better, update it
                    if (!isInOpenSet || tentativeGCost < neighbor.GCost)
                    {
                        neighbor.Parent = current;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = HexGrid.Distance(neighborCoord, request.Goal);

                        if (!isInOpenSet)
                        {
                            _openSet.Add(neighbor);
                        }
                    }
                }
            }

            // No path found
        }

        /// <summary>
        /// Checks if a hex coordinate is passable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsPassable(HexCoord coord)
        {
            var terrain = _chunkManager.GetTerrainAt(coord);
            
            // If chunk not loaded, assume passable for now
            if (terrain == null)
                return true;

            return TerrainData.IsPassable(terrain.Value.Type);
        }

        /// <summary>
        /// Gets movement cost for a hex coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMovementCost(HexCoord coord, bool ignoreTerrain)
        {
            if (ignoreTerrain)
                return 10; // Uniform cost

            var terrain = _chunkManager.GetTerrainAt(coord);
            
            // If chunk not loaded, use default cost
            if (terrain == null)
                return 10;

            return TerrainData.GetMovementCost(terrain.Value.Type);
        }

        /// <summary>
        /// Gets or creates a node for the given coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PathNode GetOrCreateNode(HexCoord coord)
        {
            if (!_allNodes.TryGetValue(coord, out var node))
            {
                node = _nodePool.Rent();
                node.Coord = coord;
                _allNodes[coord] = node;
            }
            return node;
        }

        /// <summary>
        /// Finds node with lowest F cost in open set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PathNode GetLowestFCostNode()
        {
            var lowest = _openSet[0];
            for (int i = 1; i < _openSet.Count; i++)
            {
                if (_openSet[i].FCost < lowest.FCost ||
                    (_openSet[i].FCost == lowest.FCost && _openSet[i].HCost < lowest.HCost))
                {
                    lowest = _openSet[i];
                }
            }
            return lowest;
        }

        /// <summary>
        /// Reconstructs the path by backtracking from goal to start.
        /// </summary>
        private void ReconstructPath(PathNode endNode, PathRequest request)
        {
            var path = new List<HexCoord>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.Coord);
                current = current.Parent;
            }

            path.Reverse();
            request.Path.AddRange(path);
        }
    }
}
