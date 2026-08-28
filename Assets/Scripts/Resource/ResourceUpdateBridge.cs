using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源更新桥接器：向 Lua 层提供更友好的下载入口，底层仍复用 ResourceManager/BundleManager。
/// </summary>
public class ResourceUpdateBridge : MonoBehaviour
{
    public static ResourceUpdateBridge Instance { get; private set; }

    public bool IsUpdating { get; private set; }

    public static ResourceUpdateBridge EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject bridgeObject = new GameObject("ResourceUpdateBridge");
        DontDestroyOnLoad(bridgeObject);
        return bridgeObject.AddComponent<ResourceUpdateBridge>();
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

    public bool IsRemoteUpdateInProgress()
    {
        return IsUpdating || ResourceManager.EnsureInstance().IsRemoteUpdateInProgress();
    }

    public void StartUpdateByLabelString(string labels, Action<ResourceDownloadStatus> onProgress, Action onCompleted, Action<string> onError)
    {
        if (IsRemoteUpdateInProgress())
        {
            onError?.Invoke("Resource update is already in progress.");
            return;
        }

        IsUpdating = true;
        IList<string> labelList = ParseLabels(labels);

        ResourceManager.EnsureInstance().CheckAndDownloadDependencies(
            labelList,
            status =>
            {
                onProgress?.Invoke(status);
            },
            () =>
            {
                IsUpdating = false;
                onCompleted?.Invoke();
            },
            message =>
            {
                IsUpdating = false;
                onError?.Invoke(message);
            });
    }

    private IList<string> ParseLabels(string labels)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(labels))
        {
            return result;
        }

        string[] parts = labels.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string label = part.Trim();
            if (!string.IsNullOrEmpty(label) && !result.Contains(label))
            {
                result.Add(label);
            }
        }

        return result;
    }
}