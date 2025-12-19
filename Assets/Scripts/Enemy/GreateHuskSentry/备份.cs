// using System.Collections;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using UnityEngine;

// enum State
// {
//     Idle,
//     Walking,
//     Attacking,
//     Turning,
//     Chasing,
//     Dead
// };

// public class GreateHusk : MonoBehaviour
// {
//     private State currentState = State.Idle;

//     public float x_min = -10f;
//     public float x_max = 10f;
//     public float speed = 2f;

//     public int blood = 10;


//     private Animator animator;

//     private Vector3 startPosition;
//     private Vector3 originalScale;
    
//     // 控制是否允许移动（攻击时禁止）
//     private bool movementEnabled = true;
//     // 攻击开始时锁定的位置，防止动画/根运动造成位移
//     private Vector3 attackLockPosition;

//     public bool isMoveRight = true;

//     private float idleStartTime = 0f;

//     public float chaseDistance = 4f;
//     public float attackRange = 1.5f;

//     public float attackCooldown = 0f; // 两次攻击之间的最短间隔时间
//     private float nextAttackTime = 0f;  // 下一次可以攻击的时间点
//     private bool isAttacking = false;   // 标志，用于防止在动画播放期间重复触发
//     private Transform playerTransform;
//     // Start is called before the first frame update
//     void Start()
//     {
//         animator = GetComponent<Animator>();
//         startPosition = transform.position;
//         originalScale = transform.localScale;
//         currentState = State.Idle;
//         idleStartTime = Time.time;
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player != null)
//         {
//             playerTransform = player.transform;
//         }
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         switch (currentState)
//         {
//             case State.Idle:
//                 Idle();
//                 break;
//             case State.Walking:
//                 Move();
//                 break;
//             case State.Turning:
//                 TurnAround();
//                 break;
//             case State.Dead:
//                 death();
//                 break;
//             case State.Attacking:
//                 TryAttack();
//                 break;
//             case State.Chasing:
//                 Chase();
//                 break;
//         }
//         if (currentState == State.Attacking && isAttacking)
//         {
//             // 在攻击动画期间锁定位置
//             transform.position = attackLockPosition;
//         }
//     }
//     void Idle()
//     {
//         animator.SetBool("isWalking", false);
//         if (Time.time - idleStartTime >= 1f)
//         {
//             currentState = State.Walking;
//             idleStartTime = Time.time; // 重置计时器
//         }
//     }
//     void Move()
//     {
//         if (!movementEnabled)
//         {
//             animator.SetBool("isWalking", false);
//             return;
//         }

//         animator.SetBool("isWalking", true);
//         if (IsOutOfBounds())
//         {
//             currentState = State.Turning;
//             return;
//         }
//         float direction = isMoveRight ? 1f : -1f;
//         transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

//         float x_distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
//         if (x_distanceToPlayer <= attackRange && currentState != State.Dead)
//         {
//             currentState = State.Attacking;
//             animator.SetTrigger("findplayer");
//         }
//     }
   
//     void Chase()
//     {
//         if (playerTransform == null)
//         {
//             currentState = State.Idle;
//             return;
//         }

//         float distanceToPlayer = Mathf.Abs(playerTransform.position.x - transform.position.x);

//         if (distanceToPlayer > chaseDistance)
//         {
//             currentState = State.Idle;
//             return;
//         }

//         // 面向玩家（攻击期间锁定朝向，不改变）
//         if (!isAttacking)
//         {
//             if (playerTransform.position.x > transform.position.x)
//             {
//                 isMoveRight = true;
//                 transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
//             }
//             else
//             {
//                 isMoveRight = false;
//                 transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
//             }
//         }


//         if (distanceToPlayer <= attackRange)
//         {
//              // 状态切换到 Attacking，然后 Update() 会调用 TryAttack()
//              currentState = State.Attacking;
//              return; 
//         }

//         if (movementEnabled)
//         {
//             transform.Translate((isMoveRight ? 1 : -1) * speed * Time.deltaTime, 0, 0);
//         }

//     }
//     void TryAttack()
//     {
//         // 如果正在攻击或冷却中，则不做任何事，保持当前状态
//         if (Time.time < nextAttackTime || isAttacking)
//         {
//             currentState = State.Attacking;
//             return;
//         }
//         // 在攻击前锁定位置和朝向，停止移动
//         if (playerTransform.position.x > transform.position.x)
//         {
//             isMoveRight = true;
//             transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
//         }
//         else
//         {
//             isMoveRight = false;
//             transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
//         }
//         currentState = State.Attacking;
//         movementEnabled = false;
//         attackLockPosition = transform.position;
//         animator.SetBool("isWalking", false);

//         // 启动攻击协程
//         StartCoroutine(AttackCoroutine());
//     }

//     IEnumerator AttackCoroutine()
//     {
//         isAttacking = true;
//         animator.SetTrigger("fight");


//         float animationDuration = 0.5f; 

//         yield return new WaitForSeconds(animationDuration);
        


//         isAttacking = false;
//         nextAttackTime = Time.time + attackCooldown;
//         // 恢复移动
//         movementEnabled = true;
        
//         // 退出攻击状态，检查玩家是否仍在追击范围内
//         float distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
        
//         if (distanceToPlayer <= chaseDistance)
//         {
//             currentState = State.Chasing; // 玩家仍在仇恨范围，继续追击
//         }
//         else
//         {
//             currentState = State.Idle; // 玩家已脱离，返回空闲/巡逻
//         }
//     }
//     bool IsOutOfBounds()
//     {
//         float currentX = transform.position.x;
//         float leftBound = startPosition.x + x_min;
//         float rightBound = startPosition.x + x_max;
//         Debug.Log("CurrentX: " + currentX + ", LeftBound: " + leftBound + ", RightBound: " + rightBound);
//         Debug.Log("CurrentX: " + currentX + ", LeftBound: " + leftBound + ", RightBound: " + rightBound);
//         if(isMoveRight)
//         {
//             return currentX >= rightBound;
//         }
//         else
//         {
//             return currentX <= leftBound;
//         }
//     }

//     void TurnAround()
//     {
//         isMoveRight = !isMoveRight;
//         transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
//         currentState = State.Walking;
//     }
//     void death()
//     {
//         animator.SetTrigger("Death");
//     }


//     // 获取移动范围的中心点
//     float GetCenterPosition()
//     {
//         return startPosition.x + (x_min + x_max) / 2f;
//     }

//     // 在Scene视图中显示移动范围
//     void OnDrawGizmosSelected()
//     {
//         Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
//         Gizmos.color = Color.red;

//         // 移动范围边界
//         Gizmos.DrawLine(
//             new Vector3(currentStart.x + x_min, currentStart.y - 0.5f, currentStart.z),
//             new Vector3(currentStart.x + x_max, currentStart.y - 0.5f, currentStart.z)
//         );
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_min, currentStart.y, currentStart.z), 0.3f);
//         Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_max, currentStart.y, currentStart.z), 0.3f);
//     }
// }
