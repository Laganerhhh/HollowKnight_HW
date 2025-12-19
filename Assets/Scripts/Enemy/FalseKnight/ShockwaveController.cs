// ShockwaveController.cs
using UnityEngine;
using System.Collections;

public class ShockwaveController : MonoBehaviour
{
    // 冲击波的移动速度和生命周期由 FalseKnight 传入
    private float speed;
    private float lifetime;
    private float direction; // 1 或 -1

    public void Initialize(float shockwaveSpeed, float shockwaveLifetime, float moveDirection)
    {
        this.speed = shockwaveSpeed;
        this.lifetime = shockwaveLifetime;
        this.direction = moveDirection;
        Debug.Log("冲击波初始化：速度=" + speed + ", 生命周期=" + lifetime + ", 方向=" + direction);
        // 启动自身的移动协程
        StartCoroutine(MoveAndDestroy());
    }

    IEnumerator MoveAndDestroy()
    {
        float startTime = Time.time;
        float endTime = startTime + lifetime;
        Vector3 moveVector = new Vector3(direction * speed, 0, 0);

        while (Time.time < endTime)
        {
            // 在自身 Transform 上移动
            transform.position += moveVector * Time.deltaTime; 
            yield return null; 
        }

        // 销毁自身
        Destroy(gameObject);
    }
}