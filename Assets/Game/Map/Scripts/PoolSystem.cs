using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.Map
{
    public class PoolSystem : MonoBehaviour
    {
        private static PoolSystem _instance;
        public static PoolSystem Instance => _instance;

        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<GameObject, string> _activeObjects = new Dictionary<GameObject, string>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async UniTask<GameObject> GetAsync(string key, Transform parent = null)
        {
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }

            var queue = _pools[key];
            while (queue.Count > 0)
            {
                var cached = queue.Dequeue();
                if (cached != null)
                {
                    cached.SetActive(true);
                    if (parent != null)
                    {
                        cached.transform.SetParent(parent, false);
                    }
                    _activeObjects[cached] = key;
                    return cached;
                }
            }

            // Pool is empty, instantiate via ResourceManager
            GameObject spawned = await ResourceManager.Instance.InstantiateAsync(key, parent);
            if (spawned != null)
            {
                _activeObjects[spawned] = key;
            }
            return spawned;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (_activeObjects.TryGetValue(obj, out string key))
            {
                obj.SetActive(false);
                obj.transform.SetParent(transform, false);
                _pools[key].Enqueue(obj);
                _activeObjects.Remove(obj);
            }
            else
            {
                // Fallback: release via ResourceManager if not registered in active list
                ResourceManager.Instance.ReleaseInstance(obj);
            }
        }

        public void Clear()
        {
            foreach (var kvp in _pools)
            {
                var queue = kvp.Value;
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    if (obj != null)
                    {
                        ResourceManager.Instance.ReleaseInstance(obj);
                    }
                }
            }
            _pools.Clear();
            _activeObjects.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
