using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 陷阱的基类
/// </summary>
public class TrapsBase : MonoBehaviour
{
    protected PlayerHealth playerHealth;
    protected PlayerController playerController;

    protected Animator animator;

    void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerController = FindObjectOfType<PlayerController>();
        animator = GetComponent<Animator>();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
       
    }

}
