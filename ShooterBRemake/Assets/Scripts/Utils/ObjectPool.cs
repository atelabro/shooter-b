using UnityEngine;
using System.Collections.Generic;

namespace ShooterB
{
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        public List<Pool> pools;
        private Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
        }

        public void InitializePools()
        {
            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
                GameLog.Log($"Pool initialized: {pool.tag} with {pool.size} objects");
            }
        }

        public GameObject Get(string tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                GameLog.Warning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject objectToSpawn;

            if (poolDictionary[tag].Count > 0)
            {
                objectToSpawn = poolDictionary[tag].Dequeue();
            }
            else
            {
                Pool pool = pools.Find(p => p.tag == tag);
                objectToSpawn = Instantiate(pool.prefab);
                objectToSpawn.transform.SetParent(transform);
                GameLog.Log($"Pool {tag} exhausted, created new object");
            }

            objectToSpawn.SetActive(true);
            return objectToSpawn;
        }

        public void Return(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                GameLog.Warning($"Pool with tag {tag} doesn't exist.");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }

        public void Preload(string tag, int count)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                GameLog.Warning($"Pool with tag {tag} doesn't exist.");
                return;
            }

            Pool pool = pools.Find(p => p.tag == tag);
            if (pool == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                poolDictionary[tag].Enqueue(obj);
            }

            GameLog.Log($"Preloaded {count} additional objects for pool: {tag}");
        }

        public void ClearPool(string tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                GameLog.Warning($"Pool with tag {tag} doesn't exist.");
                return;
            }

            Queue<GameObject> pool = poolDictionary[tag];
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                Destroy(obj);
            }

            GameLog.Log($"Cleared pool: {tag}");
        }

        public void ClearAllPools()
        {
            foreach (var poolPair in poolDictionary)
            {
                while (poolPair.Value.Count > 0)
                {
                    GameObject obj = poolPair.Value.Dequeue();
                    Destroy(obj);
                }
            }

            poolDictionary.Clear();
            GameLog.Log("All pools cleared");
        }
    }
}
