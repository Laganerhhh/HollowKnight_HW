using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance { get; private set; }
    public static bool IsShuttingDown { get; private set; }

    private readonly Dictionary<int, Queue<PooledEffect>> inactivePools = new Dictionary<int, Queue<PooledEffect>>();
    private readonly HashSet<PooledEffect> activeEffects = new HashSet<PooledEffect>();
    private readonly Dictionary<int, GameObject> prefabLookup = new Dictionary<int, GameObject>();

    internal bool IsClearing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        IsShuttingDown = false;
        EnsureInstance();
    }

    public static EffectPoolManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (IsShuttingDown)
        {
            return null;
        }

        GameObject managerObject = new GameObject("EffectPoolManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<EffectPoolManager>();
    }

    public static GameObject Play(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (effectPrefab == null || IsShuttingDown)
        {
            return null;
        }

        EffectPoolManager manager = EnsureInstance();
        return manager != null ? manager.Spawn(effectPrefab, position, rotation, parent) : null;
    }

    public static GameObject Play(GameObject effectPrefab, Transform parent)
    {
        Vector3 position = parent != null ? parent.position : Vector3.zero;
        Quaternion rotation = parent != null ? parent.rotation : Quaternion.identity;
        return Play(effectPrefab, position, rotation, parent);
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

    private GameObject Spawn(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        int poolKey = effectPrefab.GetInstanceID();
        prefabLookup[poolKey] = effectPrefab;

        PooledEffect pooledEffect = Acquire(effectPrefab, poolKey);
        if (pooledEffect == null)
        {
            return null;
        }

        Transform effectTransform = pooledEffect.transform;
        effectTransform.SetParent(parent, false);
        effectTransform.position = position;
        effectTransform.rotation = rotation;

        activeEffects.Add(pooledEffect);
        pooledEffect.gameObject.SetActive(true);
        pooledEffect.NotifyAfterSpawn();
        return pooledEffect.gameObject;
    }

    internal void ReturnToPool(PooledEffect pooledEffect)
    {
        if (pooledEffect == null)
        {
            return;
        }

        if (!pooledEffect.IsManagedByPool || IsClearing || IsShuttingDown)
        {
            Destroy(pooledEffect.gameObject);
            return;
        }

        if (!activeEffects.Remove(pooledEffect) && GetQueue(pooledEffect.PoolKey).Contains(pooledEffect))
        {
            return;
        }

        pooledEffect.NotifyBeforeReturn();
        pooledEffect.SetReturningToPool(true);
        pooledEffect.transform.SetParent(transform, false);
        pooledEffect.gameObject.SetActive(false);
        pooledEffect.SetReturningToPool(false);
        GetQueue(pooledEffect.PoolKey).Enqueue(pooledEffect);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAll();
    }

    public void ClearAll()
    {
        IsClearing = true;

        foreach (PooledEffect pooledEffect in new List<PooledEffect>(activeEffects))
        {
            if (pooledEffect != null)
            {
                Destroy(pooledEffect.gameObject);
            }
        }

        activeEffects.Clear();

        foreach (KeyValuePair<int, Queue<PooledEffect>> pair in inactivePools)
        {
            Queue<PooledEffect> queue = pair.Value;
            while (queue.Count > 0)
            {
                PooledEffect pooledEffect = queue.Dequeue();
                if (pooledEffect != null)
                {
                    Destroy(pooledEffect.gameObject);
                }
            }
        }

        inactivePools.Clear();
        prefabLookup.Clear();
        IsClearing = false;
    }

    private PooledEffect Acquire(GameObject effectPrefab, int poolKey)
    {
        Queue<PooledEffect> queue = GetQueue(poolKey);
        while (queue.Count > 0)
        {
            PooledEffect pooledEffect = queue.Dequeue();
            if (pooledEffect != null)
            {
                return pooledEffect;
            }
        }

        return CreateNew(effectPrefab, poolKey);
    }

    private PooledEffect CreateNew(GameObject effectPrefab, int poolKey)
    {
        GameObject instance = Instantiate(effectPrefab, transform, false);
        if (instance == null)
        {
            return null;
        }

        PooledEffect pooledEffect = instance.GetComponent<PooledEffect>();
        if (pooledEffect == null)
        {
            pooledEffect = instance.AddComponent<PooledEffect>();
        }

        pooledEffect.Bind(this, poolKey);
        return pooledEffect;
    }

    private Queue<PooledEffect> GetQueue(int poolKey)
    {
        if (!inactivePools.TryGetValue(poolKey, out Queue<PooledEffect> queue))
        {
            queue = new Queue<PooledEffect>();
            inactivePools[poolKey] = queue;
        }

        return queue;
    }
}