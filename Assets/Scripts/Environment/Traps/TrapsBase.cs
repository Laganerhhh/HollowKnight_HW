using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrapsType
{
    Static,
    Moveable
}

/// <summary>
/// 陷阱的基类
/// </summary>
public class TrapsBase : MonoBehaviour
{
    protected PlayerHealth playerHealth;
    protected PlayerController playerController;

    protected Animator animator;

    public TrapsType trapsType = TrapsType.Static; //陷阱类型



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
