using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthIcon : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float time_interval = 3f;

    public bool isLoseHealth = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!isLoseHealth)
        {
            InvokeRepeating("PlayHealthIconAnim", time_interval, time_interval);
        }
    }

    //每隔一段时间播放一次UI动画
    private void PlayHealthIconAnim()
    {
        animator.SetTrigger("bling");
    }

    public void SetIsLoseHealth(bool isLoseHealth)
    {
        EnsureAnimator();
        this.isLoseHealth = isLoseHealth;
        if (isLoseHealth) //掉血时播放动画
        {
            animator.SetTrigger("lose_health");
            CancelInvoke("PlayHealthIconAnim");
        }
        else //回血
        {
            animator.SetTrigger("recover_health");
            CancelInvoke("PlayHealthIconAnim");
            InvokeRepeating("PlayHealthIconAnim", time_interval, time_interval);
        }
    }

    public void SetIsLoseHealthImmediate(bool isLoseHealth)
    {
        EnsureAnimator();
        this.isLoseHealth = isLoseHealth;
        CancelInvoke("PlayHealthIconAnim");

        if (animator != null)
        {
            animator.ResetTrigger("bling");
            animator.ResetTrigger("lose_health");
            animator.ResetTrigger("recover_health");
            animator.Play(isLoseHealth ? "health_icon_hit" : "Empty", 0, isLoseHealth ? 1f : 0f);
            animator.Update(0f);
        }

        if (!isLoseHealth && isActiveAndEnabled)
        {
            InvokeRepeating("PlayHealthIconAnim", time_interval, time_interval);
        }
    }

    private void EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

}
