using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.Memory
{
    /// <summary>
    /// Tracks memory allocations and GC collections for performance monitoring.
    /// Used to detect allocation hotspots and GC pressure.
    /// </summary>
    public sealed class AllocationTracker
    {
        private long _startAllocated;
        private long _startGen0;
        private long _startGen1;
        private long _startGen2;
        private readonly Stopwatch _timer;

        public AllocationTracker()
        {
            _timer = new Stopwatch();
        }

        /// <summary>
        /// Starts tracking allocations and GC collections.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            // Force a GC to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _startAllocated = GC.GetTotalMemory(false);
            _startGen0 = GC.CollectionCount(0);
            _startGen1 = GC.CollectionCount(1);
            _startGen2 = GC.CollectionCount(2);
            _timer.Restart();
        }

        /// <summary>
        /// Stops tracking and returns allocation statistics.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllocationStats Stop()
        {
            _timer.Stop();
            
            var endAllocated = GC.GetTotalMemory(false);
            var endGen0 = GC.CollectionCount(0);
            var endGen1 = GC.CollectionCount(1);
            var endGen2 = GC.CollectionCount(2);

            return new AllocationStats
            {
                BytesAllocated = Math.Max(0, endAllocated - _startAllocated),
                Gen0Collections = (int)(endGen0 - _startGen0),
                Gen1Collections = (int)(endGen1 - _startGen1),
                Gen2Collections = (int)(endGen2 - _startGen2),
                ElapsedMilliseconds = _timer.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Tracks allocations for a specific action.
        /// </summary>
        public static AllocationStats Track(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var tracker = new AllocationTracker();
            tracker.Start();
            action();
            return tracker.Stop();
        }

        /// <summary>
        /// Tracks allocations for a specific function.
        /// </summary>
        public static (T Result, AllocationStats Stats) Track<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var tracker = new AllocationTracker();
            tracker.Start();
            var result = func();
            var stats = tracker.Stop();
            return (result, stats);
        }
    }

    /// <summary>
    /// Statistics about memory allocations and GC collections.
    /// </summary>
    public struct AllocationStats
    {
        public long BytesAllocated;
        public int Gen0Collections;
        public int Gen1Collections;
        public int Gen2Collections;
        public long ElapsedMilliseconds;

        /// <summary>
        /// Total number of GC collections across all generations.
        /// </summary>
        public int TotalCollections => Gen0Collections + Gen1Collections + Gen2Collections;

        /// <summary>
        /// Returns true if no allocations occurred.
        /// </summary>
        public bool IsZeroAllocation => BytesAllocated == 0;

        /// <summary>
        /// Returns true if no GC collections occurred.
        /// </summary>
        public bool IsZeroGC => TotalCollections == 0;

        public override string ToString()
        {
            return $"Allocated: {FormatBytes(BytesAllocated)}, " +
                   $"GC: Gen0={Gen0Collections} Gen1={Gen1Collections} Gen2={Gen2Collections}, " +
                   $"Time: {ElapsedMilliseconds}ms";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F2} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    /// <summary>
    /// Scope-based allocation tracker that reports on dispose.
    /// </summary>
    public struct AllocationScope : IDisposable
    {
        private readonly AllocationTracker _tracker;
        private readonly string _name;
        private readonly Action<string, AllocationStats>? _onComplete;

        public AllocationScope(string name, Action<string, AllocationStats>? onComplete = null)
        {
            _name = name;
            _onComplete = onComplete;
            _tracker = new AllocationTracker();
            _tracker.Start();
        }

        public void Dispose()
        {
            var stats = _tracker.Stop();
            _onComplete?.Invoke(_name, stats);
        }

        /// <summary>
        /// Creates a scope that logs results to console.
        /// </summary>
        public static AllocationScope WithLogging(string name)
        {
            return new AllocationScope(name, (n, s) =>
            {
                Console.WriteLine($"[Allocation] {n}: {s}");
            });
        }
    }
}
