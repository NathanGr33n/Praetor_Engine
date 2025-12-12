using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.Profiling
{
    /// <summary>
    /// Hierarchical profiler for tracking performance with frame budgets.
    /// </summary>
    public sealed class Profiler
    {
        private readonly Dictionary<string, ProfileSection> _sections;
        private readonly Stack<string> _activeStack;
        private readonly Stopwatch _frameTimer;
        private long _frameStartTicks;
        private int _frameCount;

        public float TargetFrameTimeMs { get; set; } = 16.67f; // 60 FPS
        public float CurrentFrameTimeMs { get; private set; }
        public int FrameCount => _frameCount;

        public Profiler()
        {
            _sections = new Dictionary<string, ProfileSection>();
            _activeStack = new Stack<string>();
            _frameTimer = Stopwatch.StartNew();
        }

        /// <summary>
        /// Marks the start of a new frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginFrame()
        {
            _frameStartTicks = _frameTimer.ElapsedTicks;
            _frameCount++;
        }

        /// <summary>
        /// Marks the end of a frame and calculates timing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame()
        {
            var elapsed = _frameTimer.ElapsedTicks - _frameStartTicks;
            CurrentFrameTimeMs = (float)(elapsed * 1000.0 / Stopwatch.Frequency);
        }

        /// <summary>
        /// Begins profiling a section.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin(string name)
        {
            if (!_sections.TryGetValue(name, out var section))
            {
                section = new ProfileSection(name);
                _sections[name] = section;
            }

            section.Begin();
            _activeStack.Push(name);
        }

        /// <summary>
        /// Ends profiling the current section.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
            if (_activeStack.Count == 0)
                throw new InvalidOperationException("No active profiling section to end");

            var name = _activeStack.Pop();
            _sections[name].End();
        }

        /// <summary>
        /// Gets results for a specific section.
        /// </summary>
        public ProfileSection? GetSection(string name)
        {
            return _sections.TryGetValue(name, out var section) ? section : null;
        }

        /// <summary>
        /// Gets all sections.
        /// </summary>
        public IReadOnlyDictionary<string, ProfileSection> GetAllSections() => _sections;

        /// <summary>
        /// Resets all profiling data.
        /// </summary>
        public void Reset()
        {
            foreach (var section in _sections.Values)
            {
                section.Reset();
            }
            _frameCount = 0;
        }

        /// <summary>
        /// Creates a scoped profiling region.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfileScope Scope(string name) => new ProfileScope(this, name);
    }

    /// <summary>
    /// Represents timing data for a profiled section.
    /// </summary>
    public sealed class ProfileSection
    {
        private readonly Stopwatch _timer;
        public string Name { get; }
        public long CallCount { get; private set; }
        public double TotalMs { get; private set; }
        public double AverageMs => CallCount > 0 ? TotalMs / CallCount : 0;
        public double LastMs { get; private set; }

        internal ProfileSection(string name)
        {
            Name = name;
            _timer = new Stopwatch();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Begin()
        {
            _timer.Restart();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void End()
        {
            _timer.Stop();
            LastMs = _timer.Elapsed.TotalMilliseconds;
            TotalMs += LastMs;
            CallCount++;
        }

        internal void Reset()
        {
            CallCount = 0;
            TotalMs = 0;
            LastMs = 0;
        }

        public override string ToString() => 
            $"{Name}: {LastMs:F2}ms (avg: {AverageMs:F2}ms, calls: {CallCount})";
    }

    /// <summary>
    /// RAII-style profiling scope.
    /// </summary>
    public struct ProfileScope : IDisposable
    {
        private readonly Profiler _profiler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ProfileScope(Profiler profiler, string name)
        {
            _profiler = profiler;
            _profiler.Begin(name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _profiler.End();
        }
    }
}
