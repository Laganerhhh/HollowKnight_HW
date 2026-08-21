using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 下载界面控制器：负责发起更新、接收进度并刷新界面。
/// </summary>
public class CheckUpdateAndDownload : MonoBehaviour
{
    [Header("下载界面")]
    [SerializeField] private Image progressImage;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private TextMeshProUGUI downloadSizeText;
    [SerializeField] private TextMeshProUGUI downloadSpeedText;
    [SerializeField] private TextMeshProUGUI statusTip;
    [SerializeField] private Button retryBtn;
    [SerializeField] private float progressSmoothSpeed = 2.5f;
    [Header("下载目标")]
    [SerializeField] private List<string> downloadLabels = new List<string>();

    [Header("完成后的界面切换")]
    [SerializeField] private GameObject successUI;
    [SerializeField] private float switchDelay = 0.5f;
    [SerializeField] private string defaultSuccessUIName = "StartUI";

    private bool isUpdating;
    private bool switchAfterSuccess;
    private float targetProgress;
    private float displayedProgress;
    private long currentBytes;
    private long totalBytes;
    private float currentDownloadBytesPerSecond;
    private void Start()
    {
        if (retryBtn != null)
        {
            retryBtn.onClick.RemoveListener(OnRetryButtonClicked);
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
            retryBtn.gameObject.SetActive(false);
        }
        //test
        //Caching.ClearCache();

        ResetProgressView();
        StartUpdateFlow();
    }

    private void OnDestroy()
    {
        if (retryBtn != null)
        {
            retryBtn.onClick.RemoveListener(OnRetryButtonClicked);
        }
    }

    private void Update()
    {
        UpdateProgressVisual();

        if (!switchAfterSuccess)
        {
            return;
        }

        switchAfterSuccess = false;
        GameObject nextUI = ResolveSuccessUI();
        if (nextUI != null && UIManager.Instance != null)
        {
            UIManager.Instance.SwitchUI(nextUI);
        }
    }

    private void OnRetryButtonClicked()
    {
        if (isUpdating || ResourceManager.EnsureInstance().IsRemoteUpdateInProgress())
        {
            return;
        }

        StartUpdateFlow();
    }

    private void StartUpdateFlow()
    {
        isUpdating = true;
        switchAfterSuccess = false;

        if (retryBtn != null)
        {
            retryBtn.gameObject.SetActive(false);
        }

        UpdateStatus("Preparing to check resources...");
        targetProgress = 0f;
        displayedProgress = 0f;
        currentBytes = 0L;
        totalBytes = 0L;
        currentDownloadBytesPerSecond = 0f;
        ApplyProgressVisual(0f, 0L, 0L, 0f);

        ResourceManager.EnsureInstance().CheckAndDownloadDependencies(downloadLabels, OnDownloadProgress, OnDownloadCompleted, OnDownloadError);
    }

    private void OnDownloadProgress(ResourceDownloadStatus status)
    {
        if (status == null)
        {
            return;
        }

        UpdateStatus(status.StatusMessage);
        UpdateProgress(status.Progress, status.DownloadedBytes, status.TotalBytes, status.DownloadBytesPerSecond);
    }

    private void OnDownloadCompleted()
    {
        isUpdating = false;
        currentDownloadBytesPerSecond = 0f;
        ApplyProgressVisual(displayedProgress, currentBytes, totalBytes, currentDownloadBytesPerSecond);
        UpdateStatus("Resource download completed.");

        if (switchDelay > 0f)
        {
            Invoke(nameof(SwitchToSuccessUI), switchDelay);
            return;
        }

        SwitchToSuccessUI();
    }

    private void SwitchToSuccessUI()
    {
        switchAfterSuccess = true;
    }

    private GameObject ResolveSuccessUI()
    {
        if (successUI != null)
        {
            return successUI;
        }

        if (string.IsNullOrWhiteSpace(defaultSuccessUIName))
        {
            return null;
        }

        GameObject foundUI = GameObject.Find(defaultSuccessUIName);
        if (foundUI != null)
        {
            successUI = foundUI;
        }

        return successUI;
    }

    private void OnDownloadError(string message)
    {
        isUpdating = false;
        currentDownloadBytesPerSecond = 0f;
        ApplyProgressVisual(displayedProgress, currentBytes, totalBytes, currentDownloadBytesPerSecond);
        UpdateStatus(string.IsNullOrEmpty(message) ? "Resource update failed." : message);

        if (retryBtn != null)
        {
            retryBtn.gameObject.SetActive(true);
        }

        Debug.LogError(message);
    }

    private void ResetProgressView()
    {
        UpdateStatus("Preparing to check resources...");
        targetProgress = 0f;
        displayedProgress = 0f;
        currentBytes = 0L;
        totalBytes = 0L;
        currentDownloadBytesPerSecond = 0f;
        ApplyProgressVisual(0f, 0L, 0L, 0f);
    }

    private void UpdateStatus(string message)
    {
        if (statusTip != null)
        {
            statusTip.text = message;
        }
    }

    private void UpdateProgress(float progress, long currentBytes, long totalBytes, float downloadBytesPerSecond)
    {
        targetProgress = Mathf.Clamp01(progress);
        this.currentBytes = Math.Max(0L, currentBytes);
        this.totalBytes = Math.Max(0L, totalBytes);
        currentDownloadBytesPerSecond = Mathf.Max(0f, downloadBytesPerSecond);

        if (targetProgress < displayedProgress)
        {
            displayedProgress = targetProgress;
        }

        if (targetProgress <= 0f && this.currentBytes <= 0L && this.totalBytes <= 0L)
        {
            displayedProgress = 0f;
        }

        ApplyProgressVisual(displayedProgress, this.currentBytes, this.totalBytes, currentDownloadBytesPerSecond);
    }

    private void UpdateProgressVisual()
    {
        if (Mathf.Approximately(displayedProgress, targetProgress))
        {
            return;
        }

        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, progressSmoothSpeed * Time.unscaledDeltaTime);
        ApplyProgressVisual(displayedProgress, currentBytes, totalBytes, currentDownloadBytesPerSecond);
    }

    private void ApplyProgressVisual(float progress, long currentBytes, long totalBytes, float downloadBytesPerSecond)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (progressImage != null)
        {
            progressImage.fillAmount = clampedProgress;
        }

        if (progressPercentText != null)
        {
            progressPercentText.text = $"{clampedProgress * 100f:0.00}%";
        }

        if (downloadSizeText != null)
        {
            downloadSizeText.text = $"{FormatSize(currentBytes)} / {FormatSize(totalBytes)}";
        }

        if (downloadSpeedText != null)
        {
            downloadSpeedText.text = $"{FormatSize((long)downloadBytesPerSecond)}/s";
        }
    }

    private string FormatSize(long bytes)
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
}
