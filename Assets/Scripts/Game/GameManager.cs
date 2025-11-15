using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

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
        SoundManager.instance.PlayBGM(SoundIndex.colosseumLv1_BG, 0.5f);
        playerAnimator = player.GetComponent<Animator>();
    }


    public void GameOver()
    {
        //游戏结束
        SoundManager.instance.StopBGM();

        //重新加载当前关卡
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        playerAnimator.SetTrigger("respawn"); // 触发玩家动画的复活动作
    }   
}
