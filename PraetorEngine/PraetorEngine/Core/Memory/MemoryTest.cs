using System;
using System.Collections.Generic;
using System.IO;

namespace PraetorEngine.Core.Memory
{
    /// <summary>
    /// Test suite for memory management utilities.
    /// </summary>
    public static class MemoryTest
    {
        public static void RunAllTests()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory_test_results.txt");
            using (var writer = new StreamWriter(logPath))
            {
                var originalOut = Console.Out;
                Console.SetOut(writer);

                try
                {
                    Console.WriteLine("=== Starting Memory Management Tests ===\n");

                    TestObjectPool();
                    TestPooledArray();
                    TestAllocationTracker();
                    TestSecurityEdgeCases();
                    TestPerformance();

                    Console.WriteLine("\n=== All Memory Management Tests Passed ===");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }

            Console.WriteLine($"Memory tests completed successfully. Results written to: {logPath}");
        }

        private static void TestObjectPool()
        {
            Console.WriteLine("Test: ObjectPool");

            // Test basic rent/return
            var pool = new ObjectPool<List<int>>(() => new List<int>(), list => list.Clear());
            
            var obj1 = pool.Rent();
            obj1.Add(1);
            obj1.Add(2);
            Assert(obj1.Count == 2, "Object should have 2 items");
            
            pool.Return(obj1);
            Assert(pool.Count == 1, "Pool should have 1 object");

            var obj2 = pool.Rent();
            Assert(obj2.Count == 0, "Returned object should be reset");
            Assert(ReferenceEquals(obj1, obj2), "Should reuse same object");

            // Test scoped rent
            using (var scoped = pool.RentScoped())
            {
                scoped.Object.Add(100);
                Assert(scoped.Object.Count == 1, "Scoped object should have 1 item");
            }
            Assert(pool.Count == 1, "Object should be returned after scope");

            // Test max size
            var limitedPool = new ObjectPool<object>(() => new object(), null, maxSize: 2);
            var o1 = limitedPool.Rent();
            var o2 = limitedPool.Rent();
            var o3 = limitedPool.Rent();
            
            limitedPool.Return(o1);
            limitedPool.Return(o2);
            limitedPool.Return(o3); // Should not be added (exceeds max)
            Assert(limitedPool.Count == 2, "Pool should respect max size");

            // Test pre-allocation
            var preAllocPool = new ObjectPool<object>(() => new object(), null, maxSize: 100, preAllocate: 10);
            Assert(preAllocPool.Count == 10, "Pool should pre-allocate objects");

            Console.WriteLine("  ✓ ObjectPool works correctly\n");
        }

        private static void TestPooledArray()
        {
            Console.WriteLine("Test: PooledArray");

            // Test basic rent
            using (var pooled = PooledArray<int>.Rent(100))
            {
                Assert(pooled.Length == 100, "Length should be 100");
                Assert(pooled.Array.Length >= 100, "Array length should be at least 100");
                
                // Write to span
                var span = pooled.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = i;
                }
                Assert(pooled.Array[50] == 50, "Array should contain written values");
            }

            // Test WithPooledArray helper
            var result = PooledArrayHelper.WithPooledArray<int, int>(50, arr =>
            {
                arr[0] = 42;
                return arr[0];
            });
            Assert(result == 42, "WithPooledArray should work correctly");

            // Test WithPooledSpan helper
            int sum = 0;
            PooledArrayHelper.WithPooledSpan<int>(10, span =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = i + 1;
                    sum += span[i];
                }
            });
            Assert(sum == 55, "WithPooledSpan should work correctly");

            // Test negative length throws
            bool threwException = false;
            try
            {
                using var invalid = PooledArray<int>.Rent(-1);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw on negative length");

            Console.WriteLine("  ✓ PooledArray works correctly\n");
        }

        private static void TestAllocationTracker()
        {
            Console.WriteLine("Test: AllocationTracker");

            // Test zero allocation operation
            var stats = AllocationTracker.Track(() =>
            {
                // This should allocate nothing (struct operations)
                int x = 0;
                for (int i = 0; i < 100; i++)
                {
                    x += i;
                }
            });
            Console.WriteLine($"  Zero-allocation test: {stats}");
            // Note: Exact zero allocation hard to guarantee due to JIT/runtime, but should be very low
            
            // Test allocation operation
            var allocStats = AllocationTracker.Track(() =>
            {
                var list = new List<int>();
                for (int i = 0; i < 1000; i++)
                {
                    list.Add(i);
                }
            });
            Console.WriteLine($"  Allocation test: {allocStats}");
            Assert(allocStats.BytesAllocated > 0, "Should detect allocations");

            // Test Track with return value
            var (value, funcStats) = AllocationTracker.Track(() =>
            {
                var arr = new int[100];
                return arr.Length;
            });
            Assert(value == 100, "Function should return correct value");
            Assert(funcStats.BytesAllocated > 0, "Should track function allocations");

            // Test AllocationScope
            using (var scope = new AllocationScope("TestScope", (name, s) =>
            {
                Console.WriteLine($"  Scope '{name}': {s}");
                Assert(name == "TestScope", "Scope name should match");
            }))
            {
                _ = new int[50]; // Allocate something
            }

            Console.WriteLine("  ✓ AllocationTracker works correctly\n");
        }

        private static void TestSecurityEdgeCases()
        {
            Console.WriteLine("Test: Security and Edge Cases");

            // Test null object generator
            bool threwException = false;
            try
            {
                var pool = new ObjectPool<object>(null!);
            }
            catch (ArgumentNullException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw on null object generator");

            // Test negative max size
            threwException = false;
            try
            {
                var pool = new ObjectPool<object>(() => new object(), null, maxSize: -1);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw on negative max size");

            // Test negative pre-allocate
            threwException = false;
            try
            {
                var pool = new ObjectPool<object>(() => new object(), null, maxSize: 10, preAllocate: -1);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw on negative pre-allocate");

            // Test returning null to pool (should be ignored)
            var pool2 = new ObjectPool<object>(() => new object());
            pool2.Return(null!); // Should not crash
            Assert(pool2.Count == 0, "Null should not be added to pool");

            // Test disposed pooled object access
            var pooled = PooledArray<int>.Rent(10);
            pooled.Dispose();
            threwException = false;
            try
            {
                _ = pooled.Array;
            }
            catch (ObjectDisposedException)
            {
                threwException = true;
            }
            Assert(threwException, "Should throw when accessing disposed pooled array");

            Console.WriteLine("  ✓ Security and edge cases handled correctly\n");
        }

        private static void TestPerformance()
        {
            Console.WriteLine("Test: Performance Benchmarks");

            const int iterations = 10000;

            // Benchmark: Object pool vs new allocation
            var pool = new ObjectPool<List<int>>(() => new List<int>(), list => list.Clear(), preAllocate: 100);
            
            var pooledStats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var list = pool.Rent();
                    list.Add(i);
                    pool.Return(list);
                }
            });

            var directStats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var list = new List<int>();
                    list.Add(i);
                }
            });

            Console.WriteLine($"  Object Pool ({iterations} iterations): {pooledStats}");
            Console.WriteLine($"  Direct Allocation ({iterations} iterations): {directStats}");
            Console.WriteLine($"  Allocation reduction: {(1 - (double)pooledStats.BytesAllocated / directStats.BytesAllocated) * 100:F1}%");

            // Benchmark: Pooled array vs new array
            var arrayPooledStats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    using var arr = PooledArray<int>.Rent(100);
                    arr.Span[0] = i;
                }
            });

            var arrayDirectStats = AllocationTracker.Track(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var arr = new int[100];
                    arr[0] = i;
                }
            });

            Console.WriteLine($"  Pooled Array ({iterations} iterations): {arrayPooledStats}");
            Console.WriteLine($"  Direct Array Allocation ({iterations} iterations): {arrayDirectStats}");
            
            Assert(pooledStats.BytesAllocated < directStats.BytesAllocated, 
                "Pooled allocations should be less than direct allocations");

            Console.WriteLine("  ✓ Performance benchmarks completed\n");
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
