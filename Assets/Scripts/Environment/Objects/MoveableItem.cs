using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveType
{
    /// <summary>
    /// 匀速直线移动（恒定速度）
    /// </summary>
    Linear,

    /// <summary>
    /// 非线性移动：使用 Vector3.Lerp 并对 t 应用缓动（SmoothStep）
    /// </summary>
    NonLinear
}

/// <summary>
/// 在给定路径 `movePath` 中循环移动的组件。
/// - 线性：使用 `Vector3.MoveTowards`（匀速）
/// - 非线性：使用 `Vector3.Lerp` 并在 t 上使用 `Mathf.SmoothStep` 实现缓动
/// 支持循环、是否使用局部坐标和在点停留若干秒。
/// </summary>
public class MoveableItem : MonoBehaviour
{
    [Tooltip("路径点，至少需要 2 个点")] public Transform[] movePath;

    [Tooltip("移动类型：Linear 或 NonLinear")] public MoveType moveType = MoveType.Linear;

    [Tooltip("移动速度（世界单位/秒），对于非线性会作为平均速率使用")] public float speed = 3f;

    [Tooltip("是否使用局部坐标（LocalPosition）而非世界坐标（Position）")] public bool useLocalPosition = false;

    [Tooltip("是否在到达最后一个点后循环回到第一个点")] public bool loop = true;

    [Tooltip("到达每个目标点后等待的秒数")] public float waitAtPoint = 0f;

    [Tooltip("是否在玩家（指定标签）接触时携带玩家一起移动")] public bool carryPlayer = false;
    [Tooltip("用于识别玩家的标签，通常为 'Player'")] public string playerTag = "Player";

    [Tooltip("是否仅当玩家从平台顶部接触时才携带（推荐用于跳跃/侧面碰撞过滤）")] public bool carryOnlyFromTop = true;

    // 保存被携带对象的原始父对象，以便离开时恢复
    Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();

    void Start()
    {
        if (movePath == null || movePath.Length < 2)
        {
            Debug.LogWarning("MoveableItem: movePath 长度不足，至少需要 2 个点。移动已禁用。", this);
            return;
        }

        // 立即把物体放到第一个点，保证从第一个点开始移动
        SetPositionToTransform(movePath[0]);

        StartCoroutine(MoveAlongPath());
    }

    IEnumerator MoveAlongPath()
    {
        int current = 0;
        int count = movePath.Length;

        while (true)
        {
            Transform from = movePath[current];
            Transform to = movePath[(current + 1) % count];

            Vector3 startPos = useLocalPosition ? from.localPosition : from.position;
            Vector3 endPos = useLocalPosition ? to.localPosition : to.position;

            if (moveType == MoveType.Linear)
            {
                // 使用 MoveTowards 实现匀速移动
                while (Vector3.Distance(GetCurrentPosition(), endPos) > 0.001f)
                {
                    Vector3 next = Vector3.MoveTowards(GetCurrentPosition(), endPos, speed * Time.deltaTime);
                    SetPosition(next);
                    yield return null;
                }
            }
            else // NonLinear
            {
                // 使用 Lerp，但对 t 使用 SmoothStep 做缓动
                float distance = Vector3.Distance(startPos, endPos);
                float duration = Mathf.Max(0.0001f, distance / Mathf.Max(0.0001f, speed));
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 next = Vector3.Lerp(startPos, endPos, eased);
                    SetPosition(next);
                    yield return null;
                }
                // 确保精确到达终点
                SetPosition(endPos);
            }

            // 到达点后可等待
            if (waitAtPoint > 0f)
            {
                yield return new WaitForSeconds(waitAtPoint);
            }

            current++;
            if (current >= count - 0)
            {
                // 如果不循环并且已经走到最后一个点（current 指向最后一段的起点），停止
                if (!loop && current >= count - 1)
                {
                    yield break;
                }

                // 环形索引
                current = current % count;
            }
        }
    }

    Vector3 GetCurrentPosition()
    {
        return useLocalPosition ? transform.localPosition : transform.position;
    }

    void SetPosition(Vector3 pos)
    {
        if (useLocalPosition) transform.localPosition = pos;
        else transform.position = pos;
    }

    void SetPositionToTransform(Transform t)
    {
        if (t == null) return;
        if (useLocalPosition) transform.localPosition = t.localPosition;
        else transform.position = t.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!carryPlayer) return;
        if (other == null || !other.CompareTag(playerTag)) return;

        Transform playerRoot = other.transform.root;

        bool accept = true;
        if (carryOnlyFromTop)
        {
            accept = false;
            // 触发器没有 contact normal，可用射线从玩家向下检测是否正位于本平台之上
            Vector2 origin = other.transform.position;
            float checkDist = 1f; // 适度距离以覆盖玩家 collider 半高
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, checkDist);
            Collider2D myCol = GetComponent<Collider2D>();
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                { 
                    if (hit.collider == myCol || hit.collider.transform.IsChildOf(transform))
                        accept = true;
                }
            }
        }
        if (accept)
        {
            if (!originalParents.ContainsKey(playerRoot)) originalParents[playerRoot] = playerRoot.parent;
            playerRoot.SetParent(this.transform);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!carryPlayer) return;
        if (other == null || !other.CompareTag(playerTag)) return;
        other.transform.SetParent(null);
    }
}
