using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    private ProjectilePoolManager owner;
    private int poolKey;
    private bool cacheInitialized;
    private MonoBehaviour[] cachedBehaviours;
    private bool[] cachedBehaviourEnabledStates;
    private Collider2D[] cachedColliders;
    private bool[] cachedColliderEnabledStates;
    private Rigidbody2D[] cachedRigidbodies;
    private bool[] cachedRigidbodySimulatedStates;
    private Animator[] cachedAnimators;
    private TrailRenderer[] cachedTrailRenderers;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRotation;

    public int PoolKey => poolKey;
    public bool IsManagedByPool => owner != null;
    public bool IsReturningToPool { get; private set; }

    private void Awake()
    {
        CacheOriginalStates();
    }

    internal void Bind(ProjectilePoolManager manager, int key)
    {
        owner = manager;
        poolKey = key;
        CacheOriginalStates();
    }

    internal void NotifyAfterSpawn()
    {
        CacheOriginalStates();
        RestoreCommonState();
        SendMessage("OnSpawnFromPool", SendMessageOptions.DontRequireReceiver);
    }

    internal void NotifyBeforeReturn()
    {
        CacheOriginalStates();
        SendMessage("OnDespawnToPool", SendMessageOptions.DontRequireReceiver);
        ResetPhysicsState();
    }

    internal void SetReturningToPool(bool value)
    {
        IsReturningToPool = value;
    }

    public void ReturnToPool()
    {
        if (owner != null)
        {
            owner.ReturnToPool(this);
            return;
        }

        Destroy(gameObject);
    }

    private void CacheOriginalStates()
    {
        if (cacheInitialized)
        {
            return;
        }

        cachedBehaviours = GetComponents<MonoBehaviour>();
        cachedBehaviourEnabledStates = new bool[cachedBehaviours.Length];
        for (int i = 0; i < cachedBehaviours.Length; i++)
        {
            cachedBehaviourEnabledStates[i] = cachedBehaviours[i] != null && cachedBehaviours[i].enabled;
        }

        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        cachedColliderEnabledStates = new bool[cachedColliders.Length];
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            cachedColliderEnabledStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
        }

        cachedRigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
        cachedRigidbodySimulatedStates = new bool[cachedRigidbodies.Length];
        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            cachedRigidbodySimulatedStates[i] = cachedRigidbodies[i] != null && cachedRigidbodies[i].simulated;
        }

        cachedAnimators = GetComponentsInChildren<Animator>(true);
        cachedTrailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        originalLocalScale = transform.localScale;
        originalLocalRotation = transform.localRotation;
        cacheInitialized = true;
    }

    private void RestoreCommonState()
    {
        transform.localScale = originalLocalScale;
        transform.localRotation = originalLocalRotation;

        for (int i = 0; i < cachedBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = cachedBehaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = cachedBehaviourEnabledStates[i];
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            Collider2D collider2D = cachedColliders[i];
            if (collider2D == null)
            {
                continue;
            }

            collider2D.enabled = cachedColliderEnabledStates[i];
        }

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            Rigidbody2D rigidbody2D = cachedRigidbodies[i];
            if (rigidbody2D == null)
            {
                continue;
            }

            rigidbody2D.velocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
            rigidbody2D.simulated = cachedRigidbodySimulatedStates[i];
        }

        for (int i = 0; i < cachedAnimators.Length; i++)
        {
            Animator animator = cachedAnimators[i];
            if (animator == null)
            {
                continue;
            }

            animator.Rebind();
            animator.Update(0f);
        }

        for (int i = 0; i < cachedTrailRenderers.Length; i++)
        {
            TrailRenderer trailRenderer = cachedTrailRenderers[i];
            if (trailRenderer == null)
            {
                continue;
            }

            trailRenderer.Clear();
        }
    }

    private void ResetPhysicsState()
    {
        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            Rigidbody2D rigidbody2D = cachedRigidbodies[i];
            if (rigidbody2D == null)
            {
                continue;
            }

            rigidbody2D.velocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }
    }
}
