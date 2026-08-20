using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BundleManager : MonoBehaviour
{
    private class CacheEntry
    {
        public UnityEngine.Object Asset;
        public ResourceRequest Request;
        public int RefCount;
    }

    public static BundleManager Instance { get; private set; }

    private readonly Dictionary<string, CacheEntry> loadedAssets = new Dictionary<string, CacheEntry>();
    private readonly Dictionary<string, List<Action<UnityEngine.Object>>> loadingCallbacks = new Dictionary<string, List<Action<UnityEngine.Object>>>();

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
        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return null;
        }

        if (loadedAssets.TryGetValue(normalizedPath, out CacheEntry cacheEntry))
        {
            cacheEntry.RefCount++;
            return cacheEntry.Asset as T;
        }

        T asset = Resources.Load<T>(normalizedPath);
        if (asset == null)
        {
            Debug.LogWarning($"资源加载失败: {normalizedPath}");
            return null;
        }

        loadedAssets[normalizedPath] = new CacheEntry
        {
            Asset = asset,
            RefCount = 1
        };
        return asset;
    }

    public void LoadAssetAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
    {
        string normalizedPath = NormalizePath(path);
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
        StartCoroutine(LoadAssetAsyncInternal<T>(normalizedPath));
    }

    public bool Contains(string path)
    {
        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return false;
        }

        return loadedAssets.ContainsKey(normalizedPath) || Resources.Load(normalizedPath) != null;
    }

    public void Release(string path)
    {
        string normalizedPath = NormalizePath(path);
        if (string.IsNullOrEmpty(normalizedPath))
        {
            return;
        }

        if (!loadedAssets.TryGetValue(normalizedPath, out CacheEntry cacheEntry))
        {
            return;
        }

        cacheEntry.RefCount--;
        if (cacheEntry.RefCount > 0)
        {
            return;
        }

        if (cacheEntry.Asset != null)
        {
            Resources.UnloadAsset(cacheEntry.Asset);
        }
        loadedAssets.Remove(normalizedPath);
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
            Release(targetKey);
        }
    }

    public void UnloadUnusedAssets()
    {
        List<string> removeKeys = new List<string>();
        foreach (KeyValuePair<string, CacheEntry> pair in loadedAssets)
        {
            if (pair.Value.RefCount <= 0)
            {
                if (pair.Value.Asset != null)
                {
                    Resources.UnloadAsset(pair.Value.Asset);
                }
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
            if (pair.Value.Asset != null)
            {
                Resources.UnloadAsset(pair.Value.Asset);
            }
        }

        loadedAssets.Clear();
        loadingCallbacks.Clear();
    }

    private IEnumerator LoadAssetAsyncInternal<T>(string normalizedPath) where T : UnityEngine.Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(normalizedPath);
        yield return request;

        T asset = request.asset as T;
        if (asset == null)
        {
            Debug.LogWarning($"资源异步加载失败: {normalizedPath}");
        }

        int refCount = 0;
        if (loadingCallbacks.TryGetValue(normalizedPath, out List<Action<UnityEngine.Object>> callbacks))
        {
            refCount = callbacks.Count;
        }

        loadedAssets[normalizedPath] = new CacheEntry
        {
            Asset = asset,
            Request = request,
            RefCount = Mathf.Max(refCount, asset != null ? 1 : 0)
        };

        if (callbacks != null)
        {
            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i]?.Invoke(asset);
            }
        }

        loadingCallbacks.Remove(normalizedPath);
    }

    private string NormalizePath(string path)
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Clear();
            Instance = null;
        }
    }
}