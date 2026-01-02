using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField] private int touchDamage = 1; // 碰撞伤害数值
    [SerializeField] private float damageCooldown = 1.0f; // 伤害冷却时间，防止每帧都扣血
    private float _nextDamageTime;

    // 注意：非 Trigger 碰撞必须使用 OnCollisionEnter2D 或 OnCollisionStay2D
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
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
                
                // 设置冷却时间
                _nextDamageTime = Time.time + damageCooldown;
                
                // Debug.Log("怪物撞到了玩家，造成伤害！");
            }
        }
    }
}