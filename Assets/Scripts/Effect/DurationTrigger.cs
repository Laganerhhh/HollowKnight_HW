using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DurationTrigger : MonoBehaviour
{
    public float duration = 5f;

    private Animator animator;

    private bool isActive = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        InvokeRepeating("active", 0.5f, duration);
    }


    private void active()
    {
        isActive = !isActive;
        animator.SetBool("isActive", isActive);
    }


}
