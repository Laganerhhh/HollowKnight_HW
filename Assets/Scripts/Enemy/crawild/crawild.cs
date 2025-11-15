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
        // 检查边界
        if (IsOutOfBounds())
        {
            StartCoroutine(TurnAround());
            return;
        }

        // 移动角色
        float direction = isMoveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // 设置行走动画
        animator.SetBool("IsWalking", true);
    }

    bool IsOutOfBounds()
    {
        float currentX = transform.position.x;
        float leftBound = startPosition.x + x_min;
        float rightBound = startPosition.x + x_max;
        Debug.Log("CurrentX: " + currentX + ", LeftBound: " + leftBound + ", RightBound: " + rightBound);
        Debug.Log("CurrentX: " + currentX + ", LeftBound: " + leftBound + ", RightBound: " + rightBound);
        return currentX >= rightBound || currentX <= leftBound;
    }

    IEnumerator TurnAround()
    {
        isTurning = true;

        // 停止移动并播放转向动画
        //animator.SetBool("IsWalking", false);
        animator.SetBool("turn",true);

        // 等待转向动画完成
        yield return new WaitForSeconds(0.5f);

        // 改变方向并翻转角色
        isMoveRight = !isMoveRight;
        FlipCharacter();

        // 稍微移动一点，避免卡在边界
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

    // 辅助方法：获取移动范围中心点
    float GetCenterPosition()
    {
        return startPosition.x + (x_min + x_max) / 2f;
    }

    // 调试用：在Scene视图中显示移动范围
    void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.red;

        // 绘制边界线
        Gizmos.DrawLine(
            new Vector3(currentStart.x + x_min, currentStart.y - 0.5f, currentStart.z),
            new Vector3(currentStart.x + x_max, currentStart.y - 0.5f, currentStart.z)
        );

        // 绘制边界点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_min, currentStart.y, currentStart.z), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_max, currentStart.y, currentStart.z), 0.3f);
    }
}