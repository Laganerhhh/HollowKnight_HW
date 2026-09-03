using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance { get; private set; }

    private readonly Dictionary<string, Queue<PooledEnemy>> inactivePools = new Dictionary<string, Queue<PooledEnemy>>();
    private readonly HashSet<PooledEnemy> activeEnemies = new HashSet<PooledEnemy>();
    private readonly Dictionary<string, Coroutine> pendingRespawns = new Dictionary<string, Coroutine>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static EnemyPoolManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("EnemyPoolManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<EnemyPoolManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public GameObject Spawn(string prefabPath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrWhiteSpace(prefabPath))
        {
            Debug.LogWarning("[EnemyPoolManager] Spawn failed: prefabPath is empty.");
            return null;
        }

        PooledEnemy pooledEnemy = Acquire(prefabPath);
        if (pooledEnemy == null)
        {
            Debug.LogError($"[EnemyPoolManager] Spawn failed: could not acquire enemy for '{prefabPath}'.");
            return null;
        }

        Transform enemyTransform = pooledEnemy.transform;
        enemyTransform.SetParent(parent, false);
        enemyTransform.position = position;
        enemyTransform.rotation = rotation;
        pooledEnemy.RecordSpawnContext(position, rotation, parent);
        pooledEnemy.gameObject.SetActive(true);
        pooledEnemy.NotifyAfterSpawn();
        activeEnemies.Add(pooledEnemy);
        return pooledEnemy.gameObject;
    }

    public void Prewarm(string prefabPath, int count)
    {
        if (count <= 0 || string.IsNullOrWhiteSpace(prefabPath))
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            PooledEnemy pooledEnemy = CreateNew(prefabPath);
            if (pooledEnemy == null)
            {
                return;
            }

            Despawn(pooledEnemy);
        }
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PooledEnemy pooledEnemy = instance.GetComponent<PooledEnemy>();
        if (pooledEnemy == null || !pooledEnemy.IsManagedByPool)
        {
            ResourceManager.EnsureInstance().ReleaseInstance(instance);
            return;
        }

        Despawn(pooledEnemy);
    }

    internal void Despawn(PooledEnemy pooledEnemy)
    {
        if (pooledEnemy == null)
        {
            return;
        }

        activeEnemies.Remove(pooledEnemy);
        pooledEnemy.NotifyBeforeDespawn();
        pooledEnemy.transform.SetParent(transform, false);
        pooledEnemy.gameObject.SetActive(false);
        GetQueue(pooledEnemy.PoolKey).Enqueue(pooledEnemy);
    }

    public void ClearAll()
    {
        CancelAllPendingRespawns();

        foreach (PooledEnemy pooledEnemy in new List<PooledEnemy>(activeEnemies))
        {
            if (pooledEnemy == null)
            {
                continue;
            }

            ReleaseInstance(pooledEnemy.gameObject, pooledEnemy.SourcePrefabPath);
        }

        activeEnemies.Clear();

        foreach (KeyValuePair<string, Queue<PooledEnemy>> pair in inactivePools)
        {
            Queue<PooledEnemy> queue = pair.Value;
            while (queue.Count > 0)
            {
                PooledEnemy pooledEnemy = queue.Dequeue();
                if (pooledEnemy == null)
                {
                    continue;
                }

                ReleaseInstance(pooledEnemy.gameObject, pooledEnemy.SourcePrefabPath);
            }
        }

        inactivePools.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAll();
    }

    private PooledEnemy Acquire(string prefabPath)
    {
        Queue<PooledEnemy> queue = GetQueue(prefabPath);
        while (queue.Count > 0)
        {
            PooledEnemy pooledEnemy = queue.Dequeue();
            if (pooledEnemy != null)
            {
                return pooledEnemy;
            }
        }

        return CreateNew(prefabPath);
    }

    private PooledEnemy CreateNew(string prefabPath)
    {
        GameObject instance = ResourceManager.EnsureInstance().InstantiatePrefab(prefabPath, transform);
        if (instance == null)
        {
            return null;
        }

        PooledEnemy pooledEnemy = instance.GetComponent<PooledEnemy>();
        if (pooledEnemy == null)
        {
            pooledEnemy = instance.AddComponent<PooledEnemy>();
        }

        pooledEnemy.Bind(this, prefabPath, prefabPath);
        return pooledEnemy;
    }

    private Queue<PooledEnemy> GetQueue(string prefabPath)
    {
        if (!inactivePools.TryGetValue(prefabPath, out Queue<PooledEnemy> queue))
        {
            queue = new Queue<PooledEnemy>();
            inactivePools[prefabPath] = queue;
        }

        return queue;
    }

    private void ReleaseInstance(GameObject instance, string prefabPath)
    {
        if (instance == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(prefabPath))
        {
            ResourceManager.EnsureInstance().ReleaseInstance(instance);
            return;
        }

        Destroy(instance);
    }

    internal void ScheduleRespawn(string prefabPath, Vector3 position, Quaternion rotation, Transform parent, string spawnId, string sceneName, float delay)
    {
        if (delay <= 0f || string.IsNullOrWhiteSpace(prefabPath) || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        string respawnKey = BuildRespawnKey(sceneName, spawnId, prefabPath, position);
        CancelPendingRespawn(respawnKey);
        pendingRespawns[respawnKey] = StartCoroutine(RespawnAfterDelay(respawnKey, prefabPath, position, rotation, parent, spawnId, sceneName, delay));
    }

    private IEnumerator RespawnAfterDelay(string respawnKey, string prefabPath, Vector3 position, Quaternion rotation, Transform parent, string spawnId, string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);

        pendingRespawns.Remove(respawnKey);
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            yield break;
        }

        Transform validParent = parent;
        if (validParent == null)
        {
            GameObject parentObject = GameObject.Find("EnemyRoot");
            validParent = parentObject != null ? parentObject.transform : null;
        }

        GameObject respawnedEnemy = Spawn(prefabPath, position, rotation, validParent);
        if (respawnedEnemy != null && !string.IsNullOrEmpty(spawnId))
        {
            respawnedEnemy.name = spawnId;
            PooledEnemy pooledEnemy = respawnedEnemy.GetComponent<PooledEnemy>();
            if (pooledEnemy != null)
            {
                pooledEnemy.SetSpawnId(spawnId);
            }
        }
    }

    private static string BuildRespawnKey(string sceneName, string spawnId, string prefabPath, Vector3 position)
    {
        if (!string.IsNullOrEmpty(spawnId))
        {
            return sceneName + "::" + spawnId;
        }

        return sceneName + "::" + prefabPath + "::" + position.x + "::" + position.y + "::" + position.z;
    }

    private void CancelPendingRespawn(string respawnKey)
    {
        if (string.IsNullOrEmpty(respawnKey))
        {
            return;
        }

        if (pendingRespawns.TryGetValue(respawnKey, out Coroutine pendingCoroutine))
        {
            if (pendingCoroutine != null)
            {
                StopCoroutine(pendingCoroutine);
            }

            pendingRespawns.Remove(respawnKey);
        }
    }

    private void CancelAllPendingRespawns()
    {
        foreach (KeyValuePair<string, Coroutine> pair in pendingRespawns)
        {
            if (pair.Value != null)
            {
                StopCoroutine(pair.Value);
            }
        }

        pendingRespawns.Clear();
    }
}
