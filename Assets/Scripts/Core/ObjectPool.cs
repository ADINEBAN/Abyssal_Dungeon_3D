using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.Core
{
    public class ObjectPool<T> where T : Component
    {
        private T _prefab;
        private Transform _parent;
        private Queue<T> _pool;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new Queue<T>();

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNewObject();
                _pool.Enqueue(obj);
            }
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }

        public T Get()
        {
            if (_pool.Count == 0)
            {
                T obj = CreateNewObject();
                obj.gameObject.SetActive(true);
                return obj;
            }

            T pooledObj = _pool.Dequeue();
            pooledObj.gameObject.SetActive(true);
            return pooledObj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public class PoolManager : Singleton<PoolManager>
    {
        private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();

        public void CreatePool(string poolKey, GameObject prefab, int poolSize)
        {
            if (!_poolDictionary.ContainsKey(poolKey))
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                GameObject poolParent = new GameObject(poolKey + "_Pool");
                poolParent.transform.SetParent(this.transform);

                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(prefab, poolParent.transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                _poolDictionary.Add(poolKey, objectPool);
            }
        }

        public GameObject SpawnFromPool(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(poolKey))
            {
                Debug.LogWarning($"[PoolManager] Pool with key {poolKey} doesn't exist.");
                return null;
            }

            if (_poolDictionary[poolKey].Count == 0)
            {
                Debug.LogWarning($"[PoolManager] Pool {poolKey} is empty.");
                return null;
            }

            GameObject objToSpawn = _poolDictionary[poolKey].Dequeue();
            
            objToSpawn.SetActive(true);
            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;

            return objToSpawn;
        }

        public void ReturnToPool(string poolKey, GameObject obj)
        {
            if (!_poolDictionary.ContainsKey(poolKey))
            {
                Debug.LogWarning($"[PoolManager] Pool with key {poolKey} doesn't exist.");
                return;
            }

            obj.SetActive(false);
            _poolDictionary[poolKey].Enqueue(obj);
        }
    }
}
