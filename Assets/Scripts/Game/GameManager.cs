using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Vector3 respawnPoint;

    public GameObject player;

    public bool isLastLevel = false;

    private Animator playerAnimator;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        playerAnimator = player != null ? player.GetComponent<Animator>() : null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshCursorStateForCurrentScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isPlaying = true;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        playerAnimator = player != null ? player.GetComponent<Animator>() : null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshCursorStateForCurrentScene();
        }
    }

    public void SetIsPlaying(bool playing)
    {
        isPlaying = playing;
    }

    private bool isPlaying = true;
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            UIManager.Instance.ResumeGame();
            return;
        }

        if (isPlaying)
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 获取玩家控制脚本
    /// </summary>
    /// <returns></returns>
    public PlayerController GetPlayerController()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        return player.GetComponent<PlayerController>();
    }

    public void GameOver()
    {
        //游戏结束
        SoundManager.instance.StopBGM();

        //重新加载当前关卡
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    } 

    public void PauseGame()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PauseGame();
            return;
        }

        Time.timeScale = 0f; //暂停游戏
        isPlaying = false;
    }

    public void EnterNextLevel()
    {
        //加载下一个关卡
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex + 2 == 6)
        {
            isLastLevel = true;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex + 1);
        
    }  

    public void ResumeGame()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResumeGame();
            return;
        }

        Time.timeScale = 1f; //恢复游戏
        isPlaying = true;
    }

    public void SetRespawnPoint(Vector3 newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }

    public Vector3 GetRespawnPoint()
    {
        return respawnPoint;
    }
}
