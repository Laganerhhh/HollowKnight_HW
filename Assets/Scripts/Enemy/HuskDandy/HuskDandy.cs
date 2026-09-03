using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuskDandy : MonoBehaviour, IPoolableEnemy
{
    private State currentState = State.Idle;

    public float x_min = -10f;
    public float x_max = 10f;
    public float speed = 2f;
    public int blood = 10;

    private Animator animator;
    private bool isMoveRight = false;
    private Vector3 startPosition;
    private Vector3 originalScale;
    private float idleStartTime = 0f;
    public float idleBeforeAttackDuration = 1.0f;

    private float attackRange = 1.0F;
    private Transform playerTransform;
    private float chaseDistance = 5f;
    private bool isAttacking = false;
    private EnermyHealth enermyHealth;
    private bool hasInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
        currentState = State.Idle;
        idleStartTime = Time.time;
        RefreshPlayerReference();
    }

    private void EnsureInitialized()
    {
        if (hasInitialized)
        {
            return;
        }

        animator = GetComponent<Animator>();
        enermyHealth = GetComponent<EnermyHealth>();
        startPosition = transform.position;
        if (transform.localScale.x > 0f)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
        originalScale = transform.localScale;
        hasInitialized = true;
    }

    private void RefreshPlayerReference()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player != null ? player.transform : null;
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
            currentState = State.Idle;
            idleStartTime = Time.time;
            return;
        }

        if (!isAttacking)
        {
            FacePlayer();
        }

        if (distanceToPlayer <= attackRange)
        {
             currentState = State.Idle;
             idleStartTime = Time.time;
             animator.SetTrigger("c_to_idle");
             return; 
        }

        animator.SetBool("iswalking", true);
        transform.Translate((isMoveRight ? 1 : -1) * speed * Time.deltaTime, 0, 0);

    }
    void TurnAround()
    {
        isMoveRight = !isMoveRight;
        if (isMoveRight)
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
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
        animator.SetBool("iswalking", false);
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
        
        if (playerTransform.position.x > transform.position.x)
        {
            isMoveRight = true;
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            isMoveRight = false;
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Player")
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            playerHealth.TakeDamage(1, DamageType.CollideDamage);
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            float knockbackForce = 5f;
            if (playerTransform.position.x < transform.position.x)
            {
                playerController.ApplyKnockback(knockbackForce, Vector2.left);
            }
            else
            {
                playerController.ApplyKnockback(knockbackForce, Vector2.right);
            }
        }
    }

    public void OnSpawnFromPool()
    {
        EnsureInitialized();
        startPosition = transform.position;
        currentState = State.Idle;
        isAttacking = false;
        isMoveRight = false;
        idleStartTime = Time.time;
        blood = enermyHealth != null ? enermyHealth.max_health : blood;
        transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        RefreshPlayerReference();
        if (animator != null)
        {
            animator.SetBool("iswalking", false);
            animator.SetBool("findplayer", false);
            animator.SetBool("Death", false);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void OnDespawnToPool()
    {
        EnsureInitialized();
        currentState = State.Idle;
        isAttacking = false;
        blood = enermyHealth != null ? enermyHealth.max_health : blood;
    }
}

