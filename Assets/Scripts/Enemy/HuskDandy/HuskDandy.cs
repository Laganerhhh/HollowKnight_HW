using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HuskDandy : MonoBehaviour
{
    private State currentState = State.Idle;

    // 敌人通用属性
    public float x_min = -10f;
    public float x_max = 10f;
    public float speed = 2f;
    public int blood = 10;

    //控制
    private Animator animator;
    private bool isMoveRight = false;
    private Vector3 startPosition;
    private Vector3 originalScale;
    private float idleStartTime = 0f;
    public float idleBeforeAttackDuration = 1.0f;

    //交互
    private float attackRange = 1.0F;
    private Transform playerTransform;
    private float chaseDistance = 5f;
    private bool isAttacking = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        originalScale = transform.localScale;
        
        currentState = State.Idle;
        idleStartTime = Time.time;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    // Update is called once per frame
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
        }
    }
    void Idle()
    {
        animator.SetBool("iswalking", false);
        animator.SetTrigger("c_to_idle");
        float distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
        if (Time.time - idleStartTime >= idleBeforeAttackDuration)
        {
            if (distanceToPlayer <= attackRange)
            {
                currentState = State.Attacking;
            }
            else
            {
                currentState = State.Walking; 
                idleStartTime = Time.time; 
            }
        } 
    }
    void Move()
    {
        animator.SetBool("iswalking", true);
        if (IsOutOfBounds())
        {
            currentState = State.Turning;
            return;
        }
        float direction = isMoveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        float x_distanceToPlayer = playerTransform != null ? Mathf.Abs(playerTransform.position.x - transform.position.x) : Mathf.Infinity;
        if (x_distanceToPlayer <= chaseDistance && playerTransform.position.y <= transform.position.y + 3f)
        {
            currentState = State.Chasing;
            animator.SetBool("findplayer", true);
        }
    }
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

        animator.SetBool("isWalking", true);
        transform.Translate((isMoveRight ? 1 : -1) * speed * Time.deltaTime, 0, 0);

    }
    void TurnAround()
    {
        isMoveRight = !isMoveRight;
        //transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        if (isMoveRight)
        {
            // 移动向右，需要面向右 (负的 X Scale)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            // 移动向左，需要面向左 (正的 X Scale)
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        currentState = State.Walking;
    }
    void death()
    {
        animator.SetBool("Death",true);
    }
    void TryAttack()
    {
        FacePlayer();
        currentState = State.Attacking;
        animator.SetBool("isWalking", false);
        StartCoroutine(AttackCoroutine());
    }
    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        animator.SetTrigger("fight");

        float animationDuration = 0.5f; 
        yield return new WaitForSeconds(animationDuration);
        
        isAttacking = false;

        currentState = State.Idle;
        idleStartTime = Time.time;
    }

    bool IsOutOfBounds()
    {
        float currentX = transform.position.x;
        float leftBound = startPosition.x + x_min;
        float rightBound = startPosition.x + x_max;

        if (isMoveRight)
        {
            return currentX >= rightBound;
        }
        else
        {
            return currentX <= leftBound;
        }
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
    void FacePlayer()
    {
        if (playerTransform == null) return;
        
        // 玩家在右边 (需要怪物面向右边)
        if (playerTransform.position.x > transform.position.x)
        {
            isMoveRight = true;
            // ✅ 修正：如果原始模型面向左，面朝右需要 **负** 的 X Scale
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        // 玩家在左边 (需要怪物面向左边)
        else
        {
            isMoveRight = false;
            // ✅ 修正：如果原始模型面向左，面朝左需要 **正** 的 X Scale
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
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
            if (playerTransform.position.x < transform.position.x)
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

