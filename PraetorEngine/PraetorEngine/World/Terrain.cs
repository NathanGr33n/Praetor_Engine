using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.World
{
    /// <summary>
    /// Terrain types affecting movement and gameplay.
    /// </summary>
    public enum TerrainType : byte
    {
        Plains = 0,
        Forest = 1,
        Hills = 2,
        Mountain = 3,
        Water = 4,
        Desert = 5,
        Swamp = 6,
        Snow = 7
    }

    /// <summary>
    /// Component storing terrain information for a hex tile.
    /// Must be unmanaged for ECS compatibility.
    /// </summary>
    public struct TerrainComponent
    {
        public TerrainType Type;
        public byte Elevation; // 0-255 height levels
        public byte Visibility; // Line of sight modifier

        public TerrainComponent(TerrainType type, byte elevation = 0, byte visibility = 100)
        {
            Type = type;
            Elevation = elevation;
            Visibility = visibility;
        }
    }

    /// <summary>
    /// Static terrain properties and gameplay modifiers.
    /// Used as a lookup table for terrain behavior.
    /// </summary>
    public static class TerrainData
    {
        /// <summary>
        /// Base movement cost for each terrain type.
        /// Lower is faster, 255 means impassable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetMovementCost(TerrainType type)
        {
            return type switch
            {
                TerrainType.Plains => 10,
                TerrainType.Forest => 20,
                TerrainType.Hills => 15,
                TerrainType.Mountain => 30,
                TerrainType.Water => 255, // Impassable by default
                TerrainType.Desert => 15,
                TerrainType.Swamp => 25,
                TerrainType.Snow => 20,
                _ => 10
            };
        }

        /// <summary>
        /// Checks if a terrain type is passable for ground units.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPassable(TerrainType type)
        {
            return type != TerrainType.Water && type != TerrainType.Mountain;
        }

        /// <summary>
        /// Gets defensive bonus percentage for terrain type.
        /// Used in combat calculations (Phase 3).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDefenseBonus(TerrainType type)
        {
            return type switch
            {
                TerrainType.Plains => 0,
                TerrainType.Forest => 20,
                TerrainType.Hills => 15,
                TerrainType.Mountain => 40,
                TerrainType.Water => 0,
                TerrainType.Desert => 5,
                TerrainType.Swamp => 10,
                TerrainType.Snow => 5,
                _ => 0
            };
        }

        /// <summary>
        /// Gets vision range modifier for terrain type.
        /// Negative values reduce vision, positive values increase it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetVisionModifier(TerrainType type)
        {
            return type switch
            {
                TerrainType.Plains => 1,
                TerrainType.Forest => -1,
                TerrainType.Hills => 2,
                TerrainType.Mountain => 3,
                TerrainType.Water => 0,
                TerrainType.Desert => 1,
                TerrainType.Swamp => -1,
                TerrainType.Snow => 0,
                _ => 0
            };
        }

        /// <summary>
        /// Gets a display-friendly name for terrain type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTerrainName(TerrainType type)
        {
            return type switch
            {
                TerrainType.Plains => "Plains",
                TerrainType.Forest => "Forest",
                TerrainType.Hills => "Hills",
                TerrainType.Mountain => "Mountain",
                TerrainType.Water => "Water",
                TerrainType.Desert => "Desert",
                TerrainType.Swamp => "Swamp",
                TerrainType.Snow => "Snow",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Terrain generation utilities for creating procedural maps.
    /// </summary>
    public static class TerrainGenerator
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a random terrain type based on simple noise.
        /// For Phase 2, uses basic randomization. Can be enhanced with Perlin noise later.
        /// </summary>
        public static TerrainType GenerateTerrainAt(HexCoord coord, int seed = 0)
        {
            // Use coordinate hash for deterministic generation
            int hash = HashCode.Combine(coord.Q, coord.R, seed);
            var rng = new Random(hash);
            int value = rng.Next(100);

            // Simple biome distribution
            return value switch
            {
                < 40 => TerrainType.Plains,
                < 60 => TerrainType.Forest,
                < 70 => TerrainType.Hills,
                < 75 => TerrainType.Mountain,
                < 80 => TerrainType.Water,
                < 90 => TerrainType.Desert,
                < 95 => TerrainType.Swamp,
                _ => TerrainType.Snow
            };
        }

        /// <summary>
        /// Generates elevation based on terrain type and position.
        /// </summary>
        public static byte GenerateElevation(HexCoord coord, TerrainType type, int seed = 0)
        {
            int hash = HashCode.Combine(coord.Q, coord.R, seed, (int)type);
            var rng = new Random(hash);

            return type switch
            {
                TerrainType.Plains => (byte)rng.Next(0, 30),
                TerrainType.Forest => (byte)rng.Next(10, 50),
                TerrainType.Hills => (byte)rng.Next(40, 100),
                TerrainType.Mountain => (byte)rng.Next(120, 255),
                TerrainType.Water => 0,
                TerrainType.Desert => (byte)rng.Next(5, 40),
                TerrainType.Swamp => (byte)rng.Next(0, 20),
                TerrainType.Snow => (byte)rng.Next(80, 200),
                _ => 0
            };
        }
    }
}
