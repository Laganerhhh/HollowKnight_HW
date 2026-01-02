using UnityEngine;
using System.Collections;

public class ShockwaveController : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private float direction;

    [Header("生长设置")]
    [SerializeField] private float growDuration = 0.2f; // 冲击波变到最大所需的时间
    private Vector3 targetScale;

    public void Initialize(float shockwaveSpeed, float shockwaveLifetime, float moveDirection)
    {
        this.speed = shockwaveSpeed;
        this.lifetime = shockwaveLifetime;
        this.direction = moveDirection;

        // 记录初始设定的缩放值（假设你在 Prefab 里调好了最终大小）
        targetScale = transform.localScale;
        
        // 初始 X 缩放设为 0
        transform.localScale = new Vector3(0, targetScale.y, targetScale.z);

        StartCoroutine(MoveAndDestroy());
    }

    IEnumerator MoveAndDestroy()
    {
        float startTime = Time.time;
        float endTime = startTime + lifetime;
        Vector3 moveVector = new Vector3(direction * speed, 0, 0);

        while (Time.time < endTime)
        {
            // --- 1. 移动逻辑 ---
            transform.position += moveVector * Time.deltaTime;

            // --- 2. 缩放逻辑 (生长) ---
            float elapsed = Time.time - startTime;
            if (elapsed < growDuration)
            {
                // 计算当前的进度 (0 到 1)
                float t = elapsed / growDuration;
                // 只平滑改变 X 轴
                float newX = Mathf.Lerp(0, targetScale.x, t);
                transform.localScale = new Vector3(newX, targetScale.y, targetScale.z);
            }
            else
            {
                // 确保生长结束后保持在目标缩放
                transform.localScale = targetScale;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}