using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class crawild : MonoBehaviour, IPoolableEnemy
{
    [Header("Movement Settings")]
    public float x_min = -10f;
    public float x_max = 10f;
    public float speed = 2f;

    [Header("Animation Settings")]
    public string walkAnimation = "Crawild";
    public string turnAnimation = "Turn";
    public string deathAnimation = "Die";
    [Header("Enemy Status")]
    public int blood = 5;
    public string backgroundMusic = "Enemy/crawler";

    private Animator animator;
    private Vector3 startPosition;
    public bool isMoveRight = true;
    private bool isTurning = false;
    private Vector3 originalScale;
    private EnermyHealth enermyHealth;
    private bool hasInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
        startPosition = transform.position;
    }

    private void EnsureInitialized()
    {
        if (hasInitialized)
        {
            return;
        }

        animator = GetComponent<Animator>();
        enermyHealth = GetComponent<EnermyHealth>();
        originalScale = transform.localScale;
        startPosition = transform.position;
        hasInitialized = true;
    }

    void Update()
    {
        if (blood==0)
        {
            animator.SetBool("Die",true);
            return;
        }
        if (!isTurning)
        {
            Move();
        }
    }

    void Move()
    {
        if (IsOutOfBounds())
        {
            StartCoroutine(TurnAround());
            return;
        }

        float direction = isMoveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

    }

    bool IsOutOfBounds()
    {
        float currentX = transform.position.x;
        float leftBound = startPosition.x + x_min;
        float rightBound = startPosition.x + x_max;
        return currentX >= rightBound || currentX <= leftBound;
    }

    IEnumerator TurnAround()
    {
        isTurning = true;

        animator.SetBool("turn",true);

        yield return new WaitForSeconds(0.5f);

        isMoveRight = !isMoveRight;
        FlipCharacter();

        float direction = isMoveRight ? 0.1f : -0.1f;
        transform.Translate(Vector3.right * direction);

        isTurning = false;

    }

    void FlipCharacter()
    {
        if (isMoveRight)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    float GetCenterPosition()
    {
        return startPosition.x + (x_min + x_max) / 2f;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            new Vector3(currentStart.x + x_min, currentStart.y - 0.5f, currentStart.z),
            new Vector3(currentStart.x + x_max, currentStart.y - 0.5f, currentStart.z)
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_min, currentStart.y, currentStart.z), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(currentStart.x + x_max, currentStart.y, currentStart.z), 0.3f);
    }
    public void TakeDamage(int damage)
    {
        blood -= damage;
        if (blood < 0) blood = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Player")
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            playerHealth.TakeDamage(1, DamageType.CollideDamage);
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            float knockbackForce = 5f;
            if (playerController.transform.position.x < transform.position.x)
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
        isTurning = false;
        isMoveRight = true;
        blood = enermyHealth != null ? enermyHealth.max_health : blood;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        if (animator != null)
        {
            animator.SetBool("Die", false);
            animator.SetBool("turn", false);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void OnDespawnToPool()
    {
        EnsureInitialized();
        isTurning = false;
        blood = enermyHealth != null ? enermyHealth.max_health : blood;
    }
}