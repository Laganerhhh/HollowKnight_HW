using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int current_health = 5;
    public int max_health = 5;

    private Animator animator;

    private PlayerController playerController;
    private PlayerSoulPower playerSoulPower;

    //是否处于无敌状态
    public bool isInvincible = false;
    [SerializeField] private float invincibleDuration = 1.0f;

    void Start()
    {
        current_health = max_health;
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerSoulPower = GetComponent<PlayerSoulPower>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && current_health < max_health)
        {
            animator.SetTrigger("recover_health");
        }
    }

    //回血，当回血动画播放完毕调用
    private void RecoverHealth()
    {
        if (playerSoulPower.UseSoulPower(SoulPowerSkill.Recovery))
        {
            HealthUIMgr.Instance.GainHealth(current_health, 1, max_health);
            current_health += 1;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible)
            return;

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
            //进入无敌状态
            isInvincible = true;
            StartCoroutine(InvincibleTimer());
            //受伤动画
            animator.SetTrigger("hit");
            SoundManager.instance.PlaySound(SoundIndex.player_injured);
            playerController.enabled = false;
            //UI生命值受伤
            HealthUIMgr.Instance.LoseHealth(current_health, damage, max_health);

        }
    }

    IEnumerator InvincibleTimer()
    {
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    private void OnHitAnimEnd()
    {
        playerController.enabled = true;
    }

    private void OnDeathAnimEnd()
    {
        GameManager.instance.GameOver();
    }
}
