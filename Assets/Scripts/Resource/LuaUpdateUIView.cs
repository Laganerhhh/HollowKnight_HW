using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Startup UI for Lua hot update progress before the Lua VM is initialized.
/// </summary>
public class LuaUpdateUIView : MonoBehaviour
{
    private const float DefaultSmoothSpeed = 2.5f;

    [SerializeField] private Image progressImage;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private TextMeshProUGUI downloadSizeText;
    [SerializeField] private TextMeshProUGUI downloadSpeedText;
    [SerializeField] private TextMeshProUGUI statusTip;
    [SerializeField] private Button retryButton;
    [SerializeField] private float progressSmoothSpeed = DefaultSmoothSpeed;

    private float targetProgress;
    private float displayedProgress;
    private long currentBytes;
    private long totalBytes;
    private float currentDownloadBytesPerSecond;
    private Action retryAction;

    private void Awake()
    {
        BindIfNeeded();
        HideRetry();
        ApplyProgressVisual(0f, 0L, 0L, 0f);
    }

    private void Update()
    {
        if (Mathf.Approximately(displayedProgress, targetProgress))
        {
            return;
        }

        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, progressSmoothSpeed * Time.unscaledDeltaTime);
        ApplyProgressVisual(displayedProgress, currentBytes, totalBytes, currentDownloadBytesPerSecond);
    }

    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }
    }

    public void Initialize(Action onRetry)
    {
        BindIfNeeded();
        retryAction = onRetry;

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryButtonClicked);
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        HideRetry();
        SetStatus("Preparing Lua update...");
        SetProgress(0f, 0L, 0L, 0f);
    }

    public void SetStatus(string message)
    {
        BindIfNeeded();
        if (statusTip != null)
        {
            statusTip.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }
    }

    public void SetVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return;
        }

        SetStatus($"Lua version: {version}");
    }

    public void SetProgress(float progress, long downloadedBytes, long totalBytes, float downloadBytesPerSecond)
    {
        BindIfNeeded();
        targetProgress = Mathf.Clamp01(progress);
        currentBytes = Math.Max(0L, downloadedBytes);
        this.totalBytes = Math.Max(0L, totalBytes);
        currentDownloadBytesPerSecond = Mathf.Max(0f, downloadBytesPerSecond);

        if (targetProgress < displayedProgress)
        {
            displayedProgress = targetProgress;
        }

        if (targetProgress <= 0f && currentBytes <= 0L && this.totalBytes <= 0L)
        {
            displayedProgress = 0f;
        }

        ApplyProgressVisual(displayedProgress, currentBytes, this.totalBytes, currentDownloadBytesPerSecond);
    }

    public void ShowRetry()
    {
        BindIfNeeded();
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
        }
    }

    public void HideRetry()
    {
        BindIfNeeded();
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
        }
    }

    public void Complete(string version)
    {
        HideRetry();
        SetProgress(1f, Math.Max(currentBytes, totalBytes), Math.Max(currentBytes, totalBytes), 0f);
        SetStatus(string.IsNullOrWhiteSpace(version) ? "Lua update completed." : $"Lua update completed. Version: {version}");
    }

    private void BindIfNeeded()
    {
        if (progressImage == null)
        {
            Transform progressTransform = transform.Find("progressBar/progress");
            if (progressTransform != null)
            {
                progressImage = progressTransform.GetComponent<Image>();
            }
        }

        if (progressPercentText == null)
        {
            progressPercentText = FindText("progressPercentText");
        }

        if (downloadSizeText == null)
        {
            downloadSizeText = FindText("downloadSizeText");
        }

        if (downloadSpeedText == null)
        {
            downloadSpeedText = FindText("downloadSpeedText");
        }

        if (statusTip == null)
        {
            statusTip = FindText("statusTip");
        }

        if (retryButton == null)
        {
            Transform retryTransform = transform.Find("retryBtn");
            if (retryTransform != null)
            {
                retryButton = retryTransform.GetComponent<Button>();
            }
        }
    }

    private TextMeshProUGUI FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private void ApplyProgressVisual(float progress, long downloadedBytes, long totalBytes, float downloadBytesPerSecond)
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
            downloadSizeText.text = $"{FormatSize(downloadedBytes)} / {FormatSize(totalBytes)}";
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

    private void OnRetryButtonClicked()
    {
        HideRetry();
        retryAction?.Invoke();
    }
}
