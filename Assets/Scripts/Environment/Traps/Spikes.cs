using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : TrapsBase
{
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.CompareTag("Player"))
        {
            playerHealth.TakeDamage(1, DamageType.TrapDamage);
            if (haveAnimator && animator != null)
            {
                animator.SetTrigger("interact");
            }
        }
    }
}
