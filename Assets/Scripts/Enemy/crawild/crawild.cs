using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class crawild : MonoBehaviour
{
    [Header("Movement Settings")]
    public float x_min = -10f;
    public float x_max = 10f;
    public float speed = 2f;

    [Header("Animation Settings")]
    public string walkAnimation = "Crawild";
    public string turnAnimation = "Turn";
    public string deathAnimation = "Die";
    [Header("Enemy Status")]
    public int blood = 5;

    private Animator animator;
    private Vector3 startPosition;
    public bool isMoveRight = true;
    private bool isTurning = false;
    private Vector3 originalScale;

    void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        originalScale = transform.localScale;


    }

    void Update()
    {
        if (blood==0)
        {
            animator.SetBool("Die",true);
            return;
        }
        if (!isTurning)
        {
            Move();
        }
    }

    void Move()
    {
        // ���߽�
        if (IsOutOfBounds())
        {
            StartCoroutine(TurnAround());
            return;
        }

        // �ƶ���ɫ
        float direction = isMoveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // �������߶���
        animator.SetBool("IsWalking", true);
    }

    bool IsOutOfBounds()
    {
        float currentX = transform.position.x;
        float leftBound = startPosition.x + x_min;
        float rightBound = startPosition.x + x_max;
        return currentX >= rightBound || currentX <= leftBound;
    }

    IEnumerator TurnAround()
    {
        isTurning = true;

        // ֹͣ�ƶ�������ת�򶯻�
        //animator.SetBool("IsWalking", false);
        animator.SetBool("turn",true);

        // �ȴ�ת�򶯻����
        yield return new WaitForSeconds(0.5f);

        // �ı䷽�򲢷�ת��ɫ
        isMoveRight = !isMoveRight;
        FlipCharacter();

        // ��΢�ƶ�һ�㣬���⿨�ڱ߽�
        float direction = isMoveRight ? 0.1f : -0.1f;
        transform.Translate(Vector3.right * direction);

        isTurning = false;

    }

    void FlipCharacter()
    {
        if (isMoveRight)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    // ������������ȡ�ƶ���Χ���ĵ�
    float GetCenterPosition()
    {
        return startPosition.x + (x_min + x_max) / 2f;
    }

    // �����ã���Scene��ͼ����ʾ�ƶ���Χ
    void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.red;

        // ���Ʊ߽���
        Gizmos.DrawLine(
            new Vector3(currentStart.x + x_min, currentStart.y - 0.5f, currentStart.z),
            new Vector3(currentStart.x + x_max, currentStart.y - 0.5f, currentStart.z)
        );

        // ���Ʊ߽��
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_min, currentStart.y, currentStart.z), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_max, currentStart.y, currentStart.z), 0.3f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Player")
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            playerHealth.TakeDamage(1, DamageType.CollideDamage);
            //击退玩家
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            float knockbackForce = 5f;
            if (playerController.transform.position.x < transform.position.x)
            {
                // 玩家在左侧，向左击退
                playerController.ApplyKnockback(knockbackForce, Vector2.left);
            }
            else
            {
                // 玩家在右侧，向右击退
                playerController.ApplyKnockback(knockbackForce, Vector2.right);
            }
        }
    }
}