using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
    NormalDamage,
    TrapDamage
}

public class PlayerHealth : MonoBehaviour
{
    public int current_health = 5;
    public int max_health = 5;

    private Animator animator;

    private PlayerController playerController;
    private PlayerSoulPower playerSoulPower;
    private Rigidbody2D rb2d;

    // 最后一个安全位置（掉落或陷阱将回到此位置）
    public Vector2 safePosition;
    [Tooltip("低于此 Y 值视为掉落，需要重置到安全点")] public float fallDeathY = -20f;
    [Tooltip("重生后短时间无敌，避免连续伤害")] public float respawnInvincibleDuration = 1.0f;

    //是否处于无敌状态
    public bool isInvincible = false;
    [SerializeField] private float invincibleDuration = 1.0f;

    void Start()
    {
        current_health = max_health;
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerSoulPower = GetComponent<PlayerSoulPower>();
        rb2d = GetComponent<Rigidbody2D>();

        // 初始化安全点为当前出生点
        safePosition = transform.position;
    }

    IEnumerator RespawnAndInvincible()
    {
        // 等待一帧以确保受伤反馈（动画等）开始
        yield return null;
        RespawnToSafePosition();
        // 进入短暂无敌
        isInvincible = true;
        yield return new WaitForSeconds(respawnInvincibleDuration);
        isInvincible = false;
    }

    void RespawnToSafePosition()
    {
        // 将玩家移动到安全点并重置速度与状态
        transform.position = safePosition;
        if (rb2d != null)
        {
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && current_health < max_health)
        {
            animator.SetTrigger("recover_health");
        }

        // 掉落检测：如果低于阈值则认为掉落，回到安全点并进入短暂无敌
        if (transform.position.y < fallDeathY)
        {
            TakeDamage(1);
            StartCoroutine(RespawnAndInvincible());
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

    public void TakeDamage(int damage, DamageType damageType = DamageType.NormalDamage)
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

            // 如果是陷阱伤害，立即重置到最近的安全位置
            if (damageType == DamageType.TrapDamage)
            {
                StartCoroutine(RespawnAndInvincible());
            }
            
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
