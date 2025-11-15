using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthIcon : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float time_interval = 3f;

    public bool isLoseHealth = false;

    private void Start()
    {
        animator = GetComponent<Animator>();

        InvokeRepeating("PlayHealthIconAnim", time_interval, time_interval);
    }

    //每隔一段时间播放一次UI动画
    private void PlayHealthIconAnim()
    {
        animator.SetTrigger("bling");
    }

    public void SetIsLoseHealth(bool isLoseHealth)
    {
        this.isLoseHealth = isLoseHealth;
        if (isLoseHealth) //掉血时播放动画
        {
            animator.SetTrigger("lose_health");
        }
        else //回血
        {
            //TODO
        }
    }

}
