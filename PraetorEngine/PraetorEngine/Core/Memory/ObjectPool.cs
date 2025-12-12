using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.Memory
{
    /// <summary>
    /// Thread-safe object pool for reusing objects to reduce allocations and GC pressure.
    /// Uses ConcurrentBag for lock-free operations.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;
        private readonly int _maxSize;
        private int _count;

        /// <summary>
        /// Creates a new object pool.
        /// </summary>
        /// <param name="objectGenerator">Factory function to create new objects</param>
        /// <param name="resetAction">Optional action to reset objects before returning to pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="preAllocate">Number of objects to pre-allocate</param>
        public ObjectPool(
            Func<T> objectGenerator,
            Action<T>? resetAction = null,
            int maxSize = 1024,
            int preAllocate = 0)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException(nameof(objectGenerator));
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be non-negative");
            if (preAllocate < 0)
                throw new ArgumentOutOfRangeException(nameof(preAllocate), "Pre-allocate count must be non-negative");

            _objectGenerator = objectGenerator;
            _resetAction = resetAction;
            _maxSize = maxSize;
            _objects = new ConcurrentBag<T>();
            _count = 0;

            // Pre-allocate objects
            for (int i = 0; i < preAllocate; i++)
            {
                var obj = _objectGenerator();
                _objects.Add(obj);
                _count++;
            }
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Rent()
        {
            if (_objects.TryTake(out var obj))
            {
                System.Threading.Interlocked.Decrement(ref _count);
                return obj;
            }

            return _objectGenerator();
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T obj)
        {
            if (obj == null)
                return;

            // Reset object if reset action provided
            _resetAction?.Invoke(obj);

            // Only add to pool if under max size
            if (_maxSize == 0 || _count < _maxSize)
            {
                _objects.Add(obj);
                System.Threading.Interlocked.Increment(ref _count);
            }
            // Otherwise, let object be garbage collected
        }

        /// <summary>
        /// Gets the current number of objects in the pool.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        public void Clear()
        {
            while (_objects.TryTake(out _))
            {
                System.Threading.Interlocked.Decrement(ref _count);
            }
        }
    }

    /// <summary>
    /// Pooled object wrapper that automatically returns to pool when disposed.
    /// Use with 'using' statement for automatic return.
    /// </summary>
    public struct PooledObject<T> : IDisposable where T : class
    {
        private readonly ObjectPool<T> _pool;
        private T? _object;

        public T Object => _object ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

        internal PooledObject(ObjectPool<T> pool, T obj)
        {
            _pool = pool;
            _object = obj;
        }

        public void Dispose()
        {
            if (_object != null)
            {
                _pool.Return(_object);
                _object = null;
            }
        }
    }

    /// <summary>
    /// Extension methods for ObjectPool.
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Rents an object that will be automatically returned when disposed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledObject<T> RentScoped<T>(this ObjectPool<T> pool) where T : class
        {
            return new PooledObject<T>(pool, pool.Rent());
        }
    }
}
