using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//2D的飞行虫效果
public class FlyBug : MonoBehaviour
{
    //随机向四周小距离移动的飞行虫
    public float moveRange = 0.5f;
    public float moveSpeed = 1f;

    public float rotationSpeed = 50f;
    
    // 最大的随机抖动角度（度）和抖动速率（用于Perlin噪声）
    public float maxJitterAngle = 10f;
    public float jitterSpeed = 0.5f;
    // 最小目标距离，确保每次新目标不会与当前位置重合导致卡住
    public float minTargetDistance = 0.1f;

    private Vector3 targetPosition;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
        SetNewTargetPosition();
    }

    //只会在初始位置附近移动，同时也会朝目标位置转向
    void Update()
    {
        Vector3 pos = transform.position;

        // 目标方向（2D）
        Vector2 toTarget = new Vector2(targetPosition.x - pos.x, targetPosition.y - pos.y);

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            SetNewTargetPosition();
            return;
        }

        Vector2 dir = toTarget.normalized;

        // 使用Perlin噪声生成平滑的随机角度抖动（-maxJitterAngle..maxJitterAngle）
        float noise = (Mathf.PerlinNoise(Time.time * jitterSpeed, 0f) - 0.5f) * 2f;
        float jitter = noise * maxJitterAngle;

        // 目标角度（度），并加上抖动
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + jitter;

        // 平滑旋转到目标角度（2D，绕Z轴）
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Mathf.Clamp01(rotationSpeed * Time.deltaTime / 100f));
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 实际移动方向考虑抖动（将方向绕Z轴旋转 jitter 度）
        Vector2 moveDir = Quaternion.Euler(0f, 0f, jitter) * dir;
        Vector3 newPos = pos + new Vector3(moveDir.x, moveDir.y, 0f) * moveSpeed * Time.deltaTime;

        // 限制在以 initialPosition 为中心的圆形范围内（2D）
        Vector2 offsetFromCenter = new Vector2(newPos.x - initialPosition.x, newPos.y - initialPosition.y);
        if (offsetFromCenter.magnitude > moveRange)
        {
            offsetFromCenter = offsetFromCenter.normalized * moveRange;
            newPos.x = initialPosition.x + offsetFromCenter.x;
            newPos.y = initialPosition.y + offsetFromCenter.y;
        }

        transform.position = newPos;

        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(targetPosition.x, targetPosition.y)) < 0.1f)
        {
            SetNewTargetPosition();
        }
    }

    private void SetNewTargetPosition()
    {
        // 生成一个与当前位置至少相距 minTargetDistance 的目标点
        Vector2 currentPos2 = new Vector2(transform.position.x, transform.position.y);
        int attempts = 0;
        Vector3 candidate = initialPosition;
        do
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(minTargetDistance, moveRange);
            candidate = initialPosition + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
            attempts++;
        } while (Vector2.Distance(currentPos2, new Vector2(candidate.x, candidate.y)) < minTargetDistance && attempts < 10);

        targetPosition = candidate;
    }
}
