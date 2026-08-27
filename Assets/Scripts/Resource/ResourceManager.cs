using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceDownloadStatus
{
    public string StatusMessage;
    public string CurrentLabel;
    public long DownloadedBytes;
    public long TotalBytes;
    public float Progress;
    public float DownloadBytesPerSecond;
    public bool HasCatalogUpdate;
    public bool IsDone;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public static ResourceManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("ResourceManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<ResourceManager>();
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
        BundleManager.EnsureInstance();
    }

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        return BundleManager.EnsureInstance().LoadAsset<T>(path);
    }

    public void LoadAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
    {
        BundleManager.EnsureInstance().LoadAssetAsync(path, callback);
    }

    public bool Exists(string path)
    {
        return BundleManager.EnsureInstance().Contains(path);
    }

    public void Release(string path)
    {
        BundleManager.EnsureInstance().Release(path);
    }

    public void Release(UnityEngine.Object asset)
    {
        BundleManager.EnsureInstance().Release(asset);
    }

    public void ReleaseInstance(GameObject instance)
    {
        BundleManager.EnsureInstance().ReleaseInstance(instance);
    }

    public void UnloadUnusedAssets()
    {
        BundleManager.EnsureInstance().UnloadUnusedAssets();
    }

    public bool IsRemoteUpdateInProgress()
    {
        return BundleManager.EnsureInstance().IsRemoteUpdateInProgress;
    }

    public bool IsDependencyDownloaded(string label)
    {
        return BundleManager.EnsureInstance().IsDependencyDownloaded(label);
    }

    public void CheckAndDownloadDependencies(IList<string> labels, Action<ResourceDownloadStatus> onProgress, Action onCompleted, Action<string> onError)
    {
        BundleManager.EnsureInstance().CheckAndDownloadDependencies(labels, onProgress, onCompleted, onError);
    }

    public Sprite LoadSprite(string path)
    {
        return Load<Sprite>(BuildSpritePath(path));
    }

    public void LoadSpriteAsync(string path, Action<Sprite> callback)
    {
        LoadAsync(BuildSpritePath(path), callback);
    }

    public AudioClip LoadAudioClip(string path)
    {
        return Load<AudioClip>(BuildAudioPath(path));
    }

    public void LoadAudioClipAsync(string path, Action<AudioClip> callback)
    {
        LoadAsync(BuildAudioPath(path), callback);
    }

    public GameObject LoadPrefab(string path)
    {
        return Load<GameObject>(BuildPrefabPath(path));
    }

    public void LoadPrefabAsync(string path, Action<GameObject> callback)
    {
        LoadAsync(BuildPrefabPath(path), callback);
    }

    public GameObject InstantiatePrefab(string path, Transform parent = null)
    {
        return BundleManager.EnsureInstance().Instantiate(BuildPrefabPath(path), parent);
    }

    public GameObject InstantiatePrefab(string path, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return BundleManager.EnsureInstance().Instantiate(BuildPrefabPath(path), position, rotation, parent);
    }

    public void InstantiatePrefabAsync(string path, Action<GameObject> callback)
    {
        BundleManager.EnsureInstance().InstantiateAsync(BuildPrefabPath(path), null, callback);
    }

    public void InstantiatePrefabAsync(string path, Transform parent, Action<GameObject> callback)
    {
        BundleManager.EnsureInstance().InstantiateAsync(BuildPrefabPath(path), parent, callback);
    }

    public void InstantiatePrefabAsync(string path, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> callback)
    {
        BundleManager.EnsureInstance().InstantiateAsync(BuildPrefabPath(path), position, rotation, parent, callback);
    }

    public TextAsset LoadTextAsset(string path)
    {
        return Load<TextAsset>(path);
    }

    public string LoadText(string path)
    {
        TextAsset textAsset = LoadTextAsset(path);
        return textAsset != null ? textAsset.text : null;
    }

    private string BuildSpritePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath;
        }

        return BuildPath("Sprites", normalizedPath);
    }

    private string BuildAudioPath(string path)
    {
        return BuildPath("Audios", path);
    }

    private string BuildPrefabPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', '/').Trim();
    }

    private string BuildPath(string root, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath;
        }

        return $"{root}/{normalizedPath}";
    }
}