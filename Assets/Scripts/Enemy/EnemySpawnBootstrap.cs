using System;
using System.Collections.Generic;
using LuaInterface;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemySpawnBootstrap : MonoBehaviour
{
    private const string MonsterTablePath = "MMonsterData";
    private const string MonsterModulePath = "Data.MMonsterData";
    private const string SceneSpawnTablePath = "EnemySpawnSceneData";
    private const string EnemyRootName = "EnemyRoot";

    public static EnemySpawnBootstrap Instance { get; private set; }

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private string spawnedSceneName;
    private Transform currentEnemyRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("EnemySpawnBootstrap");
        bootstrapObject.AddComponent<EnemySpawnBootstrap>();
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

    private void Start()
    {
        TrySpawnForScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        spawnedSceneName = null;
        spawnedEnemies.Clear();
        currentEnemyRoot = null;
        TrySpawnForScene(scene);
    }

    private void TrySpawnForScene(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.name) || spawnedSceneName == scene.name)
        {
            return;
        }

        if (!TryGetLuaState(out LuaState luaState))
        {
            Debug.LogWarning($"[EnemySpawnBootstrap] LuaState is not ready, skip enemy spawn for scene '{scene.name}'.");
            return;
        }

        EnemyPoolManager.EnsureInstance();
        ResourceManager.EnsureInstance();

        LuaTable monsterTable = null;
        LuaTable spawnRootTable = null;
        LuaTable spawnListTable = null;

        try
        {
            if (!TryLoadMonsterTable(luaState, out monsterTable))
            {
                return;
            }

            if (!TryLoadSceneSpawnTable(luaState, scene.name, out spawnRootTable))
            {
                return;
            }

            ValidateSceneConfig(scene.name, spawnRootTable);

            spawnListTable = spawnRootTable.GetTable<LuaTable>("spawns");
            if (spawnListTable == null)
            {
                Debug.LogWarning($"[EnemySpawnBootstrap] Spawn list table is missing in {GetSceneSpawnTableName(scene.name)}.");
                return;
            }

            PrewarmPools(monsterTable, spawnListTable);
            DisableLegacyEnemies(spawnRootTable);
            SpawnConfiguredEnemies(monsterTable, spawnListTable);
            spawnedSceneName = scene.name;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[EnemySpawnBootstrap] Failed to spawn enemies for scene '{scene.name}': {exception}");
        }
        finally
        {
            if (spawnListTable != null)
            {
                spawnListTable.Dispose();
            }

            if (spawnRootTable != null)
            {
                spawnRootTable.Dispose();
            }

            if (monsterTable != null)
            {
                monsterTable.Dispose();
            }
        }
    }

    private bool TryLoadMonsterTable(LuaState luaState, out LuaTable monsterTable)
    {
        monsterTable = null;
        luaState.DoString($"{MonsterTablePath} = require '{MonsterModulePath}'", "EnemySpawnBootstrap_MMonsterData");
        monsterTable = luaState.GetTable(MonsterTablePath, false);
        if (monsterTable != null)
        {
            return true;
        }

        Debug.LogError("[EnemySpawnBootstrap] Failed to load monster template table 'MMonsterData'.");
        return false;
    }

    private bool TryLoadSceneSpawnTable(LuaState luaState, string sceneName, out LuaTable spawnRootTable)
    {
        spawnRootTable = null;
        string sceneSpawnTableName = GetSceneSpawnTableName(sceneName);
        string sceneSpawnModulePath = $"Data.{sceneSpawnTableName}";

        try
        {
            luaState.DoString(
                $"{SceneSpawnTablePath} = require '{sceneSpawnModulePath}'",
                $"EnemySpawnBootstrap_{sceneSpawnTableName}");
        }
        catch (Exception exception)
        {
            Debug.Log($"[EnemySpawnBootstrap] No enemy spawn config found for scene '{sceneName}' at module '{sceneSpawnModulePath}'. Detail: {exception.Message}");
            return false;
        }

        spawnRootTable = luaState.GetTable(SceneSpawnTablePath, false);
        if (spawnRootTable != null)
        {
            return true;
        }

        Debug.LogWarning($"[EnemySpawnBootstrap] Scene spawn table '{sceneSpawnTableName}' loaded but returned nil.");
        return false;
    }

    private void ValidateSceneConfig(string sceneName, LuaTable spawnRootTable)
    {
        string configuredSceneName = Convert.ToString(spawnRootTable["sceneName"]);
        if (!string.IsNullOrEmpty(configuredSceneName) && !string.Equals(configuredSceneName, sceneName, StringComparison.Ordinal))
        {
            Debug.LogWarning($"[EnemySpawnBootstrap] Scene spawn config name mismatch. Active scene='{sceneName}', config sceneName='{configuredSceneName}'.");
        }
    }

    private void PrewarmPools(LuaTable monsterTable, LuaTable spawnListTable)
    {
        HashSet<string> prewarmedMonsterIds = new HashSet<string>();
        for (int i = 1; i <= spawnListTable.Length; i++)
        {
            LuaTable spawnEntry = spawnListTable[i] as LuaTable;
            if (spawnEntry == null)
            {
                continue;
            }

            try
            {
                string monsterId = Convert.ToString(spawnEntry["monsterId"]);
                if (string.IsNullOrEmpty(monsterId) || prewarmedMonsterIds.Contains(monsterId))
                {
                    continue;
                }

                LuaTable monsterConfig = monsterTable[monsterId] as LuaTable;
                if (monsterConfig == null)
                {
                    Debug.LogWarning($"[EnemySpawnBootstrap] Monster config missing for '{monsterId}'.");
                    continue;
                }

                try
                {
                    string prefabPath = Convert.ToString(monsterConfig["prefabPath"]);
                    int prewarmCount = ToInt(monsterConfig["poolPrewarm"], 0);
                    EnemyPoolManager.EnsureInstance().Prewarm(prefabPath, prewarmCount);
                    prewarmedMonsterIds.Add(monsterId);
                }
                finally
                {
                    monsterConfig.Dispose();
                }
            }
            finally
            {
                spawnEntry.Dispose();
            }
        }
    }

    private void SpawnConfiguredEnemies(LuaTable monsterTable, LuaTable spawnListTable)
    {
        for (int i = 1; i <= spawnListTable.Length; i++)
        {
            LuaTable spawnEntry = spawnListTable[i] as LuaTable;
            if (spawnEntry == null)
            {
                continue;
            }

            try
            {
                string monsterId = Convert.ToString(spawnEntry["monsterId"]);
                LuaTable monsterConfig = monsterTable[monsterId] as LuaTable;
                if (monsterConfig == null)
                {
                    Debug.LogWarning($"[EnemySpawnBootstrap] Monster config missing for '{monsterId}'.");
                    continue;
                }

                try
                {
                    GameObject spawnedEnemy = SpawnSingleEnemy(monsterConfig, spawnEntry);
                    if (spawnedEnemy != null)
                    {
                        spawnedEnemies.Add(spawnedEnemy);
                    }
                }
                finally
                {
                    monsterConfig.Dispose();
                }
            }
            finally
            {
                spawnEntry.Dispose();
            }
        }
    }

    private GameObject SpawnSingleEnemy(LuaTable monsterConfig, LuaTable spawnEntry)
    {
        string prefabPath = Convert.ToString(monsterConfig["prefabPath"]);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("[EnemySpawnBootstrap] Spawn skipped: prefabPath is empty.");
            return null;
        }

        LuaTable positionTable = spawnEntry.GetTable<LuaTable>("position");
        LuaTable patrolTable = spawnEntry.GetTable<LuaTable>("patrol");

        try
        {
            Vector3 position = ReadVector3(positionTable);
            bool faceRight = ToBool(spawnEntry["faceRight"], true);
            Transform enemyRoot = GetOrCreateEnemyRoot();

            GameObject enemyObject = EnemyPoolManager.EnsureInstance().Spawn(prefabPath, position, Quaternion.identity, enemyRoot);
            if (enemyObject == null)
            {
                return null;
            }

            ApplyCommonConfig(enemyObject, monsterConfig);
            ApplySpawnOverrides(enemyObject, spawnEntry, patrolTable, faceRight);
            RenameEnemy(enemyObject, spawnEntry);
            return enemyObject;
        }
        finally
        {
            if (patrolTable != null)
            {
                patrolTable.Dispose();
            }

            if (positionTable != null)
            {
                positionTable.Dispose();
            }
        }
    }

    private void ApplyCommonConfig(GameObject enemyObject, LuaTable monsterConfig)
    {
        EnermyHealth health = enemyObject.GetComponent<EnermyHealth>();
        int maxHealth = ToInt(monsterConfig["maxHealth"], health != null ? health.max_health : 0);
        if (health != null)
        {
            health.max_health = maxHealth;
            bool keepCorpseAfterDeath = ToBool(monsterConfig["keepCorpseAfterDeath"], true);
            float corpseDuration = ToFloat(monsterConfig["corpseDuration"], 8f);
            float respawnTime = ToFloat(monsterConfig["respawnTime"], 0f);
            health.ConfigurePoolDeathBehaviour(keepCorpseAfterDeath, corpseDuration, respawnTime);
            health.OnSpawnFromPool();
        }

        crawild crawlid = enemyObject.GetComponent<crawild>();
        if (crawlid != null)
        {
            crawlid.speed = ToFloat(monsterConfig["speed"], crawlid.speed);
            crawlid.blood = ToInt(monsterConfig["blood"], crawlid.blood);
        }

        HuskDandy huskDandy = enemyObject.GetComponent<HuskDandy>();
        if (huskDandy != null)
        {
            huskDandy.speed = ToFloat(monsterConfig["speed"], huskDandy.speed);
            huskDandy.blood = ToInt(monsterConfig["blood"], huskDandy.blood);
            huskDandy.idleBeforeAttackDuration = ToFloat(monsterConfig["idleBeforeAttackDuration"], huskDandy.idleBeforeAttackDuration);
        }

        GreateHusk greatHusk = enemyObject.GetComponent<GreateHusk>();
        if (greatHusk != null)
        {
            greatHusk.speed = ToFloat(monsterConfig["speed"], greatHusk.speed);
            greatHusk.blood = ToInt(monsterConfig["blood"], greatHusk.blood);
            greatHusk.idleBeforeAttackDuration = ToFloat(monsterConfig["idleBeforeAttackDuration"], greatHusk.idleBeforeAttackDuration);
            greatHusk.chaseDistance = ToFloat(monsterConfig["chaseDistance"], greatHusk.chaseDistance);
            greatHusk.attackRange = ToFloat(monsterConfig["attackRange"], greatHusk.attackRange);
            greatHusk.attackCooldown = ToFloat(monsterConfig["attackCooldown"], greatHusk.attackCooldown);
            greatHusk.defenseDuration = ToFloat(monsterConfig["defenseDuration"], greatHusk.defenseDuration);
            greatHusk.defenseCooldown = ToFloat(monsterConfig["defenseCooldown"], greatHusk.defenseCooldown);
        }
    }

    private void ApplySpawnOverrides(GameObject enemyObject, LuaTable spawnEntry, LuaTable patrolTable, bool faceRight)
    {
        float patrolXMin = patrolTable != null ? ToFloat(patrolTable["xMin"], 0f) : 0f;
        float patrolXMax = patrolTable != null ? ToFloat(patrolTable["xMax"], 0f) : 0f;

        crawild crawlid = enemyObject.GetComponent<crawild>();
        if (crawlid != null)
        {
            crawlid.x_min = patrolXMin;
            crawlid.x_max = patrolXMax;
            crawlid.isMoveRight = faceRight;
            crawlid.OnSpawnFromPool();
            return;
        }

        HuskDandy huskDandy = enemyObject.GetComponent<HuskDandy>();
        if (huskDandy != null)
        {
            huskDandy.x_min = patrolXMin;
            huskDandy.x_max = patrolXMax;
            huskDandy.OnSpawnFromPool();
            return;
        }

        GreateHusk greatHusk = enemyObject.GetComponent<GreateHusk>();
        if (greatHusk != null)
        {
            greatHusk.x_min = patrolXMin;
            greatHusk.x_max = patrolXMax;
            greatHusk.isMoveRight = faceRight;
            greatHusk.OnSpawnFromPool();
        }
    }

    private void RenameEnemy(GameObject enemyObject, LuaTable spawnEntry)
    {
        string spawnId = Convert.ToString(spawnEntry["spawnId"]);
        if (!string.IsNullOrEmpty(spawnId))
        {
            enemyObject.name = spawnId;
            PooledEnemy pooledEnemy = enemyObject.GetComponent<PooledEnemy>();
            if (pooledEnemy != null)
            {
                pooledEnemy.SetSpawnId(spawnId);
            }
        }
    }

    private void DisableLegacyEnemies(LuaTable spawnRootTable)
    {
        LuaTable legacyEnemiesTable = spawnRootTable.GetTable<LuaTable>("legacyEnemies");
        if (legacyEnemiesTable == null)
        {
            return;
        }

        try
        {
            for (int i = 1; i <= legacyEnemiesTable.Length; i++)
            {
                string legacyEnemyName = Convert.ToString(legacyEnemiesTable[i]);
                if (!string.IsNullOrEmpty(legacyEnemyName))
                {
                    DisableLegacyEnemyByName(legacyEnemyName);
                }
            }
        }
        finally
        {
            legacyEnemiesTable.Dispose();
        }
    }

    private static string GetSceneSpawnTableName(string sceneName)
    {
        return $"M{sceneName}MonsterSpawnData";
    }

    private Transform GetOrCreateEnemyRoot()
    {
        if (currentEnemyRoot != null)
        {
            return currentEnemyRoot;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject rootObject = rootObjects[i];
            if (rootObject != null && rootObject.name == EnemyRootName)
            {
                currentEnemyRoot = rootObject.transform;
                return currentEnemyRoot;
            }
        }

        GameObject enemyRootObject = new GameObject(EnemyRootName);
        SceneManager.MoveGameObjectToScene(enemyRootObject, activeScene);
        currentEnemyRoot = enemyRootObject.transform;
        return currentEnemyRoot;
    }

    private void DisableLegacyEnemyByName(string enemyName)
    {
        GameObject legacyEnemy = GameObject.Find(enemyName);
        if (legacyEnemy != null)
        {
            legacyEnemy.SetActive(false);
        }
    }

    private bool TryGetLuaState(out LuaState luaState)
    {
        luaState = LuaClient.Instance != null ? LuaClient.GetMainState() : null;
        return luaState != null;
    }

    private static Vector3 ReadVector3(LuaTable positionTable)
    {
        if (positionTable == null)
        {
            return Vector3.zero;
        }

        return new Vector3(
            ToFloat(positionTable["x"], 0f),
            ToFloat(positionTable["y"], 0f),
            ToFloat(positionTable["z"], 0f));
    }

    private static int ToInt(object value, int defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static float ToFloat(object value, float defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }

        try
        {
            return Convert.ToSingle(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static bool ToBool(object value, bool defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        try
        {
            return Convert.ToInt32(value) != 0;
        }
        catch
        {
            return defaultValue;
        }
    }
}
