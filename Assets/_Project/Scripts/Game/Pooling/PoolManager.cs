using System.Collections.Generic;
using LostDungeon.Core.Pooling;
using LostDungeon.Game.Managers;
using UnityEngine;

namespace LostDungeon.Game.Pooling
{
    /// <summary>
    /// Object Pool: reuses objects instead of making new ones.
    /// Each prefab gets its own pool the first time it spawns.
    /// </summary>
    [DefaultExecutionOrder(-100)] // runs before other scripts need it
    public class PoolManager : Singleton<PoolManager>
    {
        [SerializeField] private int maxPooledPerPrefab = 50;

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> poolsByPrefab = new Dictionary<GameObject, ObjectPool<GameObject>>();
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> poolsByInstance = new Dictionary<GameObject, ObjectPool<GameObject>>();
        private readonly List<IPoolable> poolableBuffer = new List<IPoolable>();

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject instance = GetPool(prefab.gameObject).Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            instance.GetComponents(poolableBuffer);
            foreach (IPoolable poolable in poolableBuffer)
                poolable.OnSpawned();

            return instance.GetComponent<T>();
        }

        public void Despawn(GameObject instance)
        {
            if (!poolsByInstance.TryGetValue(instance, out ObjectPool<GameObject> pool))
            {
                Destroy(instance); // not from a pool
                return;
            }

            instance.GetComponents(poolableBuffer);
            foreach (IPoolable poolable in poolableBuffer)
                poolable.OnDespawned();

            pool.Release(instance);
        }

        /// <summary>
        /// Deletes all pooled objects when a level unloads.
        /// Its enemies came from that level's Addressables bundle.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject instance in poolsByInstance.Keys)
                if (instance != null)
                    Destroy(instance);
            foreach (Transform container in transform)
                Destroy(container.gameObject);

            poolsByInstance.Clear();
            poolsByPrefab.Clear();
        }

        /// <summary>Makes some copies early, like while a level loads.</summary>
        public void Prewarm(GameObject prefab, int count) => GetPool(prefab).Prewarm(count);

        private ObjectPool<GameObject> GetPool(GameObject prefab)
        {
            if (poolsByPrefab.TryGetValue(prefab, out ObjectPool<GameObject> pool))
                return pool;

            Transform container = new GameObject($"{prefab.name} Pool").transform;
            container.SetParent(transform);

            pool = new ObjectPool<GameObject>(
                create: () =>
                {
                    GameObject instance = Instantiate(prefab, container);
                    instance.SetActive(false);
                    poolsByInstance[instance] = poolsByPrefab[prefab];
                    return instance;
                },
                onRelease: instance => instance.SetActive(false),
                onDestroy: instance =>
                {
                    poolsByInstance.Remove(instance);
                    Destroy(instance);
                },
                maxSize: maxPooledPerPrefab);

            poolsByPrefab[prefab] = pool;
            return pool;
        }
    }
}
