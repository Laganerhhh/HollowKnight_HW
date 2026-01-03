using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnermyHealth : MonoBehaviour
{
    [SerializeField] private int current_health = 2;
    public int max_health = 2;

    [SerializeField] private string dieAnimParam = "Die";
    private Animator animator;

    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject injury_effect;
    [SerializeField] private GameObject hit_particle_effect;
    [SerializeField] private GameObject death_effect;

    [SerializeField] private GameObject defense_particle;

    [SerializeField] private bool isFalseknight = false;

    public bool isDefensing = false;

    private FalseKnight falseKnight = null;

    void Start()
    {
        current_health = max_health;
        animator = GetComponent<Animator>();

        if (isFalseknight)
        {
            falseKnight = GetComponentInParent<FalseKnight>();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isFalseknight)
        {
            FalseKnightTakeDamege(damage);
            return;
        }

        if (isDefensing)
        {
            DefenseDamege();
            return;
        }

        current_health -= damage;
        //创建受伤特效
        if (injury_effect != null)
        {
            Instantiate(injury_effect, transform.position, Quaternion.identity, transform);
            Instantiate(hit_particle_effect, transform.position - transform.up * 0.5f, Quaternion.identity);
        }
        //播放受伤音效
        SoundManager.instance.PlaySound(SoundIndex.enermy_damage);
        if (current_health <= 0)
        {
            current_health = 0;
            Die();
            return;
        }
    }

    private void DefenseDamege()
    {
        SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);
        Instantiate(defense_particle, transform.position + new Vector3(0.5f, 0, 0), Quaternion.identity, transform);
    }

    private void FalseKnightTakeDamege(int damage)
    {
        //创建受伤特效
        if (injury_effect != null)
        {
            Instantiate(injury_effect, transform.position + transform.up * 0.5f, Quaternion.identity, transform);
            Instantiate(hit_particle_effect, transform.position + transform.up * 0.5f, Quaternion.identity);
        }
        falseKnight.TakeDamage(damage);
    }

    void Die()
    {
        Instantiate(death_effect, transform.position - transform.up * 0.5f, Quaternion.identity);

        animator.SetTrigger(dieAnimParam);
        //禁用敌人的碰撞体等
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        //禁用敌人的移动脚本等
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }
    }

    void OnDieAnimEnd()
    {
        DestroySelf();
    }

    private void DestroySelf()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Destroy(rb);
        //删除所有脚本，只保留sprite渲染器用于播放死亡动画
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this && script.GetType() != typeof(SpriteRenderer))
            {
                Destroy(script);
            }
        }
    }

}
