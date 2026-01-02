using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        playerAnimator = player.GetComponent<Animator>();

        UIManager.Instance.HideCursor();
    }

    public void SetIsPlaying(bool playing)
    {
        isPlaying = playing;
    }

    private bool isPlaying = true;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isPlaying)
        {
            PauseGame();
            isPlaying = false;
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
        Time.timeScale = 0f; //暂停游戏
        UIManager.Instance.TogglePauseUI();
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
        Time.timeScale = 1f; //恢复游戏
        UIManager.Instance.TogglePauseUI();
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
