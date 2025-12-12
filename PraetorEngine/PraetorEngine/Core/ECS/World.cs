using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.ECS
{
    /// <summary>
    /// Central ECS world managing all entities, components, and archetypes.
    /// Uses archetype-based storage for cache-efficient iteration.
    /// </summary>
    public sealed class World
    {
        private readonly List<Archetype> _archetypes;
        private readonly Dictionary<Entity, Archetype> _entityToArchetype;
        private readonly Queue<int> _freeEntityIds;
        private int[] _entityGenerations;
        
        private int _nextEntityId;
        private int _entityCount;

        public int EntityCount => _entityCount;
        public int ArchetypeCount => _archetypes.Count;

        public World(int initialCapacity = 10000)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be positive");

            _archetypes = new List<Archetype>();
            _entityToArchetype = new Dictionary<Entity, Archetype>(initialCapacity);
            _freeEntityIds = new Queue<int>();
            _entityGenerations = new int[initialCapacity];
            _nextEntityId = 1; // Start at 1, reserve 0 for null
            _entityCount = 0;
        }

        /// <summary>
        /// Creates a new entity without components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity()
        {
            int id;
            if (_freeEntityIds.Count > 0)
            {
                id = _freeEntityIds.Dequeue();
            }
            else
            {
                id = _nextEntityId++;
                EnsureEntityCapacity(id);
            }

            var entity = new Entity(id, _entityGenerations[id]);
            _entityCount++;
            return entity;
        }

        /// <summary>
        /// Destroys an entity and removes all its components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DestroyEntity(Entity entity)
        {
            if (!ValidateEntity(entity))
                return false;

            // Remove from archetype tracking
            if (_entityToArchetype.TryGetValue(entity, out var archetype))
            {
                archetype.RemoveEntity(entity);
                _entityToArchetype.Remove(entity);
            }

            // Remove all components
            foreach (var storage in _componentStorage.Values)
            {
                var removeMethod = storage.GetType().GetMethod("Remove");
                removeMethod?.Invoke(storage, new object[] { entity });
            }

            // Increment generation to invalidate old references
            _entityGenerations[entity.Id]++;
            _freeEntityIds.Enqueue(entity.Id);
            _entityCount--;
            return true;
        }

        /// <summary>
        /// Checks if an entity is alive (valid generation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityAlive(Entity entity)
        {
            return ValidateEntity(entity);
        }

        // Component storage: Type -> ComponentArray
        private readonly Dictionary<Type, object> _componentStorage = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentArray<T> GetOrCreateComponentArray<T>() where T : unmanaged
        {
            var type = typeof(T);
            if (!_componentStorage.TryGetValue(type, out var array))
            {
                array = new ComponentArray<T>();
                _componentStorage[type] = array;
            }
            return (ComponentArray<T>)array;
        }

        /// <summary>
        /// Adds a component to an entity.
        /// </summary>
        public void AddComponent<T>(Entity entity, in T component) where T : unmanaged
        {
            if (!ValidateEntity(entity))
                throw new ArgumentException($"Entity {entity} is not valid", nameof(entity));

            var componentArray = GetOrCreateComponentArray<T>();
            componentArray.Add(entity, component);
        }

        /// <summary>
        /// Removes a component from an entity.
        /// </summary>
        public bool RemoveComponent<T>(Entity entity) where T : unmanaged
        {
            if (!ValidateEntity(entity))
                return false;

            var componentArray = GetOrCreateComponentArray<T>();
            return componentArray.Remove(entity);
        }

        /// <summary>
        /// Gets a reference to a component. Throws if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Entity entity) where T : unmanaged
        {
            if (!ValidateEntity(entity))
                throw new ArgumentException($"Entity {entity} is not valid", nameof(entity));

            var componentArray = GetOrCreateComponentArray<T>();
            return ref componentArray.Get(entity);
        }

        /// <summary>
        /// Checks if an entity has a specific component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : unmanaged
        {
            if (!ValidateEntity(entity))
                return false;

            var componentArray = GetOrCreateComponentArray<T>();
            return componentArray.Has(entity);
        }

        /// <summary>
        /// Gets all archetypes that match the specified component types.
        /// </summary>
        public List<Archetype> GetArchetypes(params ComponentType[] types)
        {
            var result = new List<Archetype>();
            foreach (var archetype in _archetypes)
            {
                if (archetype.Matches(types))
                {
                    result.Add(archetype);
                }
            }
            return result;
        }

        /// <summary>
        /// Clears all entities and archetypes.
        /// </summary>
        public void Clear()
        {
            foreach (var archetype in _archetypes)
            {
                archetype.Clear();
            }
            _archetypes.Clear();
            _entityToArchetype.Clear();
            
            // Clear all component storage
            foreach (var storage in _componentStorage.Values)
            {
                var clearMethod = storage.GetType().GetMethod("Clear");
                clearMethod?.Invoke(storage, null);
            }
            _componentStorage.Clear();
            
            _freeEntityIds.Clear();
            Array.Clear(_entityGenerations, 0, _entityGenerations.Length);
            _nextEntityId = 1;
            _entityCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateEntity(Entity entity)
        {
            return entity.Id > 0 && 
                   entity.Id < _entityGenerations.Length && 
                   _entityGenerations[entity.Id] == entity.Generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEntityCapacity(int entityId)
        {
            if (entityId >= _entityGenerations.Length)
            {
                int newCapacity = Math.Max(entityId + 1, _entityGenerations.Length * 2);
                Array.Resize(ref _entityGenerations, newCapacity);
            }
        }

        private Archetype GetOrCreateArchetype(ComponentType[] types)
        {
            // Find existing archetype with exact match
            foreach (var archetype in _archetypes)
            {
                if (archetype.ExactMatch(types))
                {
                    return archetype;
                }
            }

            // Create new archetype
            var newArchetype = new Archetype(types);
            _archetypes.Add(newArchetype);
            return newArchetype;
        }
    }
}
