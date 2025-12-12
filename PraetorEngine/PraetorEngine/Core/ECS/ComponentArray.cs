using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Dense packed array storage for components with sparse set for O(1) entity-to-component lookup.
    /// Uses the sparse set pattern: sparse array maps entity ID to dense index, dense array stores components.
    /// </summary>
    public sealed class ComponentArray<T> where T : unmanaged
    {
        private T[] _denseComponents;
        private int[] _denseToEntity;        // Maps dense index to entity ID
        private int[] _sparseToIndex;        // Maps entity ID to dense index (-1 if not present)
        private int _count;

        public int Count => _count;
        public int Capacity => _denseComponents.Length;

        public ComponentArray(int initialCapacity = 1024)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be positive");

            _denseComponents = new T[initialCapacity];
            _denseToEntity = new int[initialCapacity];
            _sparseToIndex = new int[initialCapacity];
            Array.Fill(_sparseToIndex, -1);
            _count = 0;
        }

        /// <summary>
        /// Adds a component for an entity. Throws if component already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Entity entity, in T component)
        {
            if (entity.Id < 0)
                throw new ArgumentException("Entity ID must be non-negative", nameof(entity));

            EnsureSparseCapacity(entity.Id);

            if (_sparseToIndex[entity.Id] != -1)
                throw new InvalidOperationException($"Component {typeof(T).Name} already exists for entity {entity}");

            EnsureDenseCapacity(_count + 1);

            int denseIndex = _count;
            _denseComponents[denseIndex] = component;
            _denseToEntity[denseIndex] = entity.Id;
            _sparseToIndex[entity.Id] = denseIndex;
            _count++;
        }

        /// <summary>
        /// Removes a component from an entity. Returns true if removed, false if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(Entity entity)
        {
            if (entity.Id < 0 || entity.Id >= _sparseToIndex.Length)
                return false;

            int denseIndex = _sparseToIndex[entity.Id];
            if (denseIndex == -1)
                return false;

            // Swap with last element (swap-and-pop for dense array)
            int lastIndex = _count - 1;
            if (denseIndex != lastIndex)
            {
                _denseComponents[denseIndex] = _denseComponents[lastIndex];
                int movedEntityId = _denseToEntity[lastIndex];
                _denseToEntity[denseIndex] = movedEntityId;
                _sparseToIndex[movedEntityId] = denseIndex;
            }

            _sparseToIndex[entity.Id] = -1;
            _count--;
            return true;
        }

        /// <summary>
        /// Checks if an entity has this component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            return entity.Id >= 0 && 
                   entity.Id < _sparseToIndex.Length && 
                   _sparseToIndex[entity.Id] != -1;
        }

        /// <summary>
        /// Gets a reference to a component. Throws if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity entity)
        {
            if (entity.Id < 0 || entity.Id >= _sparseToIndex.Length)
                throw new ArgumentException($"Entity {entity} is out of range", nameof(entity));

            int denseIndex = _sparseToIndex[entity.Id];
            if (denseIndex == -1)
                throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T).Name}");

            return ref _denseComponents[denseIndex];
        }

        /// <summary>
        /// Tries to get a reference to a component. Returns false if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Entity entity, out T component)
        {
            if (entity.Id >= 0 && entity.Id < _sparseToIndex.Length)
            {
                int denseIndex = _sparseToIndex[entity.Id];
                if (denseIndex != -1)
                {
                    component = _denseComponents[denseIndex];
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Gets the dense component array for iteration. Use with GetEntities() for entity-component pairs.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetComponents() => _denseComponents.AsSpan(0, _count);

        /// <summary>
        /// Gets the entity IDs corresponding to dense components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<int> GetEntityIds() => _denseToEntity.AsSpan(0, _count);

        /// <summary>
        /// Clears all components.
        /// </summary>
        public void Clear()
        {
            Array.Fill(_sparseToIndex, -1);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureDenseCapacity(int required)
        {
            if (required <= _denseComponents.Length)
                return;

            int newCapacity = Math.Max(required, _denseComponents.Length * 2);
            Array.Resize(ref _denseComponents, newCapacity);
            Array.Resize(ref _denseToEntity, newCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSparseCapacity(int entityId)
        {
            if (entityId < _sparseToIndex.Length)
                return;

            int newCapacity = Math.Max(entityId + 1, _sparseToIndex.Length * 2);
            int oldLength = _sparseToIndex.Length;
            Array.Resize(ref _sparseToIndex, newCapacity);
            Array.Fill(_sparseToIndex, -1, oldLength, newCapacity - oldLength);
        }
    }
}
