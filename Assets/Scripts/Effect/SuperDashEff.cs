using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperDashEff : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void OnSuperDashStart()
    {   
        if (animator == null)
            animator = GetComponent<Animator>();
        //animator.ResetTrigger("superdash_end");
    }

    public void OnSuperDashEnd()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        animator.SetTrigger("superdash_end");
    }
}
