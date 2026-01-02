using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField] private int touchDamage = 1; // 碰撞伤害数值
    [SerializeField] private float damageCooldown = 1.0f; // 伤害冷却时间，防止每帧都扣血
    private float _nextDamageTime;

    public bool canMakeDamege = true;

    void Start()
    {
        canMakeDamege = true;
    }

    // 注意：非 Trigger 碰撞必须使用 OnCollisionEnter2D 或 OnCollisionStay2D
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canMakeDamege) return;
        HandleDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!canMakeDamege) return;
        // 如果玩家一直贴着怪物，持续触发伤害逻辑
        HandleDamage(collision.gameObject);
    }

    private void HandleDamage(GameObject target)
    {
        if (target.CompareTag("Player") && Time.time >= _nextDamageTime)
        {
            PlayerHealth health = target.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(touchDamage);

                PlayerController playerController = target.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // 计算击退方向：从怪物指向玩家的方向
                    Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;
                    float knockbackForce = 10f; // 可根据需要调整击退力大小
                    playerController.ApplyKnockback(knockbackForce, knockbackDirection);
                }
                
                // 设置冷却时间
                _nextDamageTime = Time.time + damageCooldown;
                
                // Debug.Log("怪物撞到了玩家，造成伤害！");
            }
        }
    }
}