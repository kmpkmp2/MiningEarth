using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DeepEarth.Core
{
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ResourceManager");
                    _instance = go.AddComponent<ResourceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();
        private readonly Dictionary<GameObject, AsyncOperationHandle> _instantiatedObjects = new Dictionary<GameObject, AsyncOperationHandle>();

        public async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
        {
            if (_loadedAssets.TryGetValue(key, out var cachedHandle))
            {
                if (cachedHandle.IsValid())
                {
                    return cachedHandle.Result as T;
                }
                _loadedAssets.Remove(key);
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                _loadedAssets[key] = handle;

                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load Addressable asset with key: {key}");
                    _loadedAssets.Remove(key);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception loading Addressable asset with key '{key}': {ex.Message}");
                _loadedAssets.Remove(key);
                return null;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(key, parent);
                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var spawned = handle.Result;
                    _instantiatedObjects[spawned] = handle;
                    return spawned;
                }
                else
                {
                    Debug.LogError($"Failed to instantiate Addressable prefab with key: {key}");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception instantiating Addressable prefab with key '{key}': {ex.Message}");
                return null;
            }
        }

        public void ReleaseInstance(GameObject obj)
        {
            if (obj == null) return;

            if (_instantiatedObjects.TryGetValue(obj, out var handle))
            {
                Addressables.Release(handle);
                _instantiatedObjects.Remove(obj);
            }
            else
            {
                // Fallback direct destroy if not managed by Addressables
                Destroy(obj);
            }
        }

        public void ReleaseAsset(string key)
        {
            if (_loadedAssets.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _loadedAssets.Remove(key);
            }
        }

        private void OnDestroy()
        {
            CleanUp();
        }

        public void CleanUp()
        {
            foreach (var handle in _instantiatedObjects.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _instantiatedObjects.Clear();

            foreach (var handle in _loadedAssets.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedAssets.Clear();
        }
    }
}
