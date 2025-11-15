using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int current_health = 5;
    public int max_health = 5;

    private Animator animator;

    private PlayerController playerController;

    void Start()
    {
        current_health = max_health;
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    public void TakeDamage(int damage)
    {
        damage = Mathf.Min(damage, current_health);
        current_health -= damage;
        if (current_health <= 0)
        {
            //死亡
            HealthUIMgr.Instance.LoseHealth(current_health, damage, max_health);
            playerController.enabled = false;
            animator.SetTrigger("death");
            SoundManager.instance.PlaySound(SoundIndex.player_death);
        }
        else
        {
            //受伤动画
            animator.SetTrigger("hit");
            SoundManager.instance.PlaySound(SoundIndex.player_injured);
            playerController.enabled = false;
            //UI生命值受伤
            HealthUIMgr.Instance.LoseHealth(current_health, damage, max_health);

        }
    }

    private void OnHitAnimEnd()
    {
        playerController.enabled = true;
    }
}
