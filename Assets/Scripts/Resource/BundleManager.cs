using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class BundleManager : MonoBehaviour
{
    private class CacheEntry
    {
        public UnityEngine.Object Asset;
        public AsyncOperationHandle Handle;
        public int RefCount;
        public bool IsAddressable;
#if UNITY_EDITOR
        public bool IsEditorAsset;
#endif
    }

    private class RemoteDownloadTarget
    {
        public string DisplayName;
        public IResourceLocation Location;
    }

    public static BundleManager Instance { get; private set; }

    public bool IsRemoteUpdateInProgress => remoteUpdateCoroutine != null;

    private const string DownloadLogPrefix = "[BundleManager][Download]";

    private readonly Dictionary<string, CacheEntry> loadedAssets = new Dictionary<string, CacheEntry>();
    private readonly Dictionary<string, List<Action<UnityEngine.Object>>> loadingCallbacks = new Dictionary<string, List<Action<UnityEngine.Object>>>();
    private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> loadedInstances = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();
    private Coroutine remoteUpdateCoroutine;

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

#if UNITY_EDITOR
        if (TryLoadEditorAsset(normalizedPath, out T editorAsset))
        {
            loadedAssets[normalizedPath] = new CacheEntry
            {
                Asset = editorAsset,
                RefCount = 1,
                IsAddressable = false,
                IsEditorAsset = true
            };
            return editorAsset;
        }
#endif

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

#if UNITY_EDITOR
        if (TryLoadEditorAsset(normalizedPath, out T editorAsset))
        {
            CompleteEditorLoad(normalizedPath, editorAsset);
            return;
        }
#endif

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

#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorAsset<GameObject>(normalizedPath);
        if (editorPrefab != null)
        {
            GameObject editorInstance = Instantiate(editorPrefab, parent, false);
            editorInstance.name = editorPrefab.name;
            return editorInstance;
        }
#endif

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

#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorAsset<GameObject>(normalizedPath);
        if (editorPrefab != null)
        {
            GameObject editorInstance = Instantiate(editorPrefab, position, rotation, parent);
            editorInstance.name = editorPrefab.name;
            return editorInstance;
        }
#endif

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

#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorAsset<GameObject>(normalizedPath);
        if (editorPrefab != null)
        {
            GameObject editorInstance = Instantiate(editorPrefab, parent, false);
            editorInstance.name = editorPrefab.name;
            callback?.Invoke(editorInstance);
            return;
        }
#endif

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

#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorAsset<GameObject>(normalizedPath);
        if (editorPrefab != null)
        {
            GameObject editorInstance = Instantiate(editorPrefab, position, rotation, parent);
            editorInstance.name = editorPrefab.name;
            callback?.Invoke(editorInstance);
            return;
        }
#endif

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

#if UNITY_EDITOR
            if (EditorAssetExists<GameObject>(prefabPath))
            {
                return true;
            }
#endif

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

    public bool IsDependencyDownloaded(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return true;
        }

        AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(label.Trim());
        long size = sizeHandle.WaitForCompletion();
        bool isDownloaded = sizeHandle.Status == AsyncOperationStatus.Succeeded && size <= 0L;
        if (sizeHandle.IsValid())
        {
            Addressables.Release(sizeHandle);
        }

        return isDownloaded;
    }

    public void CheckAndDownloadDependencies(IList<string> labels, Action<ResourceDownloadStatus> onProgress, Action onCompleted, Action<string> onError)
    {
        if (remoteUpdateCoroutine != null)
        {
            onError?.Invoke("A resource update is already in progress.");
            return;
        }

        remoteUpdateCoroutine = StartCoroutine(CheckAndDownloadDependenciesCoroutine(labels, onProgress, onCompleted, onError));
    }

    private IEnumerator CheckAndDownloadDependenciesCoroutine(IList<string> labels, Action<ResourceDownloadStatus> onProgress, Action onCompleted, Action<string> onError)
    {
        bool useAllRemoteTargets = labels == null || labels.Count == 0;
        ResourceDownloadStatus progress = new ResourceDownloadStatus();
        long downloadedBytes = 0L;
        long totalBytes = 0L;
        float downloadBytesPerSecond = 0f;
        float nextProgressLogTime = 0f;

        try
        {
            Debug.Log($"{DownloadLogPrefix} Begin update flow. UseAllRemoteTargets={useAllRemoteTargets}, InputLabelCount={(labels != null ? labels.Count : 0)}");

            ReportProgress(onProgress, progress, "Initializing resource system...", string.Empty, 0L, 0L, 0f, false, false);
            Debug.Log($"{DownloadLogPrefix} Initializing Addressables.");

            AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync(false);
            yield return initHandle;
            if (initHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string errorMessage = GetHandleErrorMessage(initHandle, "Failed to initialize the resource system.");
                Debug.LogError($"{DownloadLogPrefix} Initialize failed. {errorMessage}");
                ReleaseHandle(initHandle);
                onError?.Invoke(errorMessage);
                yield break;
            }
            Debug.Log($"{DownloadLogPrefix} Addressables initialized successfully.");
            ReleaseHandle(initHandle);

            ReportProgress(onProgress, progress, "Checking for resource updates...", string.Empty, 0L, 0L, 0f, false, false);
            Debug.Log($"{DownloadLogPrefix} Checking for catalog updates.");
            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;
            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string errorMessage = GetHandleErrorMessage(checkHandle, "Failed to check for resource updates.");
                Debug.LogError($"{DownloadLogPrefix} Catalog update check failed. {errorMessage}");
                ReleaseHandle(checkHandle);
                onError?.Invoke(errorMessage);
                yield break;
            }

            List<string> catalogsToUpdate = checkHandle.Result != null ? new List<string>(checkHandle.Result) : new List<string>();
            bool hasCatalogUpdate = catalogsToUpdate.Count > 0;
            Debug.Log($"{DownloadLogPrefix} Catalog check finished. HasCatalogUpdate={hasCatalogUpdate}, CatalogCount={catalogsToUpdate.Count}");
            if (hasCatalogUpdate)
            {
                Debug.Log($"{DownloadLogPrefix} Catalogs to update: {string.Join(", ", catalogsToUpdate)}");
            }
            ReleaseHandle(checkHandle);

            if (hasCatalogUpdate)
            {
                ReportProgress(onProgress, progress, "Update found. Syncing catalog...", string.Empty, 0L, 0L, 0f, true, false);
                Debug.Log($"{DownloadLogPrefix} Updating catalogs.");
                AsyncOperationHandle<List<IResourceLocator>> updateCatalogHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                yield return updateCatalogHandle;
                if (updateCatalogHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    string errorMessage = GetHandleErrorMessage(updateCatalogHandle, "Failed to sync the resource catalog.");
                    Debug.LogError($"{DownloadLogPrefix} Catalog update failed. {errorMessage}");
                    ReleaseHandle(updateCatalogHandle);
                    onError?.Invoke(errorMessage);
                    yield break;
                }

                Debug.Log($"{DownloadLogPrefix} Catalog update completed.");
                ReleaseHandle(updateCatalogHandle);
            }

            List<string> targets = BuildDownloadTargetList(labels);
            List<RemoteDownloadTarget> remoteTargets = useAllRemoteTargets ? BuildRemoteDownloadTargets() : null;
            int targetCount = useAllRemoteTargets ? (remoteTargets != null ? remoteTargets.Count : 0) : targets.Count;
            Debug.Log($"{DownloadLogPrefix} Target discovery completed after initialization. TargetCount={targetCount}");
            if (useAllRemoteTargets)
            {
                if (remoteTargets != null && remoteTargets.Count > 0)
                {
                    Debug.Log($"{DownloadLogPrefix} Targets: {string.Join(", ", GetRemoteTargetDisplayNames(remoteTargets))}");
                }
            }
            else if (targets.Count > 0)
            {
                Debug.Log($"{DownloadLogPrefix} Targets: {string.Join(", ", targets)}");
            }

            if (targetCount == 0)
            {
                Debug.Log($"{DownloadLogPrefix} No remote targets found after initialization, finishing without download.");
                ReportProgress(onProgress, progress, "No remote resources need to be downloaded.", string.Empty, 0L, 0L, 0f, hasCatalogUpdate, true);
                onCompleted?.Invoke();
                yield break;
            }

            ReportProgress(onProgress, progress, "Calculating download size...", string.Empty, 0L, 0L, 0f, hasCatalogUpdate, false);
            Debug.Log($"{DownloadLogPrefix} Calculating total download size.");
            if (useAllRemoteTargets)
            {
                for (int i = 0; i < remoteTargets.Count; i++)
                {
                    RemoteDownloadTarget remoteTarget = remoteTargets[i];
                    long remoteTargetSize = GetRemoteBundleSize(remoteTarget.Location);
                    totalBytes += remoteTargetSize;
                    Debug.Log($"{DownloadLogPrefix} Remote target size. Target={remoteTarget.DisplayName}, Size={FormatSizeForLog(remoteTargetSize)} ({remoteTargetSize} bytes)");
                }
            }
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    string label = targets[i];
                    AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(label);
                    yield return sizeHandle;
                    if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        string errorMessage = GetHandleErrorMessage(sizeHandle, $"Failed to get download size: {label}");
                        Debug.LogError($"{DownloadLogPrefix} Size calculation failed for label={label}. {errorMessage}");
                        ReleaseHandle(sizeHandle);
                        onError?.Invoke(errorMessage);
                        yield break;
                    }

                    long labelSize = Math.Max(0L, sizeHandle.Result);
                    totalBytes += labelSize;
                    Debug.Log($"{DownloadLogPrefix} Label size. Label={label}, Size={FormatSizeForLog(labelSize)} ({labelSize} bytes)");
                    ReleaseHandle(sizeHandle);
                }
            }

            Debug.Log($"{DownloadLogPrefix} Total download size confirmed: {FormatSizeForLog(totalBytes)} ({totalBytes} bytes)");
            ReportProgress(onProgress, progress, totalBytes > 0L ? "Preparing remote download..." : "Resources are already up to date.", string.Empty, 0L, totalBytes, 0f, hasCatalogUpdate, totalBytes <= 0L);
            if (totalBytes <= 0L)
            {
                Debug.Log($"{DownloadLogPrefix} Total download size is zero, resources are already up to date.");
                onCompleted?.Invoke();
                yield break;
            }

            if (useAllRemoteTargets)
            {
                List<IResourceLocation> bundleLocations = new List<IResourceLocation>(remoteTargets.Count);
                for (int i = 0; i < remoteTargets.Count; i++)
                {
                    bundleLocations.Add(remoteTargets[i].Location);
                }

                Debug.Log($"{DownloadLogPrefix} Starting combined download for all remote bundle targets. BundleCount={bundleLocations.Count}");
                AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(bundleLocations, false);
                long reportedDownloadedBytes = 0L;
                long lastDownloadedBytes = 0L;
                float lastSampleTime = Time.realtimeSinceStartup;
                nextProgressLogTime = 0f;
                while (!downloadHandle.IsDone)
                {
                    if (downloadHandle.Status == AsyncOperationStatus.Failed)
                    {
                        string errorMessage = GetHandleErrorMessage(downloadHandle, "Failed to download all remote resources.");
                        Debug.LogError($"{DownloadLogPrefix} Combined remote bundle download failed. {errorMessage}");
                        ReleaseHandle(downloadHandle);
                        onError?.Invoke(errorMessage);
                        yield break;
                    }

                    DownloadStatus downloadStatus = downloadHandle.GetDownloadStatus();
                    reportedDownloadedBytes = Math.Max(reportedDownloadedBytes, downloadStatus.DownloadedBytes);
                    float currentSampleTime = Time.realtimeSinceStartup;
                    downloadBytesPerSecond = CalculateDownloadBytesPerSecond(reportedDownloadedBytes, ref lastDownloadedBytes, ref lastSampleTime, currentSampleTime, downloadBytesPerSecond);
                    if (currentSampleTime >= nextProgressLogTime)
                    {
                        Debug.Log($"{DownloadLogPrefix} Progress. Target=All remote bundles, Downloaded={FormatSizeForLog(reportedDownloadedBytes)}/{FormatSizeForLog(totalBytes)}, Speed={FormatSizeForLog((long)downloadBytesPerSecond)}/s, Percent={(totalBytes > 0L ? (reportedDownloadedBytes * 100f / totalBytes) : 0f):0.00}%");
                        nextProgressLogTime = currentSampleTime + 0.5f;
                    }
                    ReportProgress(onProgress, progress, "Downloading all remote resources...", "All remote bundles", reportedDownloadedBytes, totalBytes, downloadBytesPerSecond, hasCatalogUpdate, false);
                    yield return null;
                }

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    string errorMessage = GetHandleErrorMessage(downloadHandle, "Failed to download all remote resources.");
                    Debug.LogError($"{DownloadLogPrefix} Combined remote bundle download finished with failure status. {errorMessage}");
                    ReleaseHandle(downloadHandle);
                    onError?.Invoke(errorMessage);
                    yield break;
                }

                downloadedBytes = totalBytes;
                downloadBytesPerSecond = 0f;
                Debug.Log($"{DownloadLogPrefix} Combined remote bundle download completed. Downloaded={FormatSizeForLog(downloadedBytes)} ({downloadedBytes} bytes)");
                ReportProgress(onProgress, progress, "All remote resources downloaded.", "All remote bundles", downloadedBytes, totalBytes, 0f, hasCatalogUpdate, false);
                ReleaseHandle(downloadHandle);
            }
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    string label = targets[i];
                    AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(label);
                    yield return sizeHandle;
                    if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        string errorMessage = GetHandleErrorMessage(sizeHandle, $"Failed to get label size: {label}");
                        Debug.LogError($"{DownloadLogPrefix} Failed to query label size before download. Label={label}. {errorMessage}");
                        ReleaseHandle(sizeHandle);
                        onError?.Invoke(errorMessage);
                        yield break;
                    }

                    long labelTotalBytes = Math.Max(0L, sizeHandle.Result);
                    ReleaseHandle(sizeHandle);

                    if (labelTotalBytes <= 0L)
                    {
                        Debug.Log($"{DownloadLogPrefix} Skip label because it is already up to date. Label={label}");
                        ReportProgress(onProgress, progress, $"{label} is already up to date.", label, downloadedBytes, totalBytes, 0f, hasCatalogUpdate, false);
                        continue;
                    }

                    Debug.Log($"{DownloadLogPrefix} Start label download. Label={label}, Size={FormatSizeForLog(labelTotalBytes)} ({labelTotalBytes} bytes)");
                    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(label, false);
                    long lastDownloadedBytes = 0L;
                    float lastSampleTime = Time.realtimeSinceStartup;
                    nextProgressLogTime = 0f;
                    while (!downloadHandle.IsDone)
                    {
                        if (downloadHandle.Status == AsyncOperationStatus.Failed)
                        {
                            string errorMessage = GetHandleErrorMessage(downloadHandle, $"Download failed: {label}");
                            Debug.LogError($"{DownloadLogPrefix} Label download failed. Label={label}. {errorMessage}");
                            ReleaseHandle(downloadHandle);
                            onError?.Invoke(errorMessage);
                            yield break;
                        }

                        DownloadStatus downloadStatus = downloadHandle.GetDownloadStatus();
                        long currentDownloadedBytes = downloadedBytes + downloadStatus.DownloadedBytes;
                        float currentSampleTime = Time.realtimeSinceStartup;
                        downloadBytesPerSecond = CalculateDownloadBytesPerSecond(downloadStatus.DownloadedBytes, ref lastDownloadedBytes, ref lastSampleTime, currentSampleTime, downloadBytesPerSecond);
                        if (currentSampleTime >= nextProgressLogTime)
                        {
                            Debug.Log($"{DownloadLogPrefix} Progress. Label={label}, LabelDownloaded={FormatSizeForLog(downloadStatus.DownloadedBytes)}/{FormatSizeForLog(labelTotalBytes)}, TotalDownloaded={FormatSizeForLog(currentDownloadedBytes)}/{FormatSizeForLog(totalBytes)}, Speed={FormatSizeForLog((long)downloadBytesPerSecond)}/s, LabelPercent={(labelTotalBytes > 0L ? (downloadStatus.DownloadedBytes * 100f / labelTotalBytes) : 0f):0.00}%");
                            nextProgressLogTime = currentSampleTime + 0.5f;
                        }
                        ReportProgress(onProgress, progress, $"Downloading: {label}", label, currentDownloadedBytes, totalBytes, downloadBytesPerSecond, hasCatalogUpdate, false);
                        yield return null;
                    }

                    if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        string errorMessage = GetHandleErrorMessage(downloadHandle, $"Download failed: {label}");
                        Debug.LogError($"{DownloadLogPrefix} Label download finished with failure status. Label={label}. {errorMessage}");
                        ReleaseHandle(downloadHandle);
                        onError?.Invoke(errorMessage);
                        yield break;
                    }

                    downloadedBytes += labelTotalBytes;
                    downloadBytesPerSecond = 0f;
                    Debug.Log($"{DownloadLogPrefix} Label download completed. Label={label}, AccumulatedDownloaded={FormatSizeForLog(downloadedBytes)}/{FormatSizeForLog(totalBytes)}");
                    ReportProgress(onProgress, progress, $"Completed: {label}", label, downloadedBytes, totalBytes, 0f, hasCatalogUpdate, false);
                    ReleaseHandle(downloadHandle);
                }
            }

            Debug.Log($"{DownloadLogPrefix} Update flow completed successfully.");
            ReportProgress(onProgress, progress, "Resource download completed.", string.Empty, totalBytes, totalBytes, 0f, hasCatalogUpdate, true);
            onCompleted?.Invoke();
        }
        finally
        {
            Debug.Log($"{DownloadLogPrefix} Update flow ended. Clearing running coroutine reference.");
            remoteUpdateCoroutine = null;
        }
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

#if UNITY_EDITOR
        if (cacheEntry.IsEditorAsset)
        {
            return;
        }
#endif

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
        Type assetType = typeof(T);
        return assetType == typeof(GameObject) || assetType == typeof(Sprite);
    }

    private string NormalizePath<T>(string path) where T : UnityEngine.Object
    {
        return IsAddressableAsset<T>() ? NormalizePrefabPath(path) : NormalizeResourcePath(path);
    }

    private List<string> BuildDownloadTargetList(IList<string> labels)
    {
        List<string> targets = new List<string>();
        bool hasExplicitLabels = labels != null && labels.Count > 0;

        if (!hasExplicitLabels)
        {
            return targets;
        }

        for (int i = 0; i < labels.Count; i++)
        {
            string label = labels[i];
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            string trimmedLabel = label.Trim();
            if (!targets.Contains(trimmedLabel))
            {
                targets.Add(trimmedLabel);
            }
        }

        return targets;
    }

    private List<RemoteDownloadTarget> BuildRemoteDownloadTargets()
    {
        List<RemoteDownloadTarget> targets = new List<RemoteDownloadTarget>();
        HashSet<string> seenLocationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator == null || locator.Keys == null)
            {
                continue;
            }

            foreach (object key in locator.Keys)
            {
                if (key == null)
                {
                    continue;
                }

                if (!TryGetRemoteBundleLocations(locator, key, out List<IResourceLocation> remoteLocations))
                {
                    continue;
                }

                for (int i = 0; i < remoteLocations.Count; i++)
                {
                    IResourceLocation remoteLocation = remoteLocations[i];
                    string uniqueLocationId = GetRemoteTargetUniqueId(remoteLocation);
                    if (string.IsNullOrEmpty(uniqueLocationId) || !seenLocationIds.Add(uniqueLocationId))
                    {
                        continue;
                    }

                    targets.Add(new RemoteDownloadTarget
                    {
                        DisplayName = GetRemoteTargetDisplayName(key, remoteLocation),
                        Location = remoteLocation
                    });
                }
            }
        }

        return targets;
    }

    private string[] GetRemoteTargetDisplayNames(List<RemoteDownloadTarget> remoteTargets)
    {
        if (remoteTargets == null || remoteTargets.Count == 0)
        {
            return Array.Empty<string>();
        }

        string[] displayNames = new string[remoteTargets.Count];
        for (int i = 0; i < remoteTargets.Count; i++)
        {
            RemoteDownloadTarget remoteTarget = remoteTargets[i];
            displayNames[i] = remoteTarget != null && !string.IsNullOrEmpty(remoteTarget.DisplayName)
                ? remoteTarget.DisplayName
                : $"RemoteTarget_{i}";
        }

        return displayNames;
    }

    private string GetRemoteTargetUniqueId(IResourceLocation remoteLocation)
    {
        if (remoteLocation == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(remoteLocation.InternalId))
        {
            return remoteLocation.InternalId;
        }

        if (remoteLocation.PrimaryKey != null)
        {
            return remoteLocation.PrimaryKey;
        }

        return remoteLocation.ToString();
    }

    private string GetRemoteTargetDisplayName(object key, IResourceLocation remoteLocation)
    {
        if (key is string stringKey && !string.IsNullOrEmpty(stringKey))
        {
            return stringKey;
        }

        if (remoteLocation != null)
        {
            if (!string.IsNullOrEmpty(remoteLocation.PrimaryKey))
            {
                return remoteLocation.PrimaryKey;
            }

            if (!string.IsNullOrEmpty(remoteLocation.InternalId))
            {
                int slashIndex = remoteLocation.InternalId.LastIndexOf('/');
                return slashIndex >= 0 && slashIndex < remoteLocation.InternalId.Length - 1
                    ? remoteLocation.InternalId.Substring(slashIndex + 1)
                    : remoteLocation.InternalId;
            }
        }

        return key != null ? key.ToString() : "RemoteTarget";
    }

    private bool TryGetRemoteBundleLocations(IResourceLocator locator, object key, out List<IResourceLocation> remoteLocations)
    {
        remoteLocations = null;
        if (locator == null || key == null)
        {
            return false;
        }

        if (!locator.Locate(key, null, out IList<IResourceLocation> locations) || locations == null)
        {
            return false;
        }

        List<IResourceLocation> bundleLocations = new List<IResourceLocation>();
        for (int i = 0; i < locations.Count; i++)
        {
            CollectRemoteBundleLocations(locations[i], bundleLocations);
        }

        if (bundleLocations.Count <= 0)
        {
            return false;
        }

        remoteLocations = bundleLocations;
        return true;
    }

    private void CollectRemoteBundleLocations(IResourceLocation location, List<IResourceLocation> result)
    {
        if (location == null || result == null)
        {
            return;
        }

        if (IsRemoteBundleLocation(location))
        {
            result.Add(location);
        }

        if (!location.HasDependencies || location.Dependencies == null)
        {
            return;
        }

        for (int i = 0; i < location.Dependencies.Count; i++)
        {
            CollectRemoteBundleLocations(location.Dependencies[i], result);
        }
    }

    private bool IsRemoteBundleLocation(IResourceLocation location)
    {
        if (location == null || string.IsNullOrEmpty(location.InternalId))
        {
            return false;
        }

        if (!location.InternalId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !location.InternalId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return location.ResourceType == typeof(IAssetBundleResource);
    }

    private long GetRemoteBundleSize(IResourceLocation location)
    {
        if (location == null)
        {
            return 0L;
        }

        ILocationSizeData sizeData = location.Data as ILocationSizeData;
        if (sizeData == null)
        {
            return 0L;
        }

        return Math.Max(0L, sizeData.ComputeSize(location, Addressables.ResourceManager));
    }

    private float CalculateDownloadBytesPerSecond(long currentDownloadedBytes, ref long lastDownloadedBytes, ref float lastSampleTime, float currentSampleTime, float fallbackSpeed)
    {
        float deltaTime = currentSampleTime - lastSampleTime;
        long deltaBytes = currentDownloadedBytes - lastDownloadedBytes;
        lastDownloadedBytes = currentDownloadedBytes;
        lastSampleTime = currentSampleTime;

        if (deltaTime <= Mathf.Epsilon)
        {
            return Mathf.Max(0f, fallbackSpeed);
        }

        return Mathf.Max(0f, deltaBytes / deltaTime);
    }

    private string FormatSizeForLog(long bytes)
    {
        long safeBytes = Math.Max(0L, bytes);
        if (safeBytes < 1024L)
        {
            return $"{safeBytes} B";
        }

        float kiloBytes = safeBytes / 1024f;
        if (kiloBytes < 1024f)
        {
            return $"{kiloBytes:0.0} KB";
        }

        float megaBytes = kiloBytes / 1024f;
        if (megaBytes < 1024f)
        {
            return $"{megaBytes:0.0} MB";
        }

        return $"{megaBytes / 1024f:0.0} GB";
    }

    private void ReportProgress(Action<ResourceDownloadStatus> onProgress, ResourceDownloadStatus progress, string statusMessage, string currentLabel, long downloadedBytes, long totalBytes, float downloadBytesPerSecond, bool hasCatalogUpdate, bool isDone)
    {
        if (progress == null)
        {
            return;
        }

        progress.StatusMessage = statusMessage;
        progress.CurrentLabel = currentLabel;
        progress.DownloadedBytes = Math.Max(0L, downloadedBytes);
        progress.TotalBytes = Math.Max(0L, totalBytes);
        progress.Progress = progress.TotalBytes > 0L ? Mathf.Clamp01((float)progress.DownloadedBytes / progress.TotalBytes) : (isDone ? 1f : 0f);
        progress.DownloadBytesPerSecond = Mathf.Max(0f, downloadBytesPerSecond);
        progress.HasCatalogUpdate = hasCatalogUpdate;
        progress.IsDone = isDone;
        onProgress?.Invoke(progress);
    }

    private void ReleaseHandle<T>(AsyncOperationHandle<T> handle)
    {
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }

    private void ReleaseHandle(AsyncOperationHandle handle)
    {
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }

    private string GetHandleErrorMessage<T>(AsyncOperationHandle<T> handle, string defaultMessage)
    {
        if (handle.OperationException != null && !string.IsNullOrEmpty(handle.OperationException.Message))
        {
            return $"{defaultMessage}\n{handle.OperationException.Message}";
        }

        return defaultMessage;
    }

    private string GetHandleErrorMessage(AsyncOperationHandle handle, string defaultMessage)
    {
        if (handle.OperationException != null && !string.IsNullOrEmpty(handle.OperationException.Message))
        {
            return $"{defaultMessage}\n{handle.OperationException.Message}";
        }

        return defaultMessage;
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

#if UNITY_EDITOR
    private bool TryLoadEditorAsset<T>(string normalizedPath, out T asset) where T : UnityEngine.Object
    {
        asset = LoadEditorAsset<T>(normalizedPath);
        return asset != null;
    }

    private T LoadEditorAsset<T>(string normalizedPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        string[] candidatePaths = BuildEditorAssetCandidatePaths<T>(normalizedPath);
        for (int i = 0; i < candidatePaths.Length; i++)
        {
            string candidatePath = candidatePaths[i];
            T asset = AssetDatabase.LoadAssetAtPath<T>(candidatePath);
            if (asset != null)
            {
                return asset;
            }
        }

        return null;
    }

    private bool EditorAssetExists<T>(string normalizedPath) where T : UnityEngine.Object
    {
        return LoadEditorAsset<T>(normalizedPath) != null;
    }

    private string[] BuildEditorAssetCandidatePaths<T>(string normalizedPath) where T : UnityEngine.Object
    {
        string pathWithoutExtension = RemoveExtension(normalizedPath.Replace('\\', '/').Trim());
        string extension = GetEditorAssetExtension<T>();

        if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                normalizedPath,
                pathWithoutExtension + extension
            };
        }

        if (typeof(T) == typeof(GameObject))
        {
            return new[]
            {
                "Assets/Prefab/" + pathWithoutExtension + extension,
                "Assets/Prefabs/" + pathWithoutExtension + extension,
                "Assets/Resources/" + pathWithoutExtension + extension,
                "Assets/" + pathWithoutExtension + extension
            };
        }

        return new[]
        {
            "Assets/Resources/" + pathWithoutExtension + extension,
            "Assets/" + pathWithoutExtension + extension
        };
    }

    private string GetEditorAssetExtension<T>() where T : UnityEngine.Object
    {
        Type assetType = typeof(T);
        if (assetType == typeof(GameObject))
        {
            return ".prefab";
        }

        if (assetType == typeof(Sprite) || assetType == typeof(Texture2D))
        {
            return ".png";
        }

        if (assetType == typeof(AudioClip))
        {
            return ".wav";
        }

        if (assetType == typeof(TextAsset))
        {
            return ".txt";
        }

        return string.Empty;
    }

    private void CompleteEditorLoad(string normalizedPath, UnityEngine.Object asset)
    {
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
                IsAddressable = false,
                IsEditorAsset = true
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
#endif

    private string RemoveExtension(string path)
    {
        int extensionIndex = path.LastIndexOf('.');
        if (extensionIndex > path.LastIndexOf('/'))
        {
            return path.Substring(0, extensionIndex);
        }

        return path;
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