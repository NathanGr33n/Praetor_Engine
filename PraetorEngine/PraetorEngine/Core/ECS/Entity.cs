using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Lightweight entity identifier consisting of an ID and generation counter.
    /// 8 bytes total for cache efficiency.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Entity : IEquatable<Entity>
    {
        public static readonly Entity Null = new Entity(0, 0);

        public readonly int Id;
        public readonly int Generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull() => Id == 0 && Generation == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;

        public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(Id, Generation);

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        public override string ToString() => $"Entity({Id}, Gen:{Generation})";
    }
}
