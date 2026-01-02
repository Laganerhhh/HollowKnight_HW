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
    [SerializeField] private ParticleSystem effect; // 在 Inspector 中将粒子系统拖到这里

    // 当主程序设置 hitbox.enabled = true 时，该方法会自动触发
    private void OnEnable()
    {
        if (effect != null)
        {
            // // 确保粒子系统在播放前是停止的，然后重新开始
            effect.Stop();
            effect.Play();
            
            ParticleSystem newEffect = Instantiate(effect, transform.position, transform.rotation, transform);
            //   Instantiate(effect, transform.position, Quaternion.identity,transform);
            Debug.Log("Playing hitbox effect");
            Destroy(newEffect.gameObject, 0.5f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞到的物体是不是玩家
        if (LayerMask.LayerToName(collision.gameObject.layer) != "Player") return;

        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}