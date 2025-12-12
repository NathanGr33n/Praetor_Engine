using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.Memory
{
    /// <summary>
    /// Wrapper around ArrayPool for renting temporary arrays with zero allocations.
    /// Automatically returns arrays to pool when disposed.
    /// </summary>
    public struct PooledArray<T> : IDisposable
    {
        private T[]? _array;
        private readonly int _length;
        private readonly bool _clearOnReturn;

        /// <summary>
        /// Gets the rented array. Throws if disposed.
        /// </summary>
        public T[] Array => _array ?? throw new ObjectDisposedException(nameof(PooledArray<T>));

        /// <summary>
        /// Gets the requested length (may be less than array length).
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Gets a span of the requested length.
        /// </summary>
        public Span<T> Span => _array != null ? _array.AsSpan(0, _length) : Span<T>.Empty;

        private PooledArray(T[] array, int length, bool clearOnReturn)
        {
            _array = array;
            _length = length;
            _clearOnReturn = clearOnReturn;
        }

        /// <summary>
        /// Rents an array from the shared pool.
        /// </summary>
        /// <param name="minimumLength">Minimum required length</param>
        /// <param name="clearOnReturn">Whether to clear array when returned to pool</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledArray<T> Rent(int minimumLength, bool clearOnReturn = false)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength), "Length must be non-negative");

            var array = ArrayPool<T>.Shared.Rent(minimumLength);
            return new PooledArray<T>(array, minimumLength, clearOnReturn);
        }

        /// <summary>
        /// Returns the array to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array, _clearOnReturn);
                _array = null;
            }
        }
    }

    /// <summary>
    /// Static helpers for array pooling.
    /// </summary>
    public static class PooledArrayHelper
    {
        /// <summary>
        /// Rents a pooled array and executes an action with it.
        /// Array is automatically returned after action completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WithPooledArray<T>(int length, Action<T[]> action, bool clearOnReturn = false)
        {
            using var pooled = PooledArray<T>.Rent(length, clearOnReturn);
            action(pooled.Array);
        }

        /// <summary>
        /// Rents a pooled array, executes a function with it, and returns the result.
        /// Array is automatically returned after function completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult WithPooledArray<T, TResult>(
            int length,
            Func<T[], TResult> func,
            bool clearOnReturn = false)
        {
            using var pooled = PooledArray<T>.Rent(length, clearOnReturn);
            return func(pooled.Array);
        }

        /// <summary>
        /// Delegate for span operations.
        /// </summary>
        public delegate void SpanAction<T>(Span<T> span);

        /// <summary>
        /// Rents a pooled array and executes an action with its span.
        /// Array is automatically returned after action completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WithPooledSpan<T>(int length, SpanAction<T> action, bool clearOnReturn = false)
        {
            using var pooled = PooledArray<T>.Rent(length, clearOnReturn);
            action(pooled.Span);
        }
    }
}
