// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class hitbox : MonoBehaviour
// {
//     [SerializeField] private int damage = 1;
//     // 当有物体进入这个触发器时执行
//     private void OnTriggerEnter2D(Collider2D collision)
//     {
//         // 检查碰撞到的物体是不是玩家
//         string layerName = LayerMask.LayerToName(collision.gameObject.layer);
//         if (layerName != "Player") return;
//         else
//         {
//             PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
//             if (playerHealth != null)
//             {
//                 playerHealth.TakeDamage(damage);
//             }
//         }
//     }
// }
using UnityEngine;

public class hitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [Header("特效设置")]
    [SerializeField] private GameObject effect; // 在 Inspector 中将粒子系统拖到这里

    public bool canMakeDamege = true;

    // 当主程序设置 hitbox.enabled = true 时，该方法会自动触发
    private void OnEnable()
    {
        if (effect != null)
        {
            GameObject newEffect = Instantiate(effect, transform.position + new Vector3(0, -1f, 0), Quaternion.identity);
            newEffect.transform.Rotate(-90, 0, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞到的物体是不是玩家
        if (LayerMask.LayerToName(collision.gameObject.layer) != "Player") return;
        if (!canMakeDamege) return;
        
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}