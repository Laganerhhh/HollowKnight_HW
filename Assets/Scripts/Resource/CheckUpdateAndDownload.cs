using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;   
using UnityEngine.AddressableAssets;

/// <summary>
/// 检查资源更新和下载
/// </summary>
public class CheckUpdateAndDownload : MonoBehaviour
{
    /// <summary>
    /// 显示下载进度
    /// </summary>
    public Text updateText;

    /// <summary>
    /// 重试按钮
    /// </summary>
    public Button retryButton;

    // Start is called before the first frame update
    void Start()
    {
        retryButton.onClick.AddListener(
            () =>
            {
                StartCoroutine(DoUpdateAddressable());
            }
        );
        DoUpdateAddressable();
    }

    IEnumerator DoUpdateAddressable()
    {
        AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync();
        yield return initHandle;

         //检查更新
        var checkHandle = Addressables.CheckForCatalogUpdates();
        yield return checkHandle;

        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            OnError("检查更新失败" + checkHandle.OperationException.Message);
        }

        if (checkHandle.Result.Count > 0)
        {
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, true);
            yield return updateHandle;  

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                OnError("更新失败" + updateHandle.OperationException.Message);
                yield break;
            }

            List<IResourceLocator> locators = updateHandle.Result;
            foreach (var locator in locators)
            {
                List<object> keys = new List<object>();
                keys.AddRange(locator.Keys);
                //获取下载大小
                var sizeHandle = Addressables.GetDownloadSizeAsync(keys.GetEnumerator());
                yield return sizeHandle;

                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    OnError("获取下载大小失败" + sizeHandle.OperationException.Message);
                    yield break;
                }

                long totalSize = sizeHandle.Result;
                updateText.text = updateText.text + "下载大小：" + totalSize;
                Debug.Log("下载大小：" + totalSize);
                if (totalSize > 0)
                {
                    var downloadHandle = Addressables.DownloadDependenciesAsync(keys, true);
                    while (!downloadHandle.IsDone)
                    {
                        if (downloadHandle.Status == AsyncOperationStatus.Failed)
                        {
                            OnError("下载失败" + downloadHandle.OperationException.Message);
                            yield break;
                        }
                        //更新下载进度
                        updateText.text = updateText.text + "下载进度：" + downloadHandle.PercentComplete;
                        Debug.Log("下载进度：" + downloadHandle.PercentComplete);
                        yield return null;
                    }
                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        updateText.text = updateText.text + "下载成功";
                        Debug.Log("下载成功");
                    }
                }
            }
        }
        else
        {
            updateText.text = "未检查到更新";
        }
    }

    private void OnError(string msg)
    {
        updateText.text = "更新失败";
        retryButton.gameObject.SetActive(true);
    }
}
