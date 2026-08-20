using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class BundleManager : MonoBehaviour
{
    private class CacheEntry
    {
        public UnityEngine.Object Asset;
        public AsyncOperationHandle Handle;
        public int RefCount;
        public bool IsAddressable;
    }

    public static BundleManager Instance { get; private set; }

    private readonly Dictionary<string, CacheEntry> loadedAssets = new Dictionary<string, CacheEntry>();
    private readonly Dictionary<string, List<Action<UnityEngine.Object>>> loadingCallbacks = new Dictionary<string, List<Action<UnityEngine.Object>>>();
    private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> loadedInstances = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

    public static BundleManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("BundleManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<BundleManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        string normalizedPath = NormalizePath<T>(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        if (loadedAssets.TryGetValue(normalizedPath, out CacheEntry cacheEntry))
        {
            cacheEntry.RefCount++;
            return cacheEntry.Asset as T;
        }

        if (IsAddressableAsset<T>())
        {
            return LoadAddressableAsset<T>(normalizedPath);
        }

        return LoadResourcesAsset<T>(normalizedPath);
    }

    public void LoadAssetAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
    {
        string normalizedPath = NormalizePath<T>(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            callback?.Invoke(null);
            return;
        }

        if (loadedAssets.TryGetValue(normalizedPath, out CacheEntry cacheEntry) && cacheEntry.Asset != null)
        {
            cacheEntry.RefCount++;
            callback?.Invoke(cacheEntry.Asset as T);
            return;
        }

        if (loadingCallbacks.TryGetValue(normalizedPath, out List<Action<UnityEngine.Object>> callbacks))
        {
            callbacks.Add(asset => callback?.Invoke(asset as T));
            return;
        }

        loadingCallbacks[normalizedPath] = new List<Action<UnityEngine.Object>>
        {
            asset => callback?.Invoke(asset as T)
        };

        if (IsAddressableAsset<T>())
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(normalizedPath);
            handle.Completed += operation => CompleteAddressableLoad(normalizedPath, operation);
            return;
        }

        ResourceRequest request = Resources.LoadAsync<T>(normalizedPath);
        request.completed += _ => CompleteResourcesLoad<T>(normalizedPath, request);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        string normalizedPath = NormalizePrefabPath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(normalizedPath, parent, false);
        GameObject instance = handle.WaitForCompletion();
        if (handle.Status != AsyncOperationStatus.Succeeded || instance == null)
        {
            Debug.LogWarning($"Prefab实例化失败: {normalizedPath}");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            return null;
        }

        loadedInstances[instance] = handle;
        return instance;
    }

    public GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        string normalizedPath = NormalizePrefabPath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(normalizedPath, position, rotation, parent);
        GameObject instance = handle.WaitForCompletion();
        if (handle.Status != AsyncOperationStatus.Succeeded || instance == null)
        {
            Debug.LogWarning($"Prefab实例化失败: {normalizedPath}");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            return null;
        }

        loadedInstances[instance] = handle;
        return instance;
    }

    public void InstantiateAsync(string path, Transform parent, Action<GameObject> callback)
    {
        string normalizedPath = NormalizePrefabPath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            callback?.Invoke(null);
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(normalizedPath, parent, false);
        handle.Completed += operation =>
        {
            GameObject instance = operation.Status == AsyncOperationStatus.Succeeded ? operation.Result : null;
            if (instance == null)
            {
                Debug.LogWarning($"Prefab异步实例化失败: {normalizedPath}");
                if (operation.IsValid())
                {
                    Addressables.Release(operation);
                }
            }
            else
            {
                loadedInstances[instance] = operation;
            }

            callback?.Invoke(instance);
        };
    }

    public void InstantiateAsync(string path, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> callback)
    {
        string normalizedPath = NormalizePrefabPath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            callback?.Invoke(null);
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(normalizedPath, position, rotation, parent);
        handle.Completed += operation =>
        {
            GameObject instance = operation.Status == AsyncOperationStatus.Succeeded ? operation.Result : null;
            if (instance == null)
            {
                Debug.LogWarning($"Prefab异步实例化失败: {normalizedPath}");
                if (operation.IsValid())
                {
                    Addressables.Release(operation);
                }
            }
            else
            {
                loadedInstances[instance] = operation;
            }

            callback?.Invoke(instance);
        };
    }

    public bool Contains(string path)
    {
        string prefabPath = NormalizePrefabPath(path);
        if (!string.IsNullOrEmpty(prefabPath))
        {
            if (loadedAssets.ContainsKey(prefabPath))
            {
                return true;
            }

            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(prefabPath, typeof(GameObject));
            IList<IResourceLocation> locations = handle.WaitForCompletion();
            bool exists = handle.Status == AsyncOperationStatus.Succeeded && locations != null && locations.Count > 0;
            Addressables.Release(handle);
            if (exists)
            {
                return true;
            }
        }

        string resourcePath = NormalizeResourcePath(path);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return false;
        }

        return loadedAssets.ContainsKey(resourcePath) || Resources.Load(resourcePath) != null;
    }

    public void Release(string path)
    {
        string targetKey = FindLoadedKey(path);
        if (string.IsNullOrEmpty(targetKey))
        {
            return;
        }

        ReleaseCacheEntry(targetKey);
    }

    public void Release(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return;
        }

        string targetKey = null;
        foreach (KeyValuePair<string, CacheEntry> pair in loadedAssets)
        {
            if (pair.Value.Asset == asset)
            {
                targetKey = pair.Key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(targetKey))
        {
            ReleaseCacheEntry(targetKey);
        }
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (loadedInstances.Remove(instance))
        {
            Addressables.ReleaseInstance(instance);
            return;
        }

        Destroy(instance);
    }

    public void UnloadUnusedAssets()
    {
        List<string> removeKeys = new List<string>();
        foreach (KeyValuePair<string, CacheEntry> pair in loadedAssets)
        {
            if (pair.Value.RefCount <= 0)
            {
                ReleaseAsset(pair.Value);
                removeKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            loadedAssets.Remove(removeKeys[i]);
        }

        Resources.UnloadUnusedAssets();
    }

    public void Clear()
    {
        foreach (KeyValuePair<string, CacheEntry> pair in loadedAssets)
        {
            ReleaseAsset(pair.Value);
        }

        loadedAssets.Clear();
        loadingCallbacks.Clear();

        foreach (KeyValuePair<GameObject, AsyncOperationHandle<GameObject>> pair in loadedInstances)
        {
            if (pair.Key != null)
            {
                Addressables.ReleaseInstance(pair.Key);
            }
        }

        loadedInstances.Clear();
    }

    private T LoadAddressableAsset<T>(string normalizedPath) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(normalizedPath);
        T asset = handle.WaitForCompletion();
        if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
        {
            Debug.LogWarning($"Addressable资源加载失败: {normalizedPath}");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            return null;
        }

        loadedAssets[normalizedPath] = new CacheEntry
        {
            Asset = asset,
            Handle = handle,
            RefCount = 1,
            IsAddressable = true
        };
        return asset;
    }

    private T LoadResourcesAsset<T>(string normalizedPath) where T : UnityEngine.Object
    {
        T asset = Resources.Load<T>(normalizedPath);
        if (asset == null)
        {
            Debug.LogWarning($"资源加载失败: {normalizedPath}");
            return null;
        }

        loadedAssets[normalizedPath] = new CacheEntry
        {
            Asset = asset,
            RefCount = 1,
            IsAddressable = false
        };
        return asset;
    }

    private void CompleteAddressableLoad<T>(string normalizedPath, AsyncOperationHandle<T> operation) where T : UnityEngine.Object
    {
        T asset = operation.Status == AsyncOperationStatus.Succeeded ? operation.Result : null;
        if (asset == null)
        {
            Debug.LogWarning($"Addressable资源异步加载失败: {normalizedPath}");
            if (operation.IsValid())
            {
                Addressables.Release(operation);
            }
        }

        List<Action<UnityEngine.Object>> callbacks = null;
        int refCount = 0;
        if (loadingCallbacks.TryGetValue(normalizedPath, out callbacks))
        {
            refCount = callbacks.Count;
        }

        if (asset != null)
        {
            loadedAssets[normalizedPath] = new CacheEntry
            {
                Asset = asset,
                Handle = operation,
                RefCount = Mathf.Max(refCount, 1),
                IsAddressable = true
            };
        }

        if (callbacks != null)
        {
            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i]?.Invoke(asset);
            }
        }

        loadingCallbacks.Remove(normalizedPath);
    }

    private void CompleteResourcesLoad<T>(string normalizedPath, ResourceRequest request) where T : UnityEngine.Object
    {
        T asset = request.asset as T;
        if (asset == null)
        {
            Debug.LogWarning($"资源异步加载失败: {normalizedPath}");
        }

        List<Action<UnityEngine.Object>> callbacks = null;
        int refCount = 0;
        if (loadingCallbacks.TryGetValue(normalizedPath, out callbacks))
        {
            refCount = callbacks.Count;
        }

        if (asset != null)
        {
            loadedAssets[normalizedPath] = new CacheEntry
            {
                Asset = asset,
                RefCount = Mathf.Max(refCount, 1),
                IsAddressable = false
            };
        }

        if (callbacks != null)
        {
            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i]?.Invoke(asset);
            }
        }

        loadingCallbacks.Remove(normalizedPath);
    }

    private void ReleaseCacheEntry(string key)
    {
        if (!loadedAssets.TryGetValue(key, out CacheEntry cacheEntry))
        {
            return;
        }

        cacheEntry.RefCount--;
        if (cacheEntry.RefCount > 0)
        {
            return;
        }

        ReleaseAsset(cacheEntry);
        loadedAssets.Remove(key);
    }

    private void ReleaseAsset(CacheEntry cacheEntry)
    {
        if (cacheEntry == null || cacheEntry.Asset == null)
        {
            return;
        }

        if (cacheEntry.IsAddressable)
        {
            if (cacheEntry.Handle.IsValid())
            {
                Addressables.Release(cacheEntry.Handle);
            }
            return;
        }

        Resources.UnloadAsset(cacheEntry.Asset);
    }

    private string FindLoadedKey(string path)
    {
        string prefabPath = NormalizePrefabPath(path);
        if (!string.IsNullOrEmpty(prefabPath) && loadedAssets.ContainsKey(prefabPath))
        {
            return prefabPath;
        }

        string resourcePath = NormalizeResourcePath(path);
        if (!string.IsNullOrEmpty(resourcePath) && loadedAssets.ContainsKey(resourcePath))
        {
            return resourcePath;
        }

        return null;
    }

    private bool IsAddressableAsset<T>() where T : UnityEngine.Object
    {
        return typeof(T) == typeof(GameObject);
    }

    private string NormalizePath<T>(string path) where T : UnityEngine.Object
    {
        return IsAddressableAsset<T>() ? NormalizePrefabPath(path) : NormalizeResourcePath(path);
    }

    private string NormalizeResourcePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Resources/".Length);
        }

        int extensionIndex = normalizedPath.LastIndexOf('.');
        if (extensionIndex > normalizedPath.LastIndexOf('/'))
        {
            normalizedPath = normalizedPath.Substring(0, extensionIndex);
        }

        return normalizedPath;
    }

    private string NormalizePrefabPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("Assets/Prefab/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Assets/Prefab/".Length);
        }
        else if (normalizedPath.StartsWith("Assets/Prefabs/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Assets/Prefabs/".Length);
        }
        else if (normalizedPath.StartsWith("Prefab/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Prefab/".Length);
        }
        else if (normalizedPath.StartsWith("Prefabs/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Prefabs/".Length);
        }

        return normalizedPath;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Clear();
            Instance = null;
        }
    }
}