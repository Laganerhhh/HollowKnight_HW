using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Transform respawnPoint;

    public GameObject player;

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

        player.transform.position = respawnPoint.position;
        playerAnimator.SetTrigger("respawn"); // 触发玩家动画的复活动作
    } 

    public void EnterNextLevel()
    {
        //加载下一个关卡
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex + 1);
    }  
}
