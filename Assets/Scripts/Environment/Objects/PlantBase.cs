using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantBase : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //播放动画
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                animator.SetTrigger("interact");
        }
    }
}
