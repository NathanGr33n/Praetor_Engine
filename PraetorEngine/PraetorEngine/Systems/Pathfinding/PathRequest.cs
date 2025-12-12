using System.Collections.Generic;
using PraetorEngine.World;

namespace PraetorEngine.Systems.Pathfinding
{
    /// <summary>
    /// Request for pathfinding operation.
    /// Pooled to avoid allocations.
    /// </summary>
    public class PathRequest
    {
        public HexCoord Start { get; set; }
        public HexCoord Goal { get; set; }
        public int MaxSearchDepth { get; set; }
        public bool IgnoreTerrain { get; set; }
        
        // Result
        public List<HexCoord> Path { get; private set; }
        public bool Success { get; set; }
        public int NodesExplored { get; set; }

        public PathRequest()
        {
            Path = new List<HexCoord>(64); // Pre-allocate for typical path length
            MaxSearchDepth = 100;
            IgnoreTerrain = false;
        }

        /// <summary>
        /// Resets request for pooling.
        /// </summary>
        public void Reset()
        {
            Start = default;
            Goal = default;
            MaxSearchDepth = 100;
            IgnoreTerrain = false;
            Path.Clear();
            Success = false;
            NodesExplored = 0;
        }

        /// <summary>
        /// Configures the request.
        /// </summary>
        public void Configure(HexCoord start, HexCoord goal, int maxDepth = 100, bool ignoreTerrain = false)
        {
            Start = start;
            Goal = goal;
            MaxSearchDepth = maxDepth;
            IgnoreTerrain = ignoreTerrain;
        }
    }
}
