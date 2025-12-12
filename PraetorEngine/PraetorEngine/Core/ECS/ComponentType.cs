using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Represents a unique identifier for a component type.
    /// Used for fast component type comparisons in archetype matching.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>, IComparable<ComponentType>
    {
        public readonly int TypeId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentType(int typeId)
        {
            TypeId = typeId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentType Of<T>() where T : unmanaged
        {
            return new ComponentType(ComponentTypeRegistry.GetTypeId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ComponentType other) => TypeId == other.TypeId;

        public override bool Equals(object? obj) => obj is ComponentType other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => TypeId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ComponentType other) => TypeId.CompareTo(other.TypeId);

        public static bool operator ==(ComponentType left, ComponentType right) => left.Equals(right);
        public static bool operator !=(ComponentType left, ComponentType right) => !left.Equals(right);

        public override string ToString() => $"ComponentType({TypeId})";
    }

    /// <summary>
    /// Registry for mapping component types to unique integer IDs.
    /// Thread-safe singleton for component type registration.
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static readonly object _lock = new object();
        private static int _nextTypeId = 1;
        private static readonly System.Collections.Generic.Dictionary<Type, int> _typeToId = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeId<T>() where T : unmanaged
        {
            var type = typeof(T);
            
            // Fast path: check without lock
            if (_typeToId.TryGetValue(type, out int id))
            {
                return id;
            }

            // Slow path: register new type
            lock (_lock)
            {
                // Double-check after acquiring lock
                if (_typeToId.TryGetValue(type, out id))
                {
                    return id;
                }

                // Validate that we haven't exceeded max component types
                if (_nextTypeId >= 256)
                {
                    throw new InvalidOperationException(
                        $"Maximum number of component types (256) exceeded. Cannot register type: {type.Name}");
                }

                id = _nextTypeId++;
                _typeToId[type] = id;
                return id;
            }
        }

        /// <summary>
        /// Gets the number of registered component types.
        /// </summary>
        public static int RegisteredTypeCount
        {
            get
            {
                lock (_lock)
                {
                    return _typeToId.Count;
                }
            }
        }
    }
}
