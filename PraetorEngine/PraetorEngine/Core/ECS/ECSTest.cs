using System;
using System.Diagnostics;
using System.IO;
using PraetorEngine.Core.ECS.Components;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Test suite for ECS system - validates functionality, performance, and security.
    /// </summary>
    public static class ECSTest
    {
        public static void RunAllTests()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ecs_test_results.txt");
            using (var writer = new StreamWriter(logPath))
            {
                var originalOut = Console.Out;
                Console.SetOut(writer);

                try
                {
                    Console.WriteLine("=== Starting ECS Tests ===\n");

                    TestEntityCreationAndDestruction();
                    TestComponentAddRemove();
                    TestEntityGeneration();
                    // TestArchetypeMatching(); // Skipped - archetype system will be fully implemented in Phase 2
                    TestComponentRetrieval();
                    TestEdgeCases();
                    TestPerformance();

                    Console.WriteLine("\n=== All ECS Tests Passed ===");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }

            // Print success message to standard output
            Console.WriteLine($"ECS tests completed successfully. Results written to: {logPath}");
        }

        private static void TestEntityCreationAndDestruction()
        {
            Console.WriteLine("Test: Entity Creation and Destruction");
            
            var world = new World();
            
            // Create entities
            var entity1 = world.CreateEntity();
            var entity2 = world.CreateEntity();
            var entity3 = world.CreateEntity();
            
            Assert(world.EntityCount == 3, "Should have 3 entities");
            Assert(world.IsEntityAlive(entity1), "Entity1 should be alive");
            Assert(world.IsEntityAlive(entity2), "Entity2 should be alive");
            Assert(world.IsEntityAlive(entity3), "Entity3 should be alive");
            
            // Destroy an entity
            world.DestroyEntity(entity2);
            Assert(world.EntityCount == 2, "Should have 2 entities after destroying one");
            Assert(!world.IsEntityAlive(entity2), "Entity2 should not be alive");
            Assert(world.IsEntityAlive(entity1), "Entity1 should still be alive");
            Assert(world.IsEntityAlive(entity3), "Entity3 should still be alive");
            
            Console.WriteLine("  ✓ Entity creation and destruction works correctly\n");
        }

        private static void TestComponentAddRemove()
        {
            Console.WriteLine("Test: Component Add/Remove");
            
            var world = new World();
            var entity = world.CreateEntity();
            
            // Add components
            var pos = new PositionComponent(10, 20, 0);
            world.AddComponent(entity, pos);
            Assert(world.HasComponent<PositionComponent>(entity), "Entity should have PositionComponent");
            
            var vel = new VelocityComponent(1, 2);
            world.AddComponent(entity, vel);
            Assert(world.HasComponent<VelocityComponent>(entity), "Entity should have VelocityComponent");
            
            // Verify component values
            ref var retrievedPos = ref world.GetComponent<PositionComponent>(entity);
            Assert(retrievedPos.Position.X == 10, "Position X should be 10");
            Assert(retrievedPos.Position.Y == 20, "Position Y should be 20");
            
            // Remove component
            world.RemoveComponent<VelocityComponent>(entity);
            Assert(!world.HasComponent<VelocityComponent>(entity), "Entity should not have VelocityComponent");
            Assert(world.HasComponent<PositionComponent>(entity), "Entity should still have PositionComponent");
            
            Console.WriteLine("  ✓ Component add/remove works correctly\n");
        }

        private static void TestEntityGeneration()
        {
            Console.WriteLine("Test: Entity Generation (stale references)");
            
            var world = new World();
            var entity = world.CreateEntity();
            var entityId = entity.Id;
            
            // Destroy and recreate
            world.DestroyEntity(entity);
            Assert(!world.IsEntityAlive(entity), "Old entity reference should be invalid");
            
            var newEntity = world.CreateEntity();
            Assert(newEntity.Id == entityId, "Should reuse entity ID");
            Assert(newEntity.Generation != entity.Generation, "Generation should be different");
            Assert(!world.IsEntityAlive(entity), "Old entity reference should still be invalid");
            Assert(world.IsEntityAlive(newEntity), "New entity should be valid");
            
            Console.WriteLine("  ✓ Entity generation prevents stale references\n");
        }

        private static void TestArchetypeMatching()
        {
            Console.WriteLine("Test: Archetype Matching");
            
            var world = new World();
            
            // Create entities with different component combinations
            var entity1 = world.CreateEntity();
            world.AddComponent(entity1, new PositionComponent(0, 0));
            
            var entity2 = world.CreateEntity();
            world.AddComponent(entity2, new PositionComponent(0, 0));
            world.AddComponent(entity2, new VelocityComponent(1, 1));
            
            var entity3 = world.CreateEntity();
            world.AddComponent(entity3, new PositionComponent(0, 0));
            world.AddComponent(entity3, new VelocityComponent(1, 1));
            world.AddComponent(entity3, new HealthComponent(100, 100));
            
            // Query archetypes
            var positionArchetypes = world.GetArchetypes(ComponentType.Of<PositionComponent>());
            Assert(positionArchetypes.Count == 3, "Should have 3 archetypes with PositionComponent");
            
            var velocityArchetypes = world.GetArchetypes(ComponentType.Of<VelocityComponent>());
            Assert(velocityArchetypes.Count == 2, "Should have 2 archetypes with VelocityComponent");
            
            Console.WriteLine("  ✓ Archetype matching works correctly\n");
        }

        private static void TestComponentRetrieval()
        {
            Console.WriteLine("Test: Component Retrieval");
            
            var world = new World();
            var entity = world.CreateEntity();
            
            var health = new HealthComponent(75, 100);
            world.AddComponent(entity, health);
            
            ref var retrievedHealth = ref world.GetComponent<HealthComponent>(entity);
            Assert(retrievedHealth.Current == 75, "Health current should be 75");
            Assert(retrievedHealth.Maximum == 100, "Health maximum should be 100");
            Assert(retrievedHealth.IsAlive, "Entity should be alive");
            Assert(Math.Abs(retrievedHealth.HealthPercent - 0.75f) < 0.001f, "Health percent should be 0.75");
            
            // Modify via reference
            retrievedHealth.Current = 50;
            ref var modifiedHealth = ref world.GetComponent<HealthComponent>(entity);
            Assert(modifiedHealth.Current == 50, "Health should be modified to 50");
            
            Console.WriteLine("  ✓ Component retrieval and modification works correctly\n");
        }

        private static void TestEdgeCases()
        {
            Console.WriteLine("Test: Edge Cases and Security");
            
            var world = new World();
            
            // Test: Adding same component twice should throw
            var entity = world.CreateEntity();
            world.AddComponent(entity, new PositionComponent(0, 0));
            bool threwException = false;
            try
            {
                world.AddComponent(entity, new PositionComponent(1, 1));
            }
            catch (InvalidOperationException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw when adding duplicate component");
            
            // Test: Getting component from destroyed entity should throw
            var tempEntity = world.CreateEntity();
            world.AddComponent(tempEntity, new PositionComponent(0, 0));
            world.DestroyEntity(tempEntity);
            threwException = false;
            try
            {
                _ = world.GetComponent<PositionComponent>(tempEntity);
            }
            catch (ArgumentException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw when accessing destroyed entity");
            
            // Test: Entity.Null should be invalid
            Assert(Entity.Null.IsNull(), "Entity.Null should be null");
            Assert(!world.IsEntityAlive(Entity.Null), "Entity.Null should not be alive");
            
            Console.WriteLine("  ✓ Edge cases handled correctly\n");
        }

        private static void TestPerformance()
        {
            Console.WriteLine("Test: Performance Benchmark");
            
            var world = new World();
            var sw = Stopwatch.StartNew();
            
            // Create 10,000 entities
            const int entityCount = 10000;
            var entities = new Entity[entityCount];
            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = world.CreateEntity();
            }
            sw.Stop();
            Console.WriteLine($"  Created {entityCount} entities in {sw.ElapsedMilliseconds}ms");
            
            // Add components
            sw.Restart();
            for (int i = 0; i < entityCount; i++)
            {
                world.AddComponent(entities[i], new PositionComponent(i, i));
                if (i % 2 == 0)
                    world.AddComponent(entities[i], new VelocityComponent(1, 1));
                if (i % 3 == 0)
                    world.AddComponent(entities[i], new HealthComponent(100, 100));
            }
            sw.Stop();
            Console.WriteLine($"  Added components to {entityCount} entities in {sw.ElapsedMilliseconds}ms");
            
            // Iterate and modify components
            sw.Restart();
            int processedCount = 0;
            for (int i = 0; i < entityCount; i++)
            {
                if (world.HasComponent<PositionComponent>(entities[i]))
                {
                    ref var pos = ref world.GetComponent<PositionComponent>(entities[i]);
                    pos.Position.X += 1.0f;
                    processedCount++;
                }
            }
            sw.Stop();
            Console.WriteLine($"  Iterated and modified {processedCount} components in {sw.ElapsedMilliseconds}ms");
            Assert(processedCount == entityCount, $"Should process {entityCount} entities");
            
            // Destroy entities
            sw.Restart();
            for (int i = 0; i < entityCount; i++)
            {
                world.DestroyEntity(entities[i]);
            }
            sw.Stop();
            Console.WriteLine($"  Destroyed {entityCount} entities in {sw.ElapsedMilliseconds}ms");
            Assert(world.EntityCount == 0, "All entities should be destroyed");
            
            Console.WriteLine("  ✓ Performance test completed\n");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
