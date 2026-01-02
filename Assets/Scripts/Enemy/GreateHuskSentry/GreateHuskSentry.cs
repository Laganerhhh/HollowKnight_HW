using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 【修改点 1】新增 Defense 状态
public enum State
{
    Idle,
    Walking,
    Attacking,
    Turning,
    Chasing,
    Dead,
    Defense // 防御状态
};

public class GreateHusk : MonoBehaviour
{
    private State currentState = State.Idle;

    public float x_min = -10f;
    public float x_max = 10f;
    public float speed = 2f;

    public int blood = 10;
    public float defenseDuration = 0.3f; // 【新增】防御动画持续时间
    public float defenseCooldown = 3.0f; // 防御动作的冷却时间
    private float nextDefenseTime = 0f; // 下一次可以防御的时间点
    private Animator animator;

    private Vector3 startPosition;
    private Vector3 originalScale;
    
    // 控制是否允许移动（攻击时禁止）
    private bool movementEnabled = true;
    // 攻击开始时锁定的位置，防止动画/根运动造成位移
    private Vector3 attackLockPosition;

    public bool isMoveRight = true;
    
    public float idleBeforeAttackDuration = 1.0f; // 【新增】进入攻击范围后，等待 idle 的时间
    private float idleStartTime = 0f;

    public float chaseDistance = 4f;
    public float attackRange = 1.5f;

    public float attackCooldown = 0f; // 两次攻击之间的最短间隔时间
    private float nextAttackTime = 0f; // 下一次可以攻击的时间点
    private bool isAttacking = false;  // 标志，用于防止在动画播放期间重复触发
    private Transform playerTransform;

    void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        originalScale = transform.localScale;
        currentState = State.Idle;
        idleStartTime = Time.time;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                Idle();
                break;
            case State.Walking:
                Move();
                break;
            case State.Turning:
                TurnAround();
                break;
            case State.Dead:
                death();
                break;
            case State.Attacking:
                TryAttack();
                break;
            case State.Chasing:
                Chase();
                break;
            case State.Defense: // Defense 状态由协程控制，Update 保持空闲
                break;
        }
        
        // 【位置锁定】在攻击和防御动画期间强制锁定位置
        if ((currentState == State.Attacking && isAttacking) || currentState == State.Defense)
        {
            transform.position = attackLockPosition;
        }
    }
    
    // 【修改点 2】Idle 状态逻辑：等待计时结束
    void Idle()
    {
        animator.SetBool("isWalking", false);
        // 如果需要播放特定的 Idle 动画过渡，可以在这里触发
        animator.SetTrigger("c_to_idle"); 

        // 检查玩家距离 (用于 Idle 结束后的判断)
        float distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
        if(Input.GetKeyDown(KeyCode.J) &&
           Time.time >= nextDefenseTime)
        {
            animator.SetTrigger("defense");
            nextDefenseTime = Time.time + defenseCooldown;
            StartCoroutine(DefenseCoroutine());
            currentState = State.Defense; // 切换到防御状态
        }
        // 计时结束
        if (Time.time - idleStartTime >= idleBeforeAttackDuration)
        {
            if (distanceToPlayer <= attackRange)
            {
                // 计时结束，玩家仍在攻击范围 -> 切换到 Attacking (TryAttack 会在 Update 中执行)
                currentState = State.Attacking;
            }
            else
            {
                // 计时结束，玩家跑远 -> 切换到 Walking/巡逻
                currentState = State.Walking; 
                idleStartTime = Time.time; 
            }
        }
    }

        void Move()
    {
        if (!movementEnabled)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        animator.SetBool("isWalking", true);
        if (IsOutOfBounds())
        {
            currentState = State.Turning;
            return;
        }
        float direction = isMoveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        float x_distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
        if (x_distanceToPlayer <= chaseDistance&& playerTransform.position.y <= transform.position.y + 3f)
        {
            currentState = State.Chasing;
            animator.SetBool("findplayer", true);
        }
    }
    
    // 【修改点 3】Chase 状态逻辑：进入攻击范围后转为 Idle
    void Chase()
    {
        if (playerTransform == null)
        {
            currentState = State.Idle;
            return;
        }

        float distanceToPlayer = Mathf.Abs(playerTransform.position.x - transform.position.x);

        if (distanceToPlayer > chaseDistance && playerTransform.position.y > transform.position.y + 3f)
        {
            // 玩家跑远，返回 Idle/Walking
            currentState = State.Idle;
            idleStartTime = Time.time;
            return;
        }

        // 面向玩家
        if (!isAttacking)
        {
            FacePlayer();
        }

        if (distanceToPlayer <= attackRange)
        {
             currentState = State.Idle;
             idleStartTime = Time.time; // 开始 Idle 计时
             animator.SetTrigger("c_to_idle"); // 触发 Idle 过渡动画
             return; 
        }

        // 追逐移动
        if (movementEnabled)
        {
            animator.SetBool("isWalking", true);
            transform.Translate((isMoveRight ? 1 : -1) * speed * Time.deltaTime, 0, 0);
        }
    }
    
    // 【修改点 4】TryAttack 方法：准备和启动攻击
    void TryAttack()
    {
        // 检查冷却
        if (Time.time < nextAttackTime || isAttacking)
        {
            currentState = State.Attacking;
            return;
        }
        
        FacePlayer(); // 确保攻击前朝向正确

        currentState = State.Attacking;
        movementEnabled = false;
        attackLockPosition = transform.position;
        animator.SetBool("isWalking", false);

        StartCoroutine(AttackCoroutine());
    }

    // 【修改点 5】AttackCoroutine 方法：攻击结束后，再次检查是否在攻击范围
    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        animator.SetTrigger("fight"); // 攻击（FBack）动画

        float animationDuration = 0.5f; 
        yield return new WaitForSeconds(animationDuration);
        
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
        movementEnabled = true;
        
        // 检查玩家距离，决定下一个状态
        float distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;

        if (distanceToPlayer <= attackRange)
        {
            currentState = State.Idle; // 玩家还在攻击范围，继续 Idle -> 攻击循环
            idleStartTime = Time.time; // 重启 Idle 计时
        }
        else if (distanceToPlayer <= chaseDistance)
        {
            currentState = State.Chasing; // 玩家在追逐范围，但不在攻击范围，继续追逐
        }
        else
        {
            currentState = State.Walking; // 玩家跑远，返回巡逻
        }
    }
    

    IEnumerator DefenseCoroutine()
    {
        // 1. 进入 Defense 状态，并锁定位置和朝向
        currentState = State.Defense;
        movementEnabled = false;
        isAttacking = false; 
        animator.SetBool("isWalking", false);
        
        FacePlayer(); // 确保防御时面向玩家
        attackLockPosition = transform.position; // 锁定防御位置

        // 2. 播放 Defense 动画
        animator.SetTrigger("defense"); // 防御动画触发器

        // 等待防御动画播放完毕
        yield return new WaitForSeconds(defenseDuration);

        // 3. Defense 结束，直接攻击 FBack (反击)
        
        // 确保移动锁定，然后启动攻击协程（AttackCoroutine会自动处理攻击结束后的状态切换）
        movementEnabled = false; 
        yield return StartCoroutine(AttackCoroutine()); 
        
        // 注意：AttackCoroutine 结束后会自动设置 currentState 和 movementEnabled
    }
    
    // --- 辅助方法 ---
    
    // 确保面向玩家
    void FacePlayer()
    {
        if (playerTransform == null) return;
        
        if (playerTransform.position.x > transform.position.x)
        {
            isMoveRight = true;
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            isMoveRight = false;
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    bool IsOutOfBounds()
    {
        float currentX = transform.position.x;
        float leftBound = startPosition.x + x_min;
        float rightBound = startPosition.x + x_max;
        
        if(isMoveRight)
        {
            return currentX >= rightBound;
        }
        else
        {
            return currentX <= leftBound;
        }
    }

    void TurnAround()
    {
        isMoveRight = !isMoveRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        currentState = State.Walking;
    }
    
    void death()
    {
        animator.SetTrigger("Death");
    }

    public void TakeDamage(int damage)
    {
        if (currentState == State.Dead) return;

        blood -= damage;
        if (blood <= 0)
        {
            blood = 0;
            currentState = State.Dead;
            death();
        }
    }
    // 获取移动范围的中心点
    float GetCenterPosition()
    {
        return startPosition.x + (x_min + x_max) / 2f;
    }

    // 在Scene视图中显示移动范围
    void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.red;

        // 移动范围边界
        Gizmos.DrawLine(
            new Vector3(currentStart.x + x_min, currentStart.y - 0.5f, currentStart.z),
            new Vector3(currentStart.x + x_max, currentStart.y - 0.5f, currentStart.z)
        );
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_min, currentStart.y, currentStart.z), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_max, currentStart.y, currentStart.z), 0.3f);
    }
}