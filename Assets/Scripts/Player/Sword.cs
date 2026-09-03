using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject hit_Enermy_Effect;

    [Header("反冲力")]
    public float knockbackForce = 7f;

    public AttackDirection attackDirection;

    private PlayerController playerController;
    private PlayerSoulPower playerSoulPower;

    void Start()
    {
        playerController = GameManager.instance.GetPlayerController();
        playerSoulPower = playerController.GetComponent<PlayerSoulPower>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Traps")
        {
            //剑气命中陷阱在击中点产生特效
            //根据接触点法线方向产生特效（通过碰撞体中心到最近点向量近似法线）
            Vector2 hitPoint = collision.ClosestPoint(transform.position);
            Vector2 normal;
            // 使用碰撞体包围盒中心到命中点的方向近似法线
            Vector2 center = collision.bounds.center;
            normal = (hitPoint - center).normalized;
            if (normal.sqrMagnitude < 0.001f)
            {
                normal = Vector2.up;
            }
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
            PlayEffect(hitEffect, hitPoint, rot, null);

            SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);

            Vector2 knockbackDirection = GetAttackDirection();
            //给玩家一个反冲的力
            playerController.ApplyKnockback(knockbackForce, knockbackDirection);

            //恢复5点能量
            playerSoulPower.AddSoulPower(5);

            //销毁剑气
            this.enabled = false;
        }

        else if (collision.tag == "Enermy")
        {
            EnermyHealth enermyHealth = collision.GetComponent<EnermyHealth>();
            if (enermyHealth == null)
            {
                FalseKnight falseKnight = collision.GetComponent<FalseKnight>();
                if (falseKnight != null)
                {
                    falseKnight.TakeDamage(1);
                }
            }
            else
            {
                enermyHealth.TakeDamage(1);
            }

            PlayEffect(hit_Enermy_Effect, collision.transform.position, collision.transform.rotation, collision.transform);

            Vector2 knockbackDirection = GetAttackDirection();
            //给玩家一个反冲的力
            playerController.ApplyKnockback(knockbackForce * 0.6f, knockbackDirection);

            //恢复20点能量
            playerSoulPower.AddSoulPower(20);

            //销毁剑气
            this.enabled = false;
        }
    }

    private Vector2 GetAttackDirection()
    {
        Vector2 knockbackDirection = Vector2.zero;
        if (attackDirection == AttackDirection.LeftRight)
        {
            if (playerController.transform.localScale.x > 0)
            {
                knockbackDirection = Vector2.right;
            }
            else
            {
                knockbackDirection = Vector2.left;
            }
        }
        else if (attackDirection == AttackDirection.Up)
        {
            knockbackDirection = Vector2.down;
        }
        else if (attackDirection == AttackDirection.Down)
        {
            knockbackDirection = Vector2.up;
        }
        return knockbackDirection;
    }

    private GameObject PlayEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (effectPrefab == null)
        {
            return null;
        }

        GameObject effectInstance = EffectPoolManager.Play(effectPrefab, position, rotation, parent);
        if (effectInstance == null)
        {
            effectInstance = Instantiate(effectPrefab, position, rotation, parent);
        }

        return effectInstance;
    }
}
