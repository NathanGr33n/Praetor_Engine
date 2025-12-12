using System;
using System.Collections.Generic;
using PraetorEngine.Core.ECS;
using PraetorEngine.Systems.Pathfinding;
using PraetorEngine.World;

namespace PraetorEngine.Systems.Movement
{
    /// <summary>
    /// System for processing unit movement in turn-based manner.
    /// Integrates with pathfinding and terrain systems.
    /// </summary>
    public class MovementSystem
    {
        private readonly Core.ECS.World _world;
        private readonly PathfindingSystem _pathfinding;
        private readonly ChunkManager _chunkManager;

        public MovementSystem(Core.ECS.World world, PathfindingSystem pathfinding, ChunkManager chunkManager)
        {
            _world = world;
            _pathfinding = pathfinding;
            _chunkManager = chunkManager;
        }

        /// <summary>
        /// Commands an entity to move to a target hex.
        /// Calculates path and validates movement points.
        /// </summary>
        public bool CommandMove(Entity entity, HexCoord target)
        {
            if (!_world.HasComponent<MovementComponent>(entity))
                return false;

            ref var movement = ref _world.GetComponent<MovementComponent>(entity);

            // Check if already at target
            if (movement.CurrentHex == target)
                return false;

            // Find path
            var request = _pathfinding.CreateRequest();
            request.Configure(movement.CurrentHex, target, maxDepth: 100, ignoreTerrain: false);
            _pathfinding.FindPath(request);

            if (!request.Success || request.Path.Count < 2)
            {
                _pathfinding.ReturnRequest(request);
                return false; // No path found
            }

            // Calculate total movement cost
            int totalCost = CalculatePathCost(request.Path);

            // Check if entity has enough movement points
            if (!movement.CanAffordMovement(totalCost))
            {
                _pathfinding.ReturnRequest(request);
                return false; // Not enough movement points
            }

            // Set target and mark as moving
            movement.TargetHex = target;
            movement.IsMoving = true;

            _pathfinding.ReturnRequest(request);
            return true;
        }

        /// <summary>
        /// Executes movement for an entity along its path.
        /// Call this each turn to advance movement.
        /// </summary>
        public void ExecuteMovement(Entity entity)
        {
            if (!_world.HasComponent<MovementComponent>(entity))
                return;

            ref var movement = ref _world.GetComponent<MovementComponent>(entity);

            if (!movement.IsMoving || movement.CurrentHex == movement.TargetHex)
            {
                movement.IsMoving = false;
                return;
            }

            // Find path to target
            var request = _pathfinding.CreateRequest();
            request.Configure(movement.CurrentHex, movement.TargetHex);
            _pathfinding.FindPath(request);

            if (!request.Success || request.Path.Count < 2)
            {
                movement.IsMoving = false;
                _pathfinding.ReturnRequest(request);
                return;
            }

            // Get next step (index 1, since 0 is current position)
            var nextHex = request.Path[1];
            var terrain = _chunkManager.GetTerrainAt(nextHex);
            int moveCost = terrain.HasValue ? TerrainData.GetMovementCost(terrain.Value.Type) : 10;

            // Check if we can afford this move
            if (movement.CanAffordMovement(moveCost))
            {
                // Execute move
                movement.CurrentHex = nextHex;
                movement.ConsumeMovement(moveCost);

                // Update position component for rendering
                if (_world.HasComponent<Core.ECS.Components.PositionComponent>(entity))
                {
                    var worldPos = HexGrid.HexToWorld(nextHex);
                    ref var position = ref _world.GetComponent<Core.ECS.Components.PositionComponent>(entity);
                    position.Position = worldPos;
                }

                // Check if reached target
                if (movement.CurrentHex == movement.TargetHex)
                {
                    movement.IsMoving = false;
                }
            }
            else
            {
                // Out of movement points
                movement.IsMoving = false;
            }

            _pathfinding.ReturnRequest(request);
        }

        /// <summary>
        /// Calculates total movement cost for a path.
        /// </summary>
        private int CalculatePathCost(List<HexCoord> path)
        {
            int totalCost = 0;

            // Skip first hex (starting position)
            for (int i = 1; i < path.Count; i++)
            {
                var terrain = _chunkManager.GetTerrainAt(path[i]);
                int cost = terrain.HasValue ? TerrainData.GetMovementCost(terrain.Value.Type) : 10;
                totalCost += cost;
            }

            return totalCost;
        }

        /// <summary>
        /// Resets movement points for an entity (called on new turn).
        /// </summary>
        public void ResetMovementPoints(Entity entity)
        {
            if (_world.HasComponent<MovementComponent>(entity))
            {
                ref var movement = ref _world.GetComponent<MovementComponent>(entity);
                movement.ResetMovementPoints();
            }
        }

        /// <summary>
        /// Resets movement points for all entities with movement component.
        /// </summary>
        public void ResetAllMovementPoints()
        {
            // In a full implementation, this would iterate all entities with MovementComponent
            // For Phase 2, this is a placeholder for the turn manager to call
        }

        /// <summary>
        /// Gets the remaining movement range for an entity.
        /// Returns list of reachable hexes within movement points.
        /// </summary>
        public List<HexCoord> GetMovementRange(Entity entity)
        {
            var reachable = new List<HexCoord>();

            if (!_world.HasComponent<MovementComponent>(entity))
                return reachable;

            ref var movement = ref _world.GetComponent<MovementComponent>(entity);
            
            // Simple flood-fill to find reachable hexes
            var visited = new HashSet<HexCoord>();
            var queue = new Queue<(HexCoord hex, int costRemaining)>();
            
            queue.Enqueue((movement.CurrentHex, movement.MovementPoints));
            visited.Add(movement.CurrentHex);

            while (queue.Count > 0)
            {
                var (currentHex, costRemaining) = queue.Dequeue();
                reachable.Add(currentHex);

                // Check all neighbors
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = HexGrid.GetNeighbor(currentHex, dir);

                    if (visited.Contains(neighbor))
                        continue;

                    var terrain = _chunkManager.GetTerrainAt(neighbor);
                    if (terrain.HasValue && !TerrainData.IsPassable(terrain.Value.Type))
                        continue;

                    int moveCost = terrain.HasValue ? TerrainData.GetMovementCost(terrain.Value.Type) : 10;
                    int newCostRemaining = costRemaining - moveCost;

                    if (newCostRemaining >= 0)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, newCostRemaining));
                    }
                }
            }

            return reachable;
        }
    }
}
