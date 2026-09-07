using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
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
    private const string InstalledManifestFileName = "LuaInstalledManifest.json";
    private const float CloseUIDelay = 0.35f;

    public static LuaHotUpdateManager Instance { get; private set; }

    public bool IsUpdating { get; private set; }

    private LuaUpdateUIView updateView;
    private GameObject updateViewObject;
    private GameObject updateCanvasObject;
    private GameObject updateEventSystemObject;
    private bool isUsingFallbackView;
    private string currentStatusMessage = "Preparing Lua update...";
    private float currentProgress;
    private long currentDownloadedBytes;
    private long currentTotalBytes;
    private float currentDownloadBytesPerSecond;
    private bool isRetryVisible;

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
            ApplyTrackedViewState(updateView);
            SetUpdateStatus(string.IsNullOrEmpty(updateError) ? "Lua update failed." : updateError);
            ShowRetry();

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
            DeleteInstalledManifestFile();
            IsUpdating = false;
            onCompleted?.Invoke(true);
            yield break;
        }

        string manifestVersion = string.IsNullOrWhiteSpace(manifest.version) ? "unknown" : manifest.version.Trim();
        LuaHotUpdateManifest installedManifest = LoadInstalledManifest();
        bool shouldInstall = ShouldInstallLuaFiles(manifest, installedManifest, manifestVersion);

        Debug.Log($"[LuaHotUpdate] Lua manifest version: {manifestVersion}, ShouldInstall={shouldInstall}");

        if (!shouldInstall)
        {
            SetUpdateStatus($"Lua is up to date. Version: {manifestVersion}");
            UpdateInstallProgress(manifest.files.Count, manifest.files.Count);
            Debug.Log($"[LuaHotUpdate] Lua files are already installed locally. Version={manifestVersion}, Directory={LuaConst.luaResDir}");
            IsUpdating = false;
            onCompleted?.Invoke(true);
            yield break;
        }

        DeleteStaleLuaFiles(installedManifest, manifest);

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

        if (successCount == manifest.files.Count)
        {
            SaveInstalledManifest(manifest);
        }
        else
        {
            DeleteInstalledManifestFile();
            Debug.LogWarning($"[LuaHotUpdate] Lua install incomplete. Installed={successCount}/{manifest.files.Count}. The local installed-manifest snapshot was cleared so the next startup will retry installation.");
        }

        Debug.Log($"[LuaHotUpdate] Lua update completed. Version={manifestVersion}, Installed={successCount}/{manifest.files.Count}, Directory={LuaConst.luaResDir}");
        if (updateView != null)
        {
            updateView.Complete(manifestVersion);
            CacheViewState($"Lua update completed. Version: {manifestVersion}", 1f, Math.Max(currentDownloadedBytes, currentTotalBytes), Math.Max(currentDownloadedBytes, currentTotalBytes), 0f, false);
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
            ApplyTrackedViewState(updateView);
            yield break;
        }

        CreateFallbackUpdateView();
        if (updateView != null)
        {
            updateView.Initialize(null);
            ApplyTrackedViewState(updateView);
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
            Debug.LogWarning($"[LuaHotUpdate] Failed to load Lua update UI prefab: {LuaUpdateUIPrefabPath}. Continue using fallback UI.");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            yield break;
        }

        if (isUsingFallbackView)
        {
            ReplaceFallbackWithPrefab(prefab);
        }
        else if (updateView == null)
        {
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
            ApplyTrackedViewState(updateView);
        }

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
        isUsingFallbackView = false;
        currentStatusMessage = "Preparing Lua update...";
        currentProgress = 0f;
        currentDownloadedBytes = 0L;
        currentTotalBytes = 0L;
        currentDownloadBytesPerSecond = 0f;
        isRetryVisible = false;
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
        //rectTransform.localScale = Vector3.one;
    }

    private void UpdateDownloadStatus(ResourceDownloadStatus status)
    {
        if (status == null)
        {
            return;
        }

        string message = string.IsNullOrEmpty(status.StatusMessage) ? "Checking Lua update..." : status.StatusMessage;
        CacheViewState(message, status.Progress, status.DownloadedBytes, status.TotalBytes, status.DownloadBytesPerSecond, false);
        if (updateView == null)
        {
            return;
        }

        updateView.SetStatus(message);
        updateView.SetProgress(status.Progress, status.DownloadedBytes, status.TotalBytes, status.DownloadBytesPerSecond);
    }

    private void UpdateInstallProgress(int installedCount, int totalCount)
    {
        if (totalCount <= 0)
        {
            return;
        }

        float progress = Mathf.Clamp01((float)installedCount / totalCount);
        CacheViewState(currentStatusMessage, progress, installedCount, totalCount, 0f, false);
        if (updateView == null)
        {
            return;
        }

        updateView.SetProgress(progress, installedCount, totalCount, 0f);
    }

    private void SetUpdateStatus(string message)
    {
        CacheViewState(string.IsNullOrEmpty(message) ? string.Empty : message, currentProgress, currentDownloadedBytes, currentTotalBytes, currentDownloadBytesPerSecond, isRetryVisible);
        if (updateView != null)
        {
            updateView.SetStatus(message);
        }
    }

    private void ShowRetry()
    {
        isRetryVisible = true;
        if (updateView != null)
        {
            updateView.ShowRetry();
        }
    }

    private void HideRetry()
    {
        isRetryVisible = false;
        if (updateView != null)
        {
            updateView.HideRetry();
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

    private void CreateFallbackUpdateView()
    {
        if (updateView != null)
        {
            return;
        }

        Transform parent = EnsureUpdateCanvas();
        GameObject root = new GameObject("LuaUpdateFallbackUI", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchToParent(rootRect);
        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);
        background.raycastTarget = true;

        CreateText(root.transform, "statusTip", new Vector2(0.5f, 0.58f), new Vector2(640f, 80f), 34, TextAlignmentOptions.Center, "Preparing Lua update...");

        GameObject progressBar = new GameObject("progressBar", typeof(RectTransform), typeof(Image));
        progressBar.transform.SetParent(root.transform, false);
        RectTransform progressBarRect = progressBar.GetComponent<RectTransform>();
        progressBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressBarRect.pivot = new Vector2(0.5f, 0.5f);
        progressBarRect.sizeDelta = new Vector2(700f, 20f);
        progressBarRect.anchoredPosition = new Vector2(0f, -10f);
        Image progressBarBackground = progressBar.GetComponent<Image>();
        progressBarBackground.color = new Color(1f, 1f, 1f, 0.18f);
        progressBarBackground.raycastTarget = false;

        GameObject progress = new GameObject("progress", typeof(RectTransform), typeof(Image));
        progress.transform.SetParent(progressBar.transform, false);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0f, 0f);
        progressRect.anchorMax = new Vector2(1f, 1f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;
        Image progressImage = progress.GetComponent<Image>();
        progressImage.color = Color.white;
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Horizontal;
        progressImage.fillOrigin = 0;
        progressImage.fillAmount = 0f;
        progressImage.raycastTarget = false;

        CreateText(root.transform, "progressPercentText", new Vector2(0.73f, 0.5f), new Vector2(160f, 50f), 28, TextAlignmentOptions.Left, "0.00%");
        CreateText(root.transform, "downloadSizeText", new Vector2(0.5f, 0.44f), new Vector2(320f, 50f), 28, TextAlignmentOptions.Center, "0 B / 0 B");
        CreateText(root.transform, "downloadSpeedText", new Vector2(0.65f, 0.5f), new Vector2(160f, 50f), 28, TextAlignmentOptions.Left, "0 B/s");

        GameObject retryButton = CreateButton(root.transform, "retryBtn", new Vector2(0.5f, 0.37f), new Vector2(220f, 72f), "Retry");
        retryButton.SetActive(false);

        updateViewObject = root;
        updateView = root.AddComponent<LuaUpdateUIView>();
        isUsingFallbackView = true;
    }

    private void ReplaceFallbackWithPrefab(GameObject prefab)
    {
        Transform uiParent = EnsureUpdateCanvas();
        GameObject previousViewObject = updateViewObject;
        LuaUpdateUIView previousView = updateView;

        GameObject prefabInstance = Instantiate(prefab, uiParent, false);
        prefabInstance.name = prefab.name;
        StretchToParent(prefabInstance.transform as RectTransform);

        LuaUpdateUIView prefabView = prefabInstance.GetComponent<LuaUpdateUIView>();
        if (prefabView == null)
        {
            prefabView = prefabInstance.AddComponent<LuaUpdateUIView>();
        }

        updateViewObject = prefabInstance;
        updateView = prefabView;
        isUsingFallbackView = false;
        updateView.Initialize(null);
        ApplyTrackedViewState(updateView);

        if (previousView != null && previousView != updateView)
        {
            Destroy(previousView.gameObject);
        }
        else if (previousViewObject != null && previousViewObject != updateViewObject)
        {
            Destroy(previousViewObject);
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchor, Vector2 size, float fontSize, TextAlignmentOptions alignment, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private GameObject CreateButton(Transform parent, string name, Vector2 anchor, Vector2 size, string text)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.16f);

        GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToParent(labelRect);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return buttonObject;
    }

    private void CacheViewState(string statusMessage, float progress, long downloadedBytes, long totalBytes, float downloadBytesPerSecond, bool retryVisible)
    {
        currentStatusMessage = statusMessage ?? string.Empty;
        currentProgress = Mathf.Clamp01(progress);
        currentDownloadedBytes = Math.Max(0L, downloadedBytes);
        currentTotalBytes = Math.Max(0L, totalBytes);
        currentDownloadBytesPerSecond = Mathf.Max(0f, downloadBytesPerSecond);
        isRetryVisible = retryVisible;
    }

    private void ApplyTrackedViewState(LuaUpdateUIView view)
    {
        if (view == null)
        {
            return;
        }

        view.SetStatus(currentStatusMessage);
        view.SetProgress(currentProgress, currentDownloadedBytes, currentTotalBytes, currentDownloadBytesPerSecond);
        if (isRetryVisible)
        {
            view.ShowRetry();
        }
        else
        {
            view.HideRetry();
        }
    }

    private bool ShouldInstallLuaFiles(LuaHotUpdateManifest manifest, LuaHotUpdateManifest installedManifest, string manifestVersion)
    {
        if (manifest == null || manifest.files == null || manifest.files.Count == 0)
        {
            return false;
        }

        if (installedManifest == null)
        {
            return true;
        }

        string installedVersion = string.IsNullOrWhiteSpace(installedManifest.version) ? string.Empty : installedManifest.version.Trim();
        if (!string.Equals(installedVersion, manifestVersion, StringComparison.Ordinal))
        {
            return true;
        }

        if (HasManifestStructureChanged(installedManifest, manifest))
        {
            return true;
        }

        return !AreLuaFilesInstalledLocally(manifest);
    }

    private bool HasManifestStructureChanged(LuaHotUpdateManifest installedManifest, LuaHotUpdateManifest currentManifest)
    {
        HashSet<string> installedEntries = BuildManifestEntrySet(installedManifest);
        HashSet<string> currentEntries = BuildManifestEntrySet(currentManifest);
        return !installedEntries.SetEquals(currentEntries);
    }

    private HashSet<string> BuildManifestEntrySet(LuaHotUpdateManifest manifest)
    {
        HashSet<string> entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (manifest == null || manifest.files == null)
        {
            return entries;
        }

        for (int i = 0; i < manifest.files.Count; i++)
        {
            LuaHotUpdateFile file = manifest.files[i];
            if (file == null)
            {
                continue;
            }

            string normalizedPath = NormalizeLuaRelativePath(file.path);
            string normalizedAddress = string.IsNullOrWhiteSpace(file.address) ? string.Empty : file.address.Trim();
            if (string.IsNullOrEmpty(normalizedPath) || string.IsNullOrEmpty(normalizedAddress))
            {
                continue;
            }

            entries.Add($"{normalizedPath}|{normalizedAddress}");
        }

        return entries;
    }

    private bool AreLuaFilesInstalledLocally(LuaHotUpdateManifest manifest)
    {
        if (manifest == null || manifest.files == null)
        {
            return false;
        }

        for (int i = 0; i < manifest.files.Count; i++)
        {
            LuaHotUpdateFile file = manifest.files[i];
            if (file == null)
            {
                return false;
            }

            string relativePath = NormalizeLuaRelativePath(file.path);
            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            string fullPath = Path.Combine(LuaConst.luaResDir, relativePath);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length <= 0L)
            {
                return false;
            }
        }

        return true;
    }

    private void DeleteStaleLuaFiles(LuaHotUpdateManifest installedManifest, LuaHotUpdateManifest currentManifest)
    {
        if (installedManifest == null || installedManifest.files == null)
        {
            return;
        }

        HashSet<string> currentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < currentManifest.files.Count; i++)
        {
            string currentPath = NormalizeLuaRelativePath(currentManifest.files[i]?.path);
            if (!string.IsNullOrEmpty(currentPath))
            {
                currentPaths.Add(currentPath);
            }
        }

        for (int i = 0; i < installedManifest.files.Count; i++)
        {
            string stalePath = NormalizeLuaRelativePath(installedManifest.files[i]?.path);
            if (string.IsNullOrEmpty(stalePath) || currentPaths.Contains(stalePath))
            {
                continue;
            }

            string fullPath = Path.Combine(LuaConst.luaResDir, stalePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"[LuaHotUpdate] Deleted stale local Lua file: {stalePath}");
            }
        }
    }

    private LuaHotUpdateManifest LoadInstalledManifest()
    {
        string installedManifestPath = GetInstalledManifestPath();
        if (!File.Exists(installedManifestPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(installedManifestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<LuaHotUpdateManifest>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[LuaHotUpdate] Failed to load installed Lua manifest snapshot, will reinstall. Error: {exception.Message}");
            return null;
        }
    }

    private void SaveInstalledManifest(LuaHotUpdateManifest manifest)
    {
        if (manifest == null)
        {
            return;
        }

        string installedManifestPath = GetInstalledManifestPath();
        string directory = Path.GetDirectoryName(installedManifestPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(installedManifestPath, JsonUtility.ToJson(manifest, true));
    }

    private void DeleteInstalledManifestFile()
    {
        string installedManifestPath = GetInstalledManifestPath();
        if (File.Exists(installedManifestPath))
        {
            File.Delete(installedManifestPath);
        }
    }

    private string GetInstalledManifestPath()
    {
        return Path.Combine(LuaConst.luaResDir, InstalledManifestFileName);
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