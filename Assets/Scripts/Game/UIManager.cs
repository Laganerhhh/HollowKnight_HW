using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LuaInterface;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI分层根节点")]
    [SerializeField] private Transform normalRoot;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Transform topRoot;
    [SerializeField] private Transform toastRoot;

    [Header("鼠标")]
    [SerializeField] private Sprite cursorSprite; //光标图片

    [Header("旧UI兼容配置")]
    [SerializeField] private GameObject initailUI; //初始UI
    [SerializeField] private GameObject currentUI; //当前显示UI
    [SerializeField] private GameObject lastUI;  //上一个显示UI

    public GameObject pauseUI; //暂停UI

    private const string PausePanelName = "PausePanel";

    public Transform NormalRoot => normalRoot != null ? normalRoot : transform;
    public Transform PopupRoot => popupRoot != null ? popupRoot : NormalRoot;
    public Transform TopRoot => topRoot != null ? topRoot : PopupRoot;
    public Transform ToastRoot => toastRoot != null ? toastRoot : TopRoot;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;
        EnsureLayerRoots();
    }

    private void Start()
    {
        if (initailUI != null)
        {
            currentUI = initailUI;
            currentUI.SetActive(true);
        }

        SetupCursor();
    }

    private void EnsureLayerRoots()
    {
        normalRoot = normalRoot != null ? normalRoot : FindOrCreateLayerRoot("NormalRoot");
        popupRoot = popupRoot != null ? popupRoot : FindOrCreateLayerRoot("PopupRoot");
        topRoot = topRoot != null ? topRoot : FindOrCreateLayerRoot("TopRoot");
        toastRoot = toastRoot != null ? toastRoot : FindOrCreateLayerRoot("ToastRoot");
    }

    private Transform FindOrCreateLayerRoot(string rootName)
    {
        Transform root = transform.Find(rootName);
        if (root != null)
        {
            return root;
        }

        GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
        RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private void SetupCursor()
    {
        cursorSprite = cursorSprite != null ? cursorSprite : Resources.Load<Sprite>("Sprites/UI/Pointers/Cursor");
        if (cursorSprite == null || cursorSprite.texture == null)
        {
            return;
        }

        //光标的热点设置在图片左上角
        Vector2 hotspot = Vector2.zero;
        Cursor.SetCursor(cursorSprite.texture, hotspot, CursorMode.Auto);
    }

    public Transform GetLayerRoot(string layerName)
    {
        switch (layerName)
        {
            case "Popup":
            case "PopupRoot":
                return PopupRoot;
            case "Top":
            case "TopRoot":
                return TopRoot;
            case "Toast":
            case "ToastRoot":
                return ToastRoot;
            case "Normal":
            case "NormalRoot":
            default:
                return NormalRoot;
        }
    }

    public void OpenPanel(string panelName)
    {
        CallLuaUI("UIPanelManager.Open", panelName);
    }

    public void ClosePanel(string panelName)
    {
        CallLuaUI("UIPanelManager.Close", panelName);
    }

    public void DestroyPanel(string panelName)
    {
        CallLuaUI("UIPanelManager.Destroy", panelName);
    }

    public void BackPanel()
    {
        CallLuaUI("UIPanelManager.Back");
    }

    private void CallLuaUI(string functionName)
    {
        LuaState luaState = GetLuaState();
        if (luaState == null)
        {
            Debug.LogWarning($"[UIManager] LuaState is not ready, skip call: {functionName}");
            return;
        }

        luaState.Call(functionName, false);
    }

    private void CallLuaUI(string functionName, string panelName)
    {
        if (string.IsNullOrEmpty(panelName))
        {
            Debug.LogWarning($"[UIManager] Invalid panel name for call: {functionName}");
            return;
        }

        LuaState luaState = GetLuaState();
        if (luaState == null)
        {
            Debug.LogWarning($"[UIManager] LuaState is not ready, skip call: {functionName}, panel: {panelName}");
            return;
        }

        luaState.Call(functionName, panelName, false);
    }

    private LuaState GetLuaState()
    {
        return LuaClient.Instance != null ? LuaClient.GetMainState() : null;
    }

    public void EnterNextScene(float delay = 0f)
    {
        StartCoroutine(EnterNextSceneCoroutine(delay));
    }

    private IEnumerator EnterNextSceneCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void LoadScene(int sceneIndex)
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(sceneIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
        //让编辑器也能退出游戏
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetGamePaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (paused)
        {
            ShowCursor();
        }
        else
        {
            HideCursor();
        }
    }

    public void OpenPausePanel()
    {
        if (IsPaused)
        {
            OpenPanel(PausePanelName);
            return;
        }

        SetGamePaused(true);

        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
            lastUI = pauseUI;
        }

        OpenPanel(PausePanelName);

        if (GameManager.instance != null)
        {
            GameManager.instance.SetIsPlaying(false);
        }
    }

    public void ClosePausePanel()
    {
        ClosePanel(PausePanelName);
        SetGamePaused(false);

        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
            lastUI = pauseUI;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.SetIsPlaying(true);
        }
    }

    public void PauseGame()
    {
        OpenPausePanel();
    }

    public void ResumeGame()
    {
        ClosePausePanel();
    }

    public void ReturnToMainMenu()
    {
        LoadScene(0); //主菜单是第0个场景
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

    [System.Obsolete("旧UI切换接口，仅用于兼容已有Inspector按钮事件；新UI请使用OpenPanel(string)。")]
    public void SwitchUI(GameObject nextUI)
    {
        StartCoroutine(SwitchUICoroutine(nextUI));
    }

    private IEnumerator SwitchUICoroutine(GameObject nextUI)
    {
        //UI切换在当前帧结束后进行
        yield return null;

        if (nextUI == null || nextUI == currentUI)
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

    [System.Obsolete("旧UI返回接口，仅用于兼容已有Inspector按钮事件；新UI请使用BackPanel()。")]
    public void BackToLastUI()
    {
        SwitchUI(lastUI);
    }

    [System.Obsolete("旧暂停UI接口，仅用于兼容已有Inspector按钮事件；新UI请使用OpenPanel/ClosePanel配合SetGamePaused。")]
    public void TogglePauseUI()
    {
        if (IsPaused)
        {
            ClosePausePanel();
            return;
        }

        OpenPausePanel();
    }
}
