using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private Sprite cursorSprite; //光标图片

    [SerializeField] private GameObject initailUI; //初始UI
    [SerializeField] private GameObject currentUI; //当前显示UI
    [SerializeField] private GameObject lastUI;  //上一个显示UI

    public GameObject pauseUI; //暂停UI

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentUI = initailUI;
        currentUI.SetActive(true);

        //设置自定义光标
        cursorSprite = ResourceManager.Instance.LoadSprite("UI/Pointers/Cursor");
        //光标的热点设置在图片左上角
        Vector2 hotspot = new Vector2(0, 0);
        Cursor.SetCursor(cursorSprite.texture, hotspot, CursorMode.Auto);
    }

    public void EnterNextScene(float delay = 0f)
    {
        StartCoroutine(EnterNextSceneCoroutine(delay));
    }

    IEnumerator EnterNextSceneCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ExitGame()
    {
        Application.Quit();
        //让编辑器也能退出游戏
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SwitchUI(GameObject nextUI)
    {
        StartCoroutine(SwitchUICoroutine(nextUI));
    }

    IEnumerator SwitchUICoroutine(GameObject nextUI)
    {
        //UI切换在当前帧结束后进行
        yield return null; 

        if (nextUI == currentUI)
        {
            yield break;
        }
        if (currentUI != null)
        {
            currentUI.SetActive(false);
            lastUI = currentUI;
        }

        currentUI = nextUI;
        currentUI.SetActive(true);
    }

    public void BackToLastUI()
    {
        // if (lastUI != null)
        // {
        //     currentUI.SetActive(false);
        //     GameObject temp = currentUI;
        //     currentUI = lastUI;
        //     lastUI = temp;
        //     currentUI.SetActive(true);
        // }

        SwitchUI(lastUI);
    }

    public void TogglePauseUI()
    {
        if (pauseUI.activeSelf)
        {
            pauseUI.SetActive(false);
            Time.timeScale = 1f; //恢复游戏时间
            lastUI = pauseUI;

            HideCursor();
        }
        else
        {
            pauseUI.SetActive(true);
            Time.timeScale = 0f; //暂停游戏时间
            currentUI = pauseUI;

            ShowCursor();
        }
    }

    public void ResumeGame()
    {
        GameManager.instance.ResumeGame();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; //确保时间恢复正常
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); //主菜单是第0个场景
    }

    public void HideCursor()
    {
        //隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowCursor()
    {
        //显示光标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
