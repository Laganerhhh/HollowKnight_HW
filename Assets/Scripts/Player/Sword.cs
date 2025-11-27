using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject hitEffect;

    [Header("反冲力")]
    public float knockbackForce = 7f;

    private PlayerController playerController;

    void Start()
    {
        playerController = GameManager.instance.GetPlayerController();
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ground")
        {
            //剑气命中地面在击中点产生特效
            Instantiate(hitEffect, collision.ClosestPoint(transform.position), Quaternion.identity);
            SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);
        }
        else if (collision.tag == "Traps")
        {
            //剑气命中陷阱在击中点产生特效
            Instantiate(hitEffect, collision.ClosestPoint(transform.position), Quaternion.identity);
            SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);

            //给玩家一个反冲的力
            Vector2 knockbackDirection = (new Vector2(playerController.transform.position.x, playerController.transform.position.y) - 
            collision.ClosestPoint(transform.position)).normalized;
            Debug.Log("Knockback Direction: " + knockbackDirection);
            playerController.ApplyKnockback(knockbackForce, knockbackDirection);
        }
    }
}
