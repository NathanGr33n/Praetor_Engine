using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PraetorEngine.Core.ECS;
using PraetorEngine.Core.ECS.Components;
using PraetorEngine.Core.Memory;
using PraetorEngine.Core.Rendering;
using PraetorEngine.Core.Profiling;
using PraetorEngine.World;
using PraetorEngine.Systems;
using PraetorEngine.Systems.Pathfinding;
using PraetorEngine.Systems.Movement;

namespace PraetorEngine
{
    /// <summary>
    /// Phase 2 integration demo - showcases all map, pathfinding, and simulation systems.
    /// </summary>
    public static class Phase2Demo
    {
        public static void RunDemo(GraphicsDevice graphicsDevice)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phase2_demo_results.txt");
            using (var writer = new StreamWriter(logPath))
            {
                var originalOut = Console.Out;
                Console.SetOut(writer);

                try
                {
                    Console.WriteLine("=== Phase 2 Integration Demo ===\n");

                    DemoTerrainSystem();
                    DemoChunkSystem();
                    DemoPathfinding();
                    DemoMovementSystem();
                    DemoSettlementSystem();
                    DemoTurnManager();
                    DemoIntegratedSystems(graphicsDevice);

                    Console.WriteLine("\n=== Phase 2 Demo Complete ===");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }

            Console.WriteLine($"Phase 2 demo completed. Results: {logPath}");
        }

        private static void DemoTerrainSystem()
        {
            Console.WriteLine("Demo: Terrain System");

            // Generate terrain samples
            var sampleHexes = new HexCoord[]
            {
                new HexCoord(0, 0),
                new HexCoord(5, 5),
                new HexCoord(10, 10),
                new HexCoord(20, 20)
            };

            Console.WriteLine("  Terrain Generation:");
            foreach (var hex in sampleHexes)
            {
                var terrain = TerrainGenerator.GenerateTerrainAt(hex, seed: 12345);
                var elevation = TerrainGenerator.GenerateElevation(hex, terrain, seed: 12345);
                var moveCost = TerrainData.GetMovementCost(terrain);
                var isPassable = TerrainData.IsPassable(terrain);

                Console.WriteLine($"    {hex}: {TerrainData.GetTerrainName(terrain)} " +
                    $"(Elev: {elevation}, Cost: {moveCost}, Passable: {isPassable})");
            }

            Console.WriteLine();
        }

        private static void DemoChunkSystem()
        {
            Console.WriteLine("Demo: Chunk System");

            var profiler = new Profiler();
            var world = new Core.ECS.World();
            var chunkManager = new ChunkManager(world, mapSeed: 12345);

            profiler.BeginFrame();

            // Load chunks in a region
            using (profiler.Scope("LoadChunks"))
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        var coord = new ChunkCoord(x, y);
                        chunkManager.LoadChunk(coord);
                    }
                }
            }

            profiler.EndFrame();

            int totalTiles = chunkManager.LoadedChunkCount * MapChunk.ChunkSize * MapChunk.ChunkSize;
            Console.WriteLine($"  Loaded {chunkManager.LoadedChunkCount} chunks ({totalTiles} hex tiles)");
            Console.WriteLine($"  {profiler.GetSection("LoadChunks")}");
            Console.WriteLine($"  Total entities: {world.EntityCount}");

            // Test terrain lookup
            var testHex = new HexCoord(10, 10);
            var terrain = chunkManager.GetTerrainAt(testHex);
            if (terrain.HasValue)
            {
                Console.WriteLine($"  Terrain at {testHex}: {TerrainData.GetTerrainName(terrain.Value.Type)}");
            }

            chunkManager.Clear();
            Console.WriteLine($"  Cleanup: {world.EntityCount} entities remaining\n");
        }

        private static void DemoPathfinding()
        {
            Console.WriteLine("Demo: Pathfinding System");

            var profiler = new Profiler();
            var world = new Core.ECS.World();
            var chunkManager = new ChunkManager(world, mapSeed: 12345);
            var pathfinding = new PathfindingSystem(chunkManager);

            // Load map region
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    chunkManager.LoadChunk(new ChunkCoord(x, y));
                }
            }

            // Test pathfinding with different distances
            var testCases = new[]
            {
                (start: new HexCoord(0, 0), goal: new HexCoord(5, 5)),
                (start: new HexCoord(10, 10), goal: new HexCoord(30, 30)),
                (start: new HexCoord(0, 0), goal: new HexCoord(60, 60))
            };

            profiler.BeginFrame();

            foreach (var (start, goal) in testCases)
            {
                int distance = HexGrid.Distance(start, goal);
                
                using (profiler.Scope($"Pathfind_{distance}"))
                {
                    var request = pathfinding.CreateRequest();
                    request.Configure(start, goal);
                    pathfinding.FindPath(request);

                    Console.WriteLine($"  Path from {start} to {goal} (distance: {distance}):");
                    Console.WriteLine($"    Success: {request.Success}");
                    Console.WriteLine($"    Path length: {request.Path.Count}");
                    Console.WriteLine($"    Nodes explored: {request.NodesExplored}");

                    pathfinding.ReturnRequest(request);
                }
            }

            profiler.EndFrame();

            Console.WriteLine($"  Performance:");
            foreach (var (start, goal) in testCases)
            {
                int distance = HexGrid.Distance(start, goal);
                var section = profiler.GetSection($"Pathfind_{distance}");
                Console.WriteLine($"    {section}");
            }

            chunkManager.Clear();
            Console.WriteLine();
        }

        private static void DemoMovementSystem()
        {
            Console.WriteLine("Demo: Movement System");

            var world = new Core.ECS.World();
            var chunkManager = new ChunkManager(world, mapSeed: 12345);
            var pathfinding = new PathfindingSystem(chunkManager);
            var movement = new MovementSystem(world, pathfinding, chunkManager);

            // Load map
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    chunkManager.LoadChunk(new ChunkCoord(x, y));
                }
            }

            // Create a unit entity
            var startHex = new HexCoord(5, 5);
            var unit = world.CreateEntity();
            var worldPos = HexGrid.HexToWorld(startHex);
            world.AddComponent(unit, new PositionComponent(worldPos.X, worldPos.Y));
            world.AddComponent(unit, new MovementComponent(maxMovementPoints: 100, startHex));

            Console.WriteLine($"  Unit created at {startHex}");

            // Command unit to move
            var targetHex = new HexCoord(15, 15);
            bool canMove = movement.CommandMove(unit, targetHex);
            Console.WriteLine($"  Command move to {targetHex}: {(canMove ? "Success" : "Failed")}");

            if (canMove)
            {
                ref var movementComp = ref world.GetComponent<MovementComponent>(unit);
                Console.WriteLine($"  Initial movement points: {movementComp.MovementPoints}/{movementComp.MaxMovementPoints}");

                // Simulate a few movement steps
                for (int step = 0; step < 5 && movementComp.IsMoving; step++)
                {
                    movement.ExecuteMovement(unit);
                    movementComp = ref world.GetComponent<MovementComponent>(unit);
                    Console.WriteLine($"    Step {step + 1}: At {movementComp.CurrentHex}, " +
                        $"MP: {movementComp.MovementPoints}, Moving: {movementComp.IsMoving}");
                }

                // Test movement range
                var range = movement.GetMovementRange(unit);
                Console.WriteLine($"  Movement range: {range.Count} reachable hexes");
            }

            chunkManager.Clear();
            Console.WriteLine();
        }

        private static void DemoSettlementSystem()
        {
            Console.WriteLine("Demo: Settlement System");

            var world = new Core.ECS.World();

            // Create settlements
            var settlements = new[]
            {
                (name: "Ironforge", hex: new HexCoord(10, 10), type: SettlementType.City),
                (name: "Riverside", hex: new HexCoord(20, 15), type: SettlementType.Town),
                (name: "Fort Stone", hex: new HexCoord(5, 30), type: SettlementType.Fortress)
            };

            Console.WriteLine("  Created settlements:");
            foreach (var (name, hex, type) in settlements)
            {
                var entity = world.CreateEntity();
                var population = SettlementHelper.GeneratePopulation(type, seed: hex.GetHashCode());
                var settlement = new SettlementComponent(name, hex, population, ownerId: 1, type);
                
                world.AddComponent(entity, settlement);
                
                // Add position for rendering
                var worldPos = HexGrid.HexToWorld(hex);
                world.AddComponent(entity, new PositionComponent(worldPos.X, worldPos.Y));

                var comp = world.GetComponent<SettlementComponent>(entity);
                Console.WriteLine($"    {comp.GetName()} at {hex}: " +
                    $"{comp.GetSizeDescription()} (Pop: {comp.Population:N0})");
            }

            // Generate random settlement names
            Console.WriteLine("\n  Random settlement names:");
            for (int i = 0; i < 5; i++)
            {
                var name = SettlementHelper.GenerateSettlementName(seed: i);
                Console.WriteLine($"    - {name}");
            }

            Console.WriteLine();
        }

        private static void DemoTurnManager()
        {
            Console.WriteLine("Demo: Turn Manager");

            var turnManager = new TurnManager(startingTurn: 1);
            var turnLog = new List<string>();

            // Subscribe to events
            turnManager.TurnStarted += (sender, e) =>
            {
                turnLog.Add($"Turn {e.TurnNumber} started");
            };

            turnManager.TurnEnded += (sender, e) =>
            {
                turnLog.Add($"Turn {e.TurnNumber} ended");
            };

            turnManager.PhaseChanged += (sender, e) =>
            {
                turnLog.Add($"Phase changed: {turnManager.GetTurnDisplay()}");
            };

            Console.WriteLine($"  Initial: {turnManager.GetTurnDisplay()}");

            // Advance through phases and turns
            for (int i = 0; i < 8; i++)
            {
                turnManager.NextPhase();
            }

            Console.WriteLine($"  After 8 phase advances: {turnManager.GetTurnDisplay()}");
            Console.WriteLine("\n  Event log:");
            foreach (var log in turnLog)
            {
                Console.WriteLine($"    {log}");
            }

            Console.WriteLine();
        }

        private static void DemoIntegratedSystems(GraphicsDevice graphicsDevice)
        {
            Console.WriteLine("Demo: Fully Integrated System Test");

            var profiler = new Profiler();
            var world = new Core.ECS.World();
            var chunkManager = new ChunkManager(world, mapSeed: 54321);
            var pathfinding = new PathfindingSystem(chunkManager);
            var movementSystem = new MovementSystem(world, pathfinding, chunkManager);
            var turnManager = new TurnManager();
            var camera = new Camera2D(graphicsDevice.Viewport);

            profiler.BeginFrame();

            // Generate a 64x64 map (4x4 chunks)
            using (profiler.Scope("GenerateMap"))
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        chunkManager.LoadChunk(new ChunkCoord(x, y));
                    }
                }
            }

            int totalHexes = 4 * 4 * MapChunk.ChunkSize * MapChunk.ChunkSize;
            Console.WriteLine($"  Generated {totalHexes} hex tiles (64x64 map)");

            // Place settlements
            using (profiler.Scope("PlaceSettlements"))
            {
                var settlementLocations = new[]
                {
                    new HexCoord(16, 16),
                    new HexCoord(32, 32),
                    new HexCoord(48, 16),
                    new HexCoord(16, 48)
                };

                foreach (var loc in settlementLocations)
                {
                    var entity = world.CreateEntity();
                    var name = SettlementHelper.GenerateSettlementName(loc.GetHashCode());
                    var type = SettlementType.City;
                    var pop = SettlementHelper.GeneratePopulation(type, loc.GetHashCode());
                    
                    world.AddComponent(entity, new SettlementComponent(name, loc, pop, 0, type));
                    var worldPos = HexGrid.HexToWorld(loc);
                    world.AddComponent(entity, new PositionComponent(worldPos.X, worldPos.Y));
                }

                Console.WriteLine($"  Placed {settlementLocations.Length} settlements");
            }

            // Create units with movement
            using (profiler.Scope("CreateUnits"))
            {
                for (int i = 0; i < 100; i++)
                {
                    var hex = new HexCoord(i % 32 + 16, i / 32 + 16);
                    var entity = world.CreateEntity();
                    var worldPos = HexGrid.HexToWorld(hex);
                    
                    world.AddComponent(entity, new PositionComponent(worldPos.X, worldPos.Y));
                    world.AddComponent(entity, new MovementComponent(100, hex));
                }

                Console.WriteLine($"  Created 100 units with movement");
            }

            // Test chunk streaming with camera
            using (profiler.Scope("ChunkStreaming"))
            {
                camera.Position = new Vector2(512, 512); // Center of map
                var viewBounds = camera.ViewBounds;
                chunkManager.UpdateStreaming(viewBounds, loadDistance: 2);
                
                Console.WriteLine($"  Camera at {camera.Position}, " +
                    $"loaded chunks: {chunkManager.LoadedChunkCount}");
            }

            // Pathfinding performance test
            using (profiler.Scope("PathfindingBatch"))
            {
                for (int i = 0; i < 10; i++)
                {
                    var start = new HexCoord(i * 5, i * 5);
                    var goal = new HexCoord(60 - i * 5, 60 - i * 5);
                    
                    var request = pathfinding.CreateRequest();
                    request.Configure(start, goal);
                    pathfinding.FindPath(request);
                    pathfinding.ReturnRequest(request);
                }
            }

            // Simulate turns
            using (profiler.Scope("TurnSimulation"))
            {
                for (int turn = 0; turn < 5; turn++)
                {
                    turnManager.NextTurn();
                }
                
                Console.WriteLine($"  Simulated 5 turns, current: {turnManager.CurrentTurn}");
            }

            profiler.EndFrame();

            // Performance summary
            Console.WriteLine("\n  === Performance Results ===");
            Console.WriteLine($"  {profiler.GetSection("GenerateMap")}");
            Console.WriteLine($"  {profiler.GetSection("PlaceSettlements")}");
            Console.WriteLine($"  {profiler.GetSection("CreateUnits")}");
            Console.WriteLine($"  {profiler.GetSection("ChunkStreaming")}");
            Console.WriteLine($"  {profiler.GetSection("PathfindingBatch")}");
            Console.WriteLine($"  {profiler.GetSection("TurnSimulation")}");
            Console.WriteLine($"  Total frame time: {profiler.CurrentFrameTimeMs:F2}ms");
            Console.WriteLine($"  Total entities: {world.EntityCount}");
            
            bool meetsTarget = profiler.CurrentFrameTimeMs < profiler.TargetFrameTimeMs;
            Console.WriteLine($"  Meets 60 FPS target: {(meetsTarget ? "✓" : "✗")}");

            // Memory allocation test
            var stats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var request = pathfinding.CreateRequest();
                    request.Configure(new HexCoord(0, 0), new HexCoord(10, 10));
                    pathfinding.FindPath(request);
                    pathfinding.ReturnRequest(request);
                }
            });

            Console.WriteLine($"\n  === Allocation Test (100 pathfinding operations) ===");
            Console.WriteLine($"  {stats}");

            chunkManager.Clear();
        }
    }
}
