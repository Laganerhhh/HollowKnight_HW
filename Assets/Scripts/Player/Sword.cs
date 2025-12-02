using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject hitEffect;

    [Header("反冲力")]
    public float knockbackForce = 7f;

    public AttackDirection attackDirection;

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

            Vector2 knockbackDirection = GetAttackDirection();
            //给玩家一个反冲的力
            playerController.ApplyKnockback(knockbackForce, knockbackDirection);
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
}
