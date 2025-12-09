using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 圆锯陷阱脚本
/// </summary>
public class Saw : TrapsBase
{
    public Transform[] movPath; //移动路径点数组

    public float speed = 2.0f; //移动速度

    
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.CompareTag("Player"))
        {
            playerHealth.TakeDamage(1, DamageType.TrapDamage);
        }
    }

    
    void Update()
    {
        if (trapsType == TrapsType.Moveable)
        {
            MoveAlongPath();
        }
    }

    //在移动路径点之间移动
    void MoveAlongPath()
    {
        if (movPath.Length == 0) return;

        //移动到下一个路径点
        Transform targetPoint = movPath[0];
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

        //如果到达路径点，更新目标点为下一个路径点
        if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            //将当前路径点移到数组末尾，实现循环移动
            List<Transform> pointsList = new List<Transform>(movPath);
            pointsList.Add(pointsList[0]);
            pointsList.RemoveAt(0);
            movPath = pointsList.ToArray();
        }
    }
}
