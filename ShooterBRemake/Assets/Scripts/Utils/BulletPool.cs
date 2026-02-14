using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public static class BulletPool
    {
        private static readonly Dictionary<GameObject, Queue<GameObject>> PoolsByPrefab = new Dictionary<GameObject, Queue<GameObject>>();

        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (!PoolsByPrefab.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                PoolsByPrefab[prefab] = pool;
            }

            while (pool.Count > 0)
            {
                GameObject pooled = pool.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            return Object.Instantiate(prefab);
        }

        public static void Return(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null)
            {
                if (instance != null)
                    Object.Destroy(instance);
                return;
            }

            if (!PoolsByPrefab.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                PoolsByPrefab[prefab] = pool;
            }

            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    }
}
