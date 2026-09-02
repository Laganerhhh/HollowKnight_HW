using UnityEngine;
using UnityEngine.SceneManagement;

public class GMTool : MonoBehaviour
{
    public static GMTool Instance { get; private set; }

    [Header("调试热键")]
    [SerializeField] private KeyCode toggleGodModeKey = KeyCode.F1;
    [SerializeField] private KeyCode nextLevelKey = KeyCode.F2;

    [Header("调试开关")]
    [SerializeField] private bool godModeEnabled = false;

    public static bool IsGodModeEnabled => Instance != null && Instance.godModeEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject gmObject = new GameObject("GMTool");
        gmObject.AddComponent<GMTool>();
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

    private void Update()
    {
        if (Input.GetKeyDown(toggleGodModeKey))
        {
            ToggleGodMode();
        }

        if (Input.GetKeyDown(nextLevelKey))
        {
            GoToNextLevel();
        }
    }

    public void ToggleGodMode()
    {
        godModeEnabled = !godModeEnabled;
        Debug.Log($"[GMTool] 无敌模式已{(godModeEnabled ? "开启" : "关闭")}");
    }

    public void GoToNextLevel()
    {
        if (GameManager.instance == null)
        {
            Debug.LogWarning("[GMTool] GameManager 不存在，无法切换到下一个关卡");
            return;
        }

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[GMTool] 当前已经是最后一个已加入 Build Settings 的场景，无法继续切关");
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            UIManager.Instance.ResumeGame();
        }

        Debug.Log($"[GMTool] 切换到下一关，场景索引：{nextSceneIndex}");
        GameManager.instance.EnterNextLevel();
    }
}
