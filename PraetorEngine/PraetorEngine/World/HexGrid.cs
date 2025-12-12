using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.World
{
    /// <summary>
    /// Hexagonal grid using axial coordinates (q, r).
    /// Flat-top orientation for strategy games.
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q; // Column
        public readonly int R; // Row

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int S => -Q - R; // Cube coordinate S

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(Q, R);

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);
        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);

        public override string ToString() => $"Hex({Q}, {R})";
    }

    /// <summary>
    /// Hex grid utilities for coordinate conversion and distance calculation.
    /// </summary>
    public static class HexGrid
    {
        public const float HexSize = 32.0f; // Default hex size in pixels
        private const float Sqrt3 = 1.732050808f;

        /// <summary>
        /// Converts hex coordinates to world position (flat-top).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 HexToWorld(HexCoord hex, float hexSize = HexSize)
        {
            float x = hexSize * (1.5f * hex.Q);
            float y = hexSize * (Sqrt3 / 2.0f * hex.Q + Sqrt3 * hex.R);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Converts world position to hex coordinates (flat-top).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HexCoord WorldToHex(Vector2 position, float hexSize = HexSize)
        {
            float q = (2.0f / 3.0f * position.X) / hexSize;
            float r = (-1.0f / 3.0f * position.X + Sqrt3 / 3.0f * position.Y) / hexSize;
            return RoundHex(q, r);
        }

        /// <summary>
        /// Rounds fractional hex coordinates to nearest integer hex.
        /// </summary>
        private static HexCoord RoundHex(float q, float r)
        {
            float s = -q - r;
            int rq = (int)MathF.Round(q);
            int rr = (int)MathF.Round(r);
            int rs = (int)MathF.Round(s);

            float qDiff = MathF.Abs(rq - q);
            float rDiff = MathF.Abs(rr - r);
            float sDiff = MathF.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
                rq = -rr - rs;
            else if (rDiff > sDiff)
                rr = -rq - rs;

            return new HexCoord(rq, rr);
        }

        /// <summary>
        /// Calculates Manhattan distance between two hexes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Distance(HexCoord a, HexCoord b)
        {
            return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
        }

        /// <summary>
        /// Gets neighbor in specified direction (0-5, clockwise from right).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HexCoord GetNeighbor(HexCoord hex, int direction)
        {
            return direction switch
            {
                0 => new HexCoord(hex.Q + 1, hex.R),      // East
                1 => new HexCoord(hex.Q + 1, hex.R - 1),  // Northeast
                2 => new HexCoord(hex.Q, hex.R - 1),      // Northwest
                3 => new HexCoord(hex.Q - 1, hex.R),      // West
                4 => new HexCoord(hex.Q - 1, hex.R + 1),  // Southwest
                5 => new HexCoord(hex.Q, hex.R + 1),      // Southeast
                _ => hex
            };
        }

        /// <summary>
        /// Gets all 6 neighbors of a hex.
        /// </summary>
        public static HexCoord[] GetNeighbors(HexCoord hex)
        {
            var neighbors = new HexCoord[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = GetNeighbor(hex, i);
            }
            return neighbors;
        }
    }
}
