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

        // 프리팹 원본 localScale 캐시(최초 인스턴스화 시 1회 기록). 풀 재사용 시 되돌리기 위함 —
        // 예: 빅슬라임 분열 미니 슬라임이 0.7배로 줄인 뒤 반환되면, 같은 풀(같은 addressableKey)을 쓰는
        // 다음 스폰(일반 슬라임/빅슬라임 본체 등)이 스케일을 재설정하지 않는 한 그대로 작게 나와버렸다.
        private readonly Dictionary<GameObject, Vector3> _defaultScales = new Dictionary<GameObject, Vector3>();

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
                    if (_defaultScales.TryGetValue(cached, out var originalScale))
                    {
                        cached.transform.localScale = originalScale;
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
                _defaultScales[spawned] = spawned.transform.localScale;
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
            _defaultScales.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
