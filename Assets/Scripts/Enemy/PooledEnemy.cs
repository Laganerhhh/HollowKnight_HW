using UnityEngine;
using UnityEngine.SceneManagement;

public class PooledEnemy : MonoBehaviour
{
    private EnemyPoolManager owner;
    private string poolKey;
    private string sourcePrefabPath;
    private string spawnId;

    private bool cacheInitialized;
    private MonoBehaviour[] cachedBehaviours;
    private bool[] cachedBehaviourEnabledStates;
    private Collider2D[] cachedColliders;
    private bool[] cachedColliderEnabledStates;
    private Rigidbody2D[] cachedRigidbodies;
    private bool[] cachedRigidbodySimulatedStates;
    private Animator[] cachedAnimators;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRotation;
    private Vector3 lastSpawnPosition;
    private Quaternion lastSpawnRotation;
    private Transform lastSpawnParent;
    private string lastSpawnSceneName;
    private float pendingRespawnDelay = -1f;

    public string PoolKey => poolKey;
    public string SourcePrefabPath => sourcePrefabPath;
    public bool IsManagedByPool => owner != null && !string.IsNullOrEmpty(poolKey);

    private void Awake()
    {
        CacheOriginalStates();
    }

    internal void Bind(EnemyPoolManager manager, string key, string prefabPath)
    {
        owner = manager;
        poolKey = key;
        sourcePrefabPath = prefabPath;
        CacheOriginalStates();
    }

    internal void RecordSpawnContext(Vector3 position, Quaternion rotation, Transform parent)
    {
        lastSpawnPosition = position;
        lastSpawnRotation = rotation;
        lastSpawnParent = parent;
        lastSpawnSceneName = gameObject.scene.name;
    }

    public void SetSpawnId(string newSpawnId)
    {
        spawnId = newSpawnId;
    }

    public void ScheduleRespawn(float delay)
    {
        pendingRespawnDelay = delay;
    }

    public void ClearScheduledRespawn()
    {
        pendingRespawnDelay = -1f;
    }

    internal void NotifyAfterSpawn()
    {
        CacheOriginalStates();
        ClearScheduledRespawn();
        RestoreCommonState();
        NotifyPoolableComponents(true);
    }

    internal void NotifyBeforeDespawn()
    {
        CacheOriginalStates();
        if (pendingRespawnDelay > 0f && owner != null && !string.IsNullOrEmpty(sourcePrefabPath))
        {
            owner.ScheduleRespawn(sourcePrefabPath, lastSpawnPosition, lastSpawnRotation, lastSpawnParent, spawnId, lastSpawnSceneName, pendingRespawnDelay);
        }

        NotifyPoolableComponents(false);
        ResetPhysicsState();
        ClearScheduledRespawn();
    }

    public void ReturnToPool()
    {
        if (owner != null)
        {
            owner.Despawn(this);
            return;
        }

        if (!string.IsNullOrEmpty(sourcePrefabPath))
        {
            ResourceManager.EnsureInstance().ReleaseInstance(gameObject);
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

    private void NotifyPoolableComponents(bool isSpawn)
    {
        if (isSpawn)
        {
            SendMessage("OnSpawnFromPool", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            SendMessage("OnDespawnToPool", SendMessageOptions.DontRequireReceiver);
        }
    }
}
