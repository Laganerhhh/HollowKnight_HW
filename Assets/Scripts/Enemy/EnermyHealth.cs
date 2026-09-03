using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnermyHealth : MonoBehaviour
{
    [SerializeField] private int current_health = 2;
    public int max_health = 2;

    [SerializeField] private string dieAnimParam = "Die";
    [SerializeField] private bool keepCorpseAfterDeath = true;
    [SerializeField] private float corpseDurationForPool = 8f;
    [SerializeField] private float respawnTimeForPool = 0f;
    private Animator animator;

    private SpriteRenderer spriteRenderer;
    private Component pooledEnemyComponent;
    private Rigidbody2D cachedRigidbody;
    private bool cachedRigidbodySimulated;
    private Coroutine delayedReturnCoroutine;

    [SerializeField] private GameObject injury_effect;
    [SerializeField] private GameObject hit_particle_effect;
    [SerializeField] private GameObject death_effect;

    [SerializeField] private GameObject defense_particle;

    [SerializeField] private bool isFalseknight = false;

    public bool isDefensing = false;

    private FalseKnight falseKnight = null;

    private void Awake()
    {
        EnsureReferences();
    }

    void Start()
    {
        current_health = max_health;
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (pooledEnemyComponent == null)
        {
            pooledEnemyComponent = GetComponent("PooledEnemy");
        }

        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody2D>();
            if (cachedRigidbody != null)
            {
                cachedRigidbodySimulated = cachedRigidbody.simulated;
            }
        }

        if (isFalseknight && falseKnight == null)
        {
            falseKnight = GetComponentInParent<FalseKnight>();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isFalseknight)
        {
            FalseKnightTakeDamege(damage);
            return;
        }

        if (isDefensing)
        {
            DefenseDamege();
            return;
        }

        current_health -= damage;
        if (injury_effect != null)
        {
            PlayEffect(injury_effect, transform.position, Quaternion.identity, transform, null);
            PlayEffect(hit_particle_effect, transform.position - transform.up * 0.5f, Quaternion.identity, null, null);
        }
        SoundManager.instance.PlaySound(SoundIndex.enermy_damage);
        if (current_health <= 0)
        {
            current_health = 0;
            Die();
            return;
        }
    }

    private void DefenseDamege()
    {
        SoundManager.instance.PlaySound(SoundIndex.player_hitRecoil);
        PlayEffect(defense_particle, transform.position + new Vector3(0.5f, 0, 0), Quaternion.identity, transform, null);
    }

    private void FalseKnightTakeDamege(int damage)
    {
        if (injury_effect != null)
        {
            PlayEffect(injury_effect, transform.position + transform.up * 0.5f, Quaternion.identity, transform, null);
            PlayEffect(hit_particle_effect, transform.position + transform.up * 0.5f, Quaternion.identity, null, null);
        }
        falseKnight.TakeDamage(damage);
    }

    void Die()
    {
        PlayEffect(death_effect, transform.position - transform.up * 0.5f, Quaternion.identity, null, null);

        animator.SetTrigger(dieAnimParam);
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.velocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = false;
        }

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }
    }

    void OnDieAnimEnd()
    {
        EnsureReferences();

        if (pooledEnemyComponent != null)
        {
            if (keepCorpseAfterDeath && corpseDurationForPool > 0f)
            {
                if (delayedReturnCoroutine != null)
                {
                    StopCoroutine(delayedReturnCoroutine);
                }

                delayedReturnCoroutine = StartCoroutine(ReturnToPoolAfterDelay(corpseDurationForPool));
                return;
            }

            ScheduleRespawnIfNeeded();
            pooledEnemyComponent.SendMessage("ReturnToPool", SendMessageOptions.DontRequireReceiver);
            return;
        }

        DestroySelf();
    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        delayedReturnCoroutine = null;

        EnsureReferences();
        ScheduleRespawnIfNeeded();
        if (pooledEnemyComponent != null)
        {
            pooledEnemyComponent.SendMessage("ReturnToPool", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void ScheduleRespawnIfNeeded()
    {
        PooledEnemy pooledEnemy = pooledEnemyComponent as PooledEnemy;
        if (pooledEnemy == null)
        {
            return;
        }

        pooledEnemy.ClearScheduledRespawn();
        if (respawnTimeForPool <= 0f)
        {
            return;
        }

        if (respawnTimeForPool <= corpseDurationForPool)
        {
            Debug.LogWarning($"[EnermyHealth] Respawn time must be greater than corpse duration on '{gameObject.name}'. Current respawnTime={respawnTimeForPool}, corpseDuration={corpseDurationForPool}.");
            return;
        }

        float remainingDelay = keepCorpseAfterDeath ? respawnTimeForPool - Mathf.Max(corpseDurationForPool, 0f) : respawnTimeForPool;
        if (remainingDelay <= 0f)
        {
            Debug.LogWarning($"[EnermyHealth] Respawn delay is invalid on '{gameObject.name}'.");
            return;
        }

        pooledEnemy.ScheduleRespawn(remainingDelay);
    }

    private void DestroySelf()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Destroy(rb);
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this && script.GetType() != typeof(SpriteRenderer))
            {
                Destroy(script);
            }
        }
    }

    public void OnSpawnFromPool()
    {
        EnsureReferences();
        if (delayedReturnCoroutine != null)
        {
            StopCoroutine(delayedReturnCoroutine);
            delayedReturnCoroutine = null;
        }

        if (pooledEnemyComponent is PooledEnemy pooledEnemy)
        {
            pooledEnemy.ClearScheduledRespawn();
        }

        current_health = max_health;
        isDefensing = false;
        if (cachedRigidbody != null)
        {
            cachedRigidbody.velocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = cachedRigidbodySimulated;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void OnDespawnToPool()
    {
        if (delayedReturnCoroutine != null)
        {
            StopCoroutine(delayedReturnCoroutine);
            delayedReturnCoroutine = null;
        }

        current_health = max_health;
        isDefensing = false;
    }

    public void ConfigurePoolDeathBehaviour(bool shouldKeepCorpseAfterDeath, float corpseDuration, float respawnTime)
    {
        keepCorpseAfterDeath = shouldKeepCorpseAfterDeath;
        corpseDurationForPool = corpseDuration;
        respawnTimeForPool = respawnTime;
    }

    private GameObject PlayEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent, Vector3? localScale)
    {
        if (effectPrefab == null)
        {
            return null;
        }

        GameObject effectInstance = TryPlayEffectFromPool(effectPrefab, position, rotation, parent);
        if (effectInstance == null)
        {
            effectInstance = Instantiate(effectPrefab, position, rotation, parent);
        }

        if (effectInstance != null && localScale.HasValue)
        {
            effectInstance.transform.localScale = localScale.Value;
        }

        return effectInstance;
    }

    private static GameObject TryPlayEffectFromPool(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        return EffectPoolManager.Play(effectPrefab, position, rotation, parent);
    }
}
