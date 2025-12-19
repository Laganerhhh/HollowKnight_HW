using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using Cinemachine;


public class FalseKnight: MonoBehaviour
{

    public enum State
    {
        Idle,           // 待机/选择下一个动作
        JumpAttack,     // 跳跃砸地攻击
        Attack,         // 挥舞狼牙棒 (近战攻击)

        Roll,           // 眩晕时刻
        Jump,           // 简单的跳跃移动 (或准备攻击)
        CrazyAttack,    // 狂暴模式的连续攻击（或砸地波）
        Die             // 死亡
    }

    [Header("战斗属性")]
    // 外部/盔甲血量（玩家通常先伤害外部）
    public int externalMax = 50;
    private int externalHealth;

    // 内部/核心血量（盔甲倒地后才可伤害）
    public int internalMax = 30;
    private int internalHealth;

    public bool isCoreExposed = false; // 本体是否暴露，玩家是否可以伤害它
    // 当内部血量为 0 时，怪物不再恢复（不再进入 Recover）
    private bool canRecover = true;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float attackRange = 2f;
    public float jumpdirection = 1f;

    [Tooltip("两次行动之间的最小间隔时间")]
    public float actionDelay = 2f; 
    public float stunDuration = 5f; // 眩晕持续时间
    
    [Header("组件引用")]
    private State currentState;
    private Animator animator;
    private Rigidbody2D rb;
    private Transform player; // 玩家引用

    [Header("玩家和场景边界")]
    public string playerTag = "Player";

    // 内部计时器和标志
    private float nextActionTime;
    private bool isFacingRight = true;
    private int RollCount = 0;
    
    [Header("冲击波设置")]
    public GameObject shockwavePrefab;
    public float shockwaveSpeed = 10f;
    public float shockwaveLifetime = 2f;

    [Header("掉落火球设置")]
    public GameObject fireballPrefab;
    public float fireballSpeed = 10f;
    public float fireballLifetime = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        // 初始化血量
        externalHealth = externalMax;
        internalHealth = internalMax;
        ChangeState(State.Idle);
    }

    void Update()
    {
        FlipCheck();
        if (Input.GetKeyDown(KeyCode.O))
        {
            ChangeState(State.CrazyAttack);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            TakeDamage(1);
        }
        switch (currentState)
        {
            case State.Idle:
                IdleLogic();
                break;
            // 攻击和眩晕状态由协程驱动，Update 中只做监测
            case State.Roll: 
                // 处于眩晕 (Roll) 状态时，可以执行一些动画播放或玩家追踪逻辑
                break;
            case State.Die:
                break;
            default:
                break;
        }
    }

    // --- 状态机控制 ---

    private void ChangeState(State newState)
    {
        if (currentState == State.Die) return; 

        // 退出旧状态的清理工作
        if (currentState == State.Roll)
        {
            isCoreExposed = false;
        }
        
        currentState = newState;
        
        // 进入新状态的初始化
        switch (newState)
        {
            case State.Idle:
                nextActionTime = Time.time + actionDelay;
                break;
            case State.JumpAttack:
                StopAllCoroutines(); 
                StartCoroutine(JumpAttackSequence());
                break;
            case State.Attack:
                StopAllCoroutines();
                StartCoroutine(MeleeAttackSequence());
                break;
            case State.Roll: // 现在是眩晕时刻
                StopAllCoroutines();
                StartCoroutine(StunnedRollSequence());
                break;
            case State.CrazyAttack:
                StopAllCoroutines();
                StartCoroutine(ExecuteCrazyAttack());
                break;
            case State.Jump:
                StopAllCoroutines();
                StartCoroutine(JumpSequence());
                break;

        }
    }

    
    private void IdleLogic()
    {
        if (player == null || Time.time < nextActionTime) return;

        float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        if (RollCount == 0)
        {
            if (Random.value < 0.4f)
            {
                ChangeState(State.JumpAttack);
            }
            else if (Random.value < 0.7f)
            {
                ChangeState(State.Attack);
            }
            else
            {
                ChangeState(State.Jump);
            }
        }
        else
        {
            if (Random.value < 0.3f)
            {
                ChangeState(State.JumpAttack);
            }
            else if (Random.value < 0.6f)
            {
                ChangeState(State.Attack);
            }
            else if (Random.value < 0.9f)
            {
                ChangeState(State.Jump);
            }
            else
            {
                ChangeState(State.CrazyAttack);
            }
        }   
    }
    

    IEnumerator MeleeAttackSequence()
    {
        animator.SetTrigger("StartAttack");
        yield return new WaitForSeconds(0.8f);
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.5f);
        ChangeState(State.Idle);
    }
    
    IEnumerator JumpAttackSequence()
    {
        animator.SetTrigger("JumpAttack");
        float horizontalDir = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.velocity = new Vector2(horizontalDir * moveSpeed * 1.5f, jumpForce * 2f);

        yield return new WaitForSeconds(1.6f);

        ChangeState(State.Idle);
    }

    IEnumerator ExecuteCrazyAttack()
    {
        animator.SetTrigger("StartAttack");
        yield return new WaitForSeconds(0.8f);
        const int attackCount = 6;

        for (int i = 0; i < attackCount; i++)
        {
            animator.SetTrigger("Attack");
            Flip();  
            yield return new WaitForSeconds(0.7f);
        }
        ChangeState(State.Idle);
    }

    IEnumerator JumpSequence()
    {
        animator.SetTrigger("Jump");
        float horizontalDir = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.velocity = new Vector2(jumpdirection * moveSpeed * 1.5f, jumpForce * 2f);
        jumpdirection *= -1f; // 每次跳跃后改变方向
        yield return new WaitForSeconds(0.8f);

        ChangeState(State.Idle);
    }
    /// <summary>
    /// 眩晕时刻 (Roll) 的逻辑：头盔掉落，本体暴露，玩家可攻击。
    /// </summary>
    IEnumerator StunnedRollSequence()
    {
        // 1. 触发眩晕动画（可能是 Roll 或 Recover）
        animator.SetTrigger("Roll"); 
        RollCount += 1;
        // 2. 停止一切移动
        rb.velocity = Vector2.zero;
        isCoreExposed = true; // 允许玩家攻击本体（此时攻击会伤害内部血量）

        // 3. 持续眩晕时间
        yield return new WaitForSeconds(stunDuration); 

        // 4. 恢复动画（爬起来，重新戴上头盔）
        // 如果内部血量已经被击破，则直接死亡而不恢复
        if (internalHealth <= 0)
        {
            canRecover = false;
            ChangeState(State.Die);
            animator.SetTrigger("Roll");
            yield break;
        }

        // 否则恢复外部血量并播放恢复动画
        if (canRecover)
        {
            animator.SetTrigger("Recover"); 
            externalHealth = externalMax;
            isCoreExposed = false; // 重新戴上头盔，外部可被伤害
            // 恢复后回到 Idle，玩家需要重新攻击外部血量
            if (RollCount == 1)
                ChangeState(State.CrazyAttack);
            else 
                ChangeState(State.Idle);
        }
    }
    
    // --- 伤害接收逻辑 ---

    /// <summary>
    /// 接收伤害逻辑。
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (currentState == State.Die) return;

        if (isCoreExposed)
        {
            // 攻击本体（内部血量）
            internalHealth -= damage;
            // 可选：播放本体受击特效
            if (internalHealth <= 0)
            {
                // 内部血量清零 -> 死亡（不再恢复）
                canRecover = false;
                ChangeState(State.Die);
                animator.SetTrigger("Dead");
            }
        }
        else
        {
            // 攻击外部盔甲（外部血量）
            externalHealth -= damage;
            StartCoroutine(HitFeedback());

            if (externalHealth <= 0)
            {
                // 进入眩晕（Roll）阶段，暴露本体
                ChangeState(State.Roll);
            }
        }
    }
    
    IEnumerator HitFeedback()
    {
        // 简单的闪烁效果
        // GetComponent<SpriteRenderer>().color = Color.red;
        yield return new WaitForSeconds(0.1f);
        // GetComponent<SpriteRenderer>().color = Color.white;
    }


    private void FlipCheck()
    {
        
        if (currentState!=State.Idle) return;

        if (player.position.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        else if (player.position.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    private void FireShockwave()
    {

        GameObject wave = Instantiate(
            shockwavePrefab,
            transform.position,
            Quaternion.identity
        );

        float direction = isFacingRight ? 1f : -1f;

        // *** 关键更改：获取 ShockwaveController 并初始化它 ***
        ShockwaveController controller = wave.GetComponent<ShockwaveController>();
        
        if (controller != null)
        {
            controller.Initialize(shockwaveSpeed, shockwaveLifetime, direction);
            Debug.Log("冲击波已启动。");
        }
        else
        {
            Debug.LogError("冲击波 Prefab 上缺少 ShockwaveController 脚本！");
        }
    }
    

    public void OnAttackStart()
    {
        if (currentState == State.JumpAttack || currentState == State.Attack)
        {
            FireShockwave();
            Debug.Log("False Knight 发射冲击波！");
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }
}