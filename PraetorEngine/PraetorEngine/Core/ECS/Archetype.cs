using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Represents a unique combination of component types.
    /// Entities with the same archetype are stored together for cache efficiency.
    /// </summary>
    public sealed class Archetype
    {
        private readonly ComponentType[] _componentTypes;
        private readonly Dictionary<Type, object> _componentArrays;
        private readonly List<Entity> _entities;
        private int _version;

        public IReadOnlyList<ComponentType> ComponentTypes => _componentTypes;
        public IReadOnlyList<Entity> Entities => _entities;
        public int Version => _version;
        public int EntityCount => _entities.Count;

        public Archetype(params ComponentType[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                throw new ArgumentException("Archetype must have at least one component type", nameof(componentTypes));

            // Sort component types for consistent archetype matching
            _componentTypes = componentTypes.OrderBy(ct => ct.TypeId).ToArray();
            _componentArrays = new Dictionary<Type, object>();
            _entities = new List<Entity>();
            _version = 0;
        }

        /// <summary>
        /// Checks if this archetype contains all specified component types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(params ComponentType[] types)
        {
            if (types.Length > _componentTypes.Length)
                return false;

            foreach (var type in types)
            {
                if (!_componentTypes.Contains(type))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if this archetype exactly matches the specified component types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ExactMatch(params ComponentType[] types)
        {
            if (types.Length != _componentTypes.Length)
                return false;

            var sortedTypes = types.OrderBy(ct => ct.TypeId).ToArray();
            for (int i = 0; i < _componentTypes.Length; i++)
            {
                if (_componentTypes[i] != sortedTypes[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets or creates a component array for the specified type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged
        {
            var type = typeof(T);
            
            if (!_componentArrays.TryGetValue(type, out var array))
            {
                // Verify this archetype actually has this component type
                var componentType = ComponentType.Of<T>();
                if (!_componentTypes.Contains(componentType))
                {
                    throw new InvalidOperationException(
                        $"Archetype does not contain component type {type.Name}");
                }

                array = new ComponentArray<T>();
                _componentArrays[type] = array;
            }

            return (ComponentArray<T>)array;
        }

        /// <summary>
        /// Adds an entity to this archetype.
        /// </summary>
        internal void AddEntity(Entity entity)
        {
            _entities.Add(entity);
            _version++;
        }

        /// <summary>
        /// Removes an entity from this archetype.
        /// </summary>
        internal bool RemoveEntity(Entity entity)
        {
            bool removed = _entities.Remove(entity);
            if (removed)
            {
                _version++;
                
                // Remove components from all arrays
                foreach (var array in _componentArrays.Values)
                {
                    var removeMethod = array.GetType().GetMethod("Remove");
                    removeMethod?.Invoke(array, new object[] { entity });
                }
            }
            return removed;
        }

        /// <summary>
        /// Clears all entities and components from this archetype.
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            foreach (var array in _componentArrays.Values)
            {
                var clearMethod = array.GetType().GetMethod("Clear");
                clearMethod?.Invoke(array, null);
            }
            _version++;
        }

        public override string ToString()
        {
            var types = string.Join(", ", _componentTypes.Select(ct => $"T{ct.TypeId}"));
            return $"Archetype[{types}] ({EntityCount} entities)";
        }

        /// <summary>
        /// Computes a hash code for archetype matching.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var type in _componentTypes)
            {
                hash.Add(type);
            }
            return hash.ToHashCode();
        }
    }
}
