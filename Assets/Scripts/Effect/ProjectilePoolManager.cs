using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }
    public static bool IsShuttingDown { get; private set; }

    private readonly Dictionary<int, Queue<PooledProjectile>> inactivePools = new Dictionary<int, Queue<PooledProjectile>>();
    private readonly HashSet<PooledProjectile> activeProjectiles = new HashSet<PooledProjectile>();

    internal bool IsClearing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        IsShuttingDown = false;
        EnsureInstance();
    }

    public static ProjectilePoolManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (IsShuttingDown)
        {
            return null;
        }

        GameObject managerObject = new GameObject("ProjectilePoolManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<ProjectilePoolManager>();
    }

    public static GameObject Spawn(GameObject projectilePrefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (projectilePrefab == null || IsShuttingDown)
        {
            return null;
        }

        ProjectilePoolManager manager = EnsureInstance();
        return manager != null ? manager.SpawnInternal(projectilePrefab, position, rotation, parent) : null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsShuttingDown = false;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        IsShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
            IsShuttingDown = true;
        }
    }

    private GameObject SpawnInternal(GameObject projectilePrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        int poolKey = projectilePrefab.GetInstanceID();
        PooledProjectile pooledProjectile = Acquire(projectilePrefab, poolKey);
        if (pooledProjectile == null)
        {
            return null;
        }

        Transform projectileTransform = pooledProjectile.transform;
        projectileTransform.SetParent(parent, false);
        projectileTransform.position = position;
        projectileTransform.rotation = rotation;

        activeProjectiles.Add(pooledProjectile);
        pooledProjectile.gameObject.SetActive(true);
        pooledProjectile.NotifyAfterSpawn();
        return pooledProjectile.gameObject;
    }

    internal void ReturnToPool(PooledProjectile pooledProjectile)
    {
        if (pooledProjectile == null)
        {
            return;
        }

        if (!pooledProjectile.IsManagedByPool || IsClearing || IsShuttingDown)
        {
            Destroy(pooledProjectile.gameObject);
            return;
        }

        if (!activeProjectiles.Remove(pooledProjectile) && GetQueue(pooledProjectile.PoolKey).Contains(pooledProjectile))
        {
            return;
        }

        pooledProjectile.NotifyBeforeReturn();
        pooledProjectile.SetReturningToPool(true);
        pooledProjectile.transform.SetParent(transform, false);
        pooledProjectile.gameObject.SetActive(false);
        pooledProjectile.SetReturningToPool(false);
        GetQueue(pooledProjectile.PoolKey).Enqueue(pooledProjectile);
    }

    public void ClearAll()
    {
        IsClearing = true;

        foreach (PooledProjectile pooledProjectile in new List<PooledProjectile>(activeProjectiles))
        {
            if (pooledProjectile != null)
            {
                Destroy(pooledProjectile.gameObject);
            }
        }

        activeProjectiles.Clear();

        foreach (KeyValuePair<int, Queue<PooledProjectile>> pair in inactivePools)
        {
            Queue<PooledProjectile> queue = pair.Value;
            while (queue.Count > 0)
            {
                PooledProjectile pooledProjectile = queue.Dequeue();
                if (pooledProjectile != null)
                {
                    Destroy(pooledProjectile.gameObject);
                }
            }
        }

        inactivePools.Clear();
        IsClearing = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAll();
    }

    private PooledProjectile Acquire(GameObject projectilePrefab, int poolKey)
    {
        Queue<PooledProjectile> queue = GetQueue(poolKey);
        while (queue.Count > 0)
        {
            PooledProjectile pooledProjectile = queue.Dequeue();
            if (pooledProjectile != null)
            {
                return pooledProjectile;
            }
        }

        return CreateNew(projectilePrefab, poolKey);
    }

    private PooledProjectile CreateNew(GameObject projectilePrefab, int poolKey)
    {
        GameObject instance = Instantiate(projectilePrefab, transform, false);
        if (instance == null)
        {
            return null;
        }

        PooledProjectile pooledProjectile = instance.GetComponent<PooledProjectile>();
        if (pooledProjectile == null)
        {
            pooledProjectile = instance.AddComponent<PooledProjectile>();
        }

        pooledProjectile.Bind(this, poolKey);
        return pooledProjectile;
    }

    private Queue<PooledProjectile> GetQueue(int poolKey)
    {
        if (!inactivePools.TryGetValue(poolKey, out Queue<PooledProjectile> queue))
        {
            queue = new Queue<PooledProjectile>();
            inactivePools[poolKey] = queue;
        }

        return queue;
    }
}
