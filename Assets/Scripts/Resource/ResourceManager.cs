using System;
using UnityEngine;

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

    public void UnloadUnusedAssets()
    {
        BundleManager.EnsureInstance().UnloadUnusedAssets();
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
        return BuildPath("Sprites", path);
    }

    private string BuildAudioPath(string path)
    {
        return BuildPath("Audios", path);
    }

    private string BuildPrefabPath(string path)
    {
        return BuildPath("Prefabs", path);
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