using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// Lua 热更管理器：在 Lua 虚拟机启动前下载并落地远端 Lua 文件。
/// </summary>
public class LuaHotUpdateManager : MonoBehaviour
{
    private const string DefaultLuaLabels = "lua";
    private const string DefaultManifestAddress = "Lua/LuaManifest.json";
    private const string LuaUpdateUIPrefabPath = "UI/LuaUpdateUI";
    private const float CloseUIDelay = 0.35f;

    public static LuaHotUpdateManager Instance { get; private set; }

    public bool IsUpdating { get; private set; }

    private LuaUpdateUIView updateView;
    private GameObject updateViewObject;
    private GameObject updateCanvasObject;
    private GameObject updateEventSystemObject;

    public static LuaHotUpdateManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("LuaHotUpdateManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<LuaHotUpdateManager>();
    }

    public static IEnumerator RunBeforeLuaStartup()
    {
        yield return EnsureInstance().RunUpdateWithUICoroutine(DefaultLuaLabels, DefaultManifestAddress);
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

    private IEnumerator RunUpdateWithUICoroutine(string labels, string manifestAddress)
    {
        yield return ShowUpdateUICoroutine();

        bool shouldRetry = false;
        do
        {
            shouldRetry = false;
            bool updateFinished = false;
            bool updateSucceeded = false;
            string updateError = null;

            yield return UpdateLuaFilesCoroutine(labels, manifestAddress, success =>
            {
                updateSucceeded = success;
                updateFinished = true;
            }, error =>
            {
                updateError = error;
                updateSucceeded = false;
                updateFinished = true;
            });

            while (!updateFinished)
            {
                yield return null;
            }

            if (updateSucceeded)
            {
                if (updateView != null)
                {
                    yield return new WaitForSecondsRealtime(CloseUIDelay);
                }
                break;
            }

            if (updateView == null)
            {
                Debug.LogWarning($"[LuaHotUpdate] Lua update failed without UI, fallback to local Lua. Error: {updateError}");
                break;
            }

            bool retryClicked = false;
            updateView.Initialize(() =>
            {
                retryClicked = true;
            });
            updateView.SetStatus(string.IsNullOrEmpty(updateError) ? "Lua update failed." : updateError);
            updateView.ShowRetry();

            while (!retryClicked)
            {
                yield return null;
            }

            shouldRetry = true;
        }
        while (shouldRetry);

        HideUpdateUI();
    }

    private IEnumerator UpdateLuaFilesCoroutine(string labels, string manifestAddress, Action<bool> onCompleted, Action<string> onError)
    {
        if (IsUpdating)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        IsUpdating = true;
        Directory.CreateDirectory(LuaConst.luaResDir);

        bool downloadFinished = false;
        string downloadError = null;
        ResourceManager.EnsureInstance().CheckAndDownloadDependencies(
            ParseLabels(labels),
            status =>
            {
                if (status != null)
                {
                    Debug.Log($"[LuaHotUpdate] {status.StatusMessage} {status.Progress:P0}");
                    UpdateDownloadStatus(status);
                }
            },
            () =>
            {
                downloadFinished = true;
            },
            message =>
            {
                downloadError = message;
                downloadFinished = true;
            });

        while (!downloadFinished)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(downloadError))
        {
            Debug.LogWarning($"[LuaHotUpdate] Lua resource download failed, fallback to local Lua. Error: {downloadError}");
            IsUpdating = false;
            onError?.Invoke(downloadError);
            yield break;
        }

        SetUpdateStatus("Loading Lua manifest...");
        string manifestJson = null;
        yield return LoadTextAssetContentCoroutine(manifestAddress, content => manifestJson = content);
        if (string.IsNullOrEmpty(manifestJson))
        {
            Debug.Log("[LuaHotUpdate] LuaManifest not found, skip Lua file installation.");
            SetUpdateStatus("No Lua update manifest found.");
            IsUpdating = false;
            onCompleted?.Invoke(true);
            yield break;
        }

        LuaHotUpdateManifest manifest = null;
        try
        {
            manifest = JsonUtility.FromJson<LuaHotUpdateManifest>(manifestJson);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[LuaHotUpdate] Failed to parse LuaManifest, fallback to local Lua. Error: {exception.Message}");
            IsUpdating = false;
            onError?.Invoke($"Failed to parse Lua manifest: {exception.Message}");
            yield break;
        }

        if (manifest == null || manifest.files == null || manifest.files.Count == 0)
        {
            Debug.Log("[LuaHotUpdate] LuaManifest has no files to install.");
            SetUpdateStatus("Lua manifest has no files.");
            IsUpdating = false;
            onCompleted?.Invoke(true);
            yield break;
        }

        string manifestVersion = string.IsNullOrWhiteSpace(manifest.version) ? "unknown" : manifest.version.Trim();
        Debug.Log($"[LuaHotUpdate] Lua manifest version: {manifestVersion}");
        SetUpdateStatus($"Installing Lua files... Version: {manifestVersion}");

        int successCount = 0;
        for (int i = 0; i < manifest.files.Count; i++)
        {
            LuaHotUpdateFile file = manifest.files[i];
            if (file == null || string.IsNullOrWhiteSpace(file.address) || string.IsNullOrWhiteSpace(file.path))
            {
                continue;
            }

            SetUpdateStatus($"Installing Lua file {i + 1}/{manifest.files.Count}...");
            UpdateInstallProgress(i, manifest.files.Count);

            byte[] luaBytes = null;
            yield return LoadTextAssetBytesCoroutine(file.address, bytes => luaBytes = bytes);
            if (luaBytes == null || luaBytes.Length == 0)
            {
                Debug.LogWarning($"[LuaHotUpdate] Failed to load Lua file: {file.address}");
                continue;
            }

            string relativePath = NormalizeLuaRelativePath(file.path);
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogWarning($"[LuaHotUpdate] Invalid Lua file path: {file.path}");
                continue;
            }

            string fullPath = Path.Combine(LuaConst.luaResDir, relativePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, luaBytes);
            successCount++;
            UpdateInstallProgress(i + 1, manifest.files.Count);
            Debug.Log($"[LuaHotUpdate] Lua file installed: {relativePath}");
        }

        Debug.Log($"[LuaHotUpdate] Lua update completed. Version={manifestVersion}, Installed={successCount}/{manifest.files.Count}, Directory={LuaConst.luaResDir}");
        if (updateView != null)
        {
            updateView.Complete(manifestVersion);
        }
        IsUpdating = false;
        onCompleted?.Invoke(true);
    }

    private IEnumerator LoadTextAssetContentCoroutine(string address, Action<string> callback)
    {
        string content = null;
        yield return LoadTextAssetCoroutine(address, asset =>
        {
            if (asset != null)
            {
                content = asset.text;
            }
        });

        callback?.Invoke(content);
    }

    private IEnumerator LoadTextAssetBytesCoroutine(string address, Action<byte[]> callback)
    {
        byte[] bytes = null;
        yield return LoadTextAssetCoroutine(address, asset =>
        {
            if (asset != null)
            {
                bytes = asset.bytes;
            }
        });

        callback?.Invoke(bytes);
    }

    private IEnumerator LoadTextAssetCoroutine(string address, Action<TextAsset> callback)
    {
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);
        yield return handle;

        TextAsset asset = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
        callback?.Invoke(asset);

        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }

    private IEnumerator ShowUpdateUICoroutine()
    {
        if (updateView != null)
        {
            updateView.Initialize(null);
            yield break;
        }

        GameObject prefab = null;
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(LuaUpdateUIPrefabPath);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            prefab = handle.Result;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[LuaHotUpdate] Failed to load Lua update UI prefab: {LuaUpdateUIPrefabPath}");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            yield break;
        }

        Transform uiParent = EnsureUpdateCanvas();
        updateViewObject = Instantiate(prefab, uiParent, false);
        updateViewObject.name = prefab.name;
        StretchToParent(updateViewObject.transform as RectTransform);
        updateView = updateViewObject.GetComponent<LuaUpdateUIView>();
        if (updateView == null)
        {
            updateView = updateViewObject.AddComponent<LuaUpdateUIView>();
        }

        updateView.Initialize(null);
        Addressables.Release(handle);
    }

    private void HideUpdateUI()
    {
        if (updateViewObject != null)
        {
            Destroy(updateViewObject);
        }

        if (updateCanvasObject != null)
        {
            Destroy(updateCanvasObject);
        }

        if (updateEventSystemObject != null)
        {
            Destroy(updateEventSystemObject);
        }

        updateViewObject = null;
        updateView = null;
        updateCanvasObject = null;
        updateEventSystemObject = null;
    }

    private Transform EnsureUpdateCanvas()
    {
        if (updateCanvasObject != null)
        {
            return updateCanvasObject.transform;
        }

        updateCanvasObject = new GameObject("LuaUpdateUICanvas");
        DontDestroyOnLoad(updateCanvasObject);
        Canvas canvas = updateCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = updateCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        updateCanvasObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            updateEventSystemObject = new GameObject("LuaUpdateUIEventSystem");
            DontDestroyOnLoad(updateEventSystemObject);
            updateEventSystemObject.AddComponent<EventSystem>();
            updateEventSystemObject.AddComponent<StandaloneInputModule>();
        }

        return updateCanvasObject.transform;
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private void UpdateDownloadStatus(ResourceDownloadStatus status)
    {
        if (updateView == null || status == null)
        {
            return;
        }

        string message = string.IsNullOrEmpty(status.StatusMessage) ? "Checking Lua update..." : status.StatusMessage;
        updateView.SetStatus(message);
        updateView.SetProgress(status.Progress, status.DownloadedBytes, status.TotalBytes, status.DownloadBytesPerSecond);
    }

    private void UpdateInstallProgress(int installedCount, int totalCount)
    {
        if (updateView == null || totalCount <= 0)
        {
            return;
        }

        float progress = Mathf.Clamp01((float)installedCount / totalCount);
        updateView.SetProgress(progress, installedCount, totalCount, 0f);
    }

    private void SetUpdateStatus(string message)
    {
        if (updateView != null)
        {
            updateView.SetStatus(message);
        }
    }

    private IList<string> ParseLabels(string labels)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(labels))
        {
            return result;
        }

        string[] parts = labels.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string label = parts[i].Trim();
            if (!string.IsNullOrEmpty(label) && !result.Contains(label))
            {
                result.Add(label);
            }
        }

        return result;
    }

    private string NormalizeLuaRelativePath(string path)
    {
        string normalizedPath = path.Replace('\\', '/').Trim();
        while (normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        if (normalizedPath.Contains(".."))
        {
            return null;
        }

        if (!normalizedPath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath += ".lua";
        }

        return normalizedPath.Replace('/', Path.DirectorySeparatorChar);
    }

    [Serializable]
    private class LuaHotUpdateManifest
    {
        public string version;
        public List<LuaHotUpdateFile> files;
    }

    [Serializable]
    private class LuaHotUpdateFile
    {
        public string address;
        public string path;
    }
}