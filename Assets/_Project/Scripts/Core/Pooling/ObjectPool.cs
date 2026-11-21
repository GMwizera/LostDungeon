using System;
using System.Collections.Generic;

namespace LostDungeon.Core.Pooling
{
    /// <summary>
    /// Generic object pool (Object Pooling pattern).
    /// Reuses objects instead of creating and destroying them, which avoids garbage collection
    /// spikes when many projectiles or effects are fired. Has no Unity code, so it is unit tested;
    /// PoolManager wraps it for GameObjects.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> available = new Stack<T>();
        private readonly HashSet<T> inUse = new HashSet<T>();
        private readonly Func<T> create;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Action<T> onDestroy;
        private readonly int maxSize;

        public int CountActive => inUse.Count;
        public int CountInactive => available.Count;
        public int CountAll => CountActive + CountInactive;

        /// <param name="create">Makes a new object when the pool is empty.</param>
        /// <param name="onGet">Runs when an object is taken out.</param>
        /// <param name="onRelease">Runs when an object is put back.</param>
        /// <param name="onDestroy">Runs when an object is returned but the pool is already full.</param>
        /// <param name="maxSize">Most inactive objects kept for reuse.</param>
        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null,
            Action<T> onDestroy = null, int maxSize = 100)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            if (maxSize <= 0)
                throw new ArgumentException("Max size must be greater than zero.", nameof(maxSize));

            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onDestroy = onDestroy;
            this.maxSize = maxSize;
        }

        public T Get()
        {
            T item = available.Count > 0 ? available.Pop() : create();
            inUse.Add(item);
            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (!inUse.Remove(item))
                throw new InvalidOperationException("Item was not taken from this pool or was already released.");

            onRelease?.Invoke(item);

            if (available.Count < maxSize)
                available.Push(item);
            else
                onDestroy?.Invoke(item);
        }

        /// <summary>Creates objects ahead of time so the first shots don't cause a hitch.</summary>
        public void Prewarm(int count)
        {
            var created = new List<T>(count);
            for (int i = 0; i < count; i++)
                created.Add(Get());
            foreach (T item in created)
                Release(item);
        }
    }
}
