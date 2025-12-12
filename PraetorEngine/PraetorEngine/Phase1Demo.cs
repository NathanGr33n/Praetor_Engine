using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PraetorEngine.Core.ECS;
using PraetorEngine.Core.ECS.Components;
using PraetorEngine.Core.Memory;
using PraetorEngine.Core.Rendering;
using PraetorEngine.Core.Profiling;
using PraetorEngine.World;

namespace PraetorEngine
{
    /// <summary>
    /// Phase 1 integration demo - showcases all implemented systems.
    /// </summary>
    public static class Phase1Demo
    {
        public static void RunDemo(GraphicsDevice graphicsDevice)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phase1_demo_results.txt");
            using (var writer = new StreamWriter(logPath))
            {
                var originalOut = Console.Out;
                Console.SetOut(writer);

                try
                {
                    Console.WriteLine("=== Phase 1 Integration Demo ===\n");

                    DemoECSWithProfiling();
                    DemoMemoryManagement();
                    DemoRenderingSystem(graphicsDevice);
                    DemoHexGrid();
                    DemoIntegratedSystems();

                    Console.WriteLine("\n=== Phase 1 Demo Complete ===");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }

            Console.WriteLine($"Phase 1 demo completed. Results: {logPath}");
        }

        private static void DemoECSWithProfiling()
        {
            Console.WriteLine("Demo: ECS + Profiling Integration");

            var profiler = new Profiler();
            var world = new Core.ECS.World();

            profiler.BeginFrame();

            // Create entities with profiling
            using (profiler.Scope("CreateEntities"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    var entity = world.CreateEntity();
                    world.AddComponent(entity, new PositionComponent(i, i));
                    if (i % 2 == 0)
                        world.AddComponent(entity, new VelocityComponent(1, 1));
                }
            }

            // Update entities
            using (profiler.Scope("UpdateEntities"))
            {
                // Simulate game logic
                for (int frame = 0; frame < 10; frame++)
                {
                    // Would iterate entities here
                }
            }

            profiler.EndFrame();

            var createSection = profiler.GetSection("CreateEntities");
            var updateSection = profiler.GetSection("UpdateEntities");
            
            Console.WriteLine($"  {createSection}");
            Console.WriteLine($"  {updateSection}");
            Console.WriteLine($"  Frame time: {profiler.CurrentFrameTimeMs:F2}ms\n");
        }

        private static void DemoMemoryManagement()
        {
            Console.WriteLine("Demo: Memory Management");

            var pool = new ObjectPool<System.Collections.Generic.List<int>>(
                () => new System.Collections.Generic.List<int>(),
                list => list.Clear(),
                preAllocate: 10
            );

            var stats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var list = pool.Rent();
                    list.Add(i);
                    pool.Return(list);
                }
            });

            Console.WriteLine($"  Pooled operations: {stats}");
            Console.WriteLine($"  Pool efficiency demonstrated\n");
        }

        private static void DemoRenderingSystem(GraphicsDevice graphicsDevice)
        {
            Console.WriteLine("Demo: Rendering System");

            var camera = new Camera2D(graphicsDevice.Viewport);
            var smoothCamera = new SmoothCamera2D(camera);

            // Simulate camera movement
            smoothCamera.SetTargetPosition(new Vector2(100, 100));
            smoothCamera.SetTargetZoom(1.5f);

            for (int i = 0; i < 60; i++) // 1 second at 60 FPS
            {
                smoothCamera.Update(1.0f / 60.0f);
            }

            var viewBounds = camera.ViewBounds;
            Console.WriteLine($"  Camera moved to: {camera.Position}");
            Console.WriteLine($"  View bounds: {viewBounds.Width}x{viewBounds.Height}");
            Console.WriteLine($"  Smooth interpolation complete\n");
        }

        private static void DemoHexGrid()
        {
            Console.WriteLine("Demo: Hexagonal Grid");

            var hex1 = new HexCoord(0, 0);
            var hex2 = new HexCoord(3, 4);

            var pos1 = HexGrid.HexToWorld(hex1);
            var pos2 = HexGrid.HexToWorld(hex2);
            var backToHex = HexGrid.WorldToHex(pos2);

            int distance = HexGrid.Distance(hex1, hex2);
            var neighbors = HexGrid.GetNeighbors(hex1);

            Console.WriteLine($"  Hex {hex1} → World {pos1}");
            Console.WriteLine($"  Hex {hex2} → World {pos2} → Hex {backToHex}");
            Console.WriteLine($"  Distance: {distance}");
            Console.WriteLine($"  Neighbors: {neighbors.Length}\n");
        }

        private static void DemoIntegratedSystems()
        {
            Console.WriteLine("Demo: Integrated System Test");

            var profiler = new Profiler();
            var world = new Core.ECS.World();
            
            // Performance test: Create hex-based entities with pooled arrays
            profiler.BeginFrame();

            using (profiler.Scope("HexEntityCreation"))
            {
                PooledArrayHelper.WithPooledSpan<HexCoord>(100, hexes =>
                {
                    for (int i = 0; i < hexes.Length; i++)
                    {
                        hexes[i] = new HexCoord(i % 10, i / 10);
                        var worldPos = HexGrid.HexToWorld(hexes[i]);
                        
                        var entity = world.CreateEntity();
                        world.AddComponent(entity, new PositionComponent(worldPos.X, worldPos.Y));
                    }
                });
            }

            profiler.EndFrame();

            Console.WriteLine($"  Created 100 hex-based entities");
            Console.WriteLine($"  {profiler.GetSection("HexEntityCreation")}");
            Console.WriteLine($"  Total frame time: {profiler.CurrentFrameTimeMs:F2}ms");
            Console.WriteLine($"  Entity count: {world.EntityCount}");
            
            // Performance validation
            bool meetsTarget = profiler.CurrentFrameTimeMs < profiler.TargetFrameTimeMs;
            Console.WriteLine($"  Meets 60 FPS target: {(meetsTarget ? "✓" : "✗")}");
        }
    }
}
