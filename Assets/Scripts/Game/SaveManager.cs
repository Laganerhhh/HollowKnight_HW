using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public const int MaxSlotCount = 10;

    private const int CurrentVersion = 1;
    private const int FirstLevelSceneIndex = 1;
    private const string SaveFolderName = "Save";
    private const string SaveFilePrefix = "save_";
    private const string SaveFileExtension = ".json";
    private const string LastSlotIdKey = "Save_LastSlotId";

    public static SaveManager Instance { get; private set; }

    private int currentSlotId;
    private SaveData pendingLoadData;

    [Serializable]
    private class SaveData
    {
        public int version;
        public int slotId;
        public string sceneName;
        public string locationName;
        public string iconName;
        public float playerX;
        public float playerY;
        public float playerZ;
        public float respawnX;
        public float respawnY;
        public float respawnZ;
        public int currentHealth;
        public int maxHealth;
        public float currentSoulPower;
        public float maxSoulPower;
        public float playTimeSeconds;
        public string saveTime;
    }

    public static SaveManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("SaveManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<SaveManager>();
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
    }

    public static int GetMaxSlotCount()
    {
        return MaxSlotCount;
    }

    public static void SaveCurrentGameAuto()
    {
        SaveManager manager = EnsureInstance();
        manager.SaveCurrentGame(manager.GetAutoSaveSlotId());
    }

    public static void SaveCurrentGameToCurrentSlot()
    {
        SaveManager manager = EnsureInstance();
        int slotId = manager.currentSlotId > 0 ? manager.currentSlotId : GetLastSlotId();
        if (slotId <= 0)
        {
            slotId = manager.GetAutoSaveSlotId();
        }

        manager.SaveCurrentGame(slotId);
    }

    public static void SaveCurrentGameToSlot(int slotId)
    {
        EnsureInstance().SaveCurrentGame(slotId);
    }

    public static void LoadSlotOrNewGame(int slotId)
    {
        SaveManager manager = EnsureInstance();
        manager.SetCurrentSlot(slotId);

        if (HasSlot(slotId))
        {
            manager.LoadSlot(slotId);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(FirstLevelSceneIndex);
    }

    public static void ContinueLastGame()
    {
        int slotId = GetLastSlotId();
        if (slotId <= 0)
        {
            return;
        }

        SaveManager manager = EnsureInstance();
        manager.SetCurrentSlot(slotId);
        manager.LoadSlot(slotId);
    }

    public static bool HasSlot(int slotId)
    {
        return File.Exists(GetSaveFilePath(slotId));
    }

    public static bool HasAnySlot()
    {
        for (int slotId = 1; slotId <= MaxSlotCount; slotId++)
        {
            if (HasSlot(slotId))
            {
                return true;
            }
        }

        return false;
    }

    public static int GetLastSlotId()
    {
        int slotId = PlayerPrefs.GetInt(LastSlotIdKey, 0);
        if (slotId > 0 && HasSlot(slotId))
        {
            return slotId;
        }

        slotId = EnsureInstance().GetLatestSlotId();
        if (slotId > 0)
        {
            SaveLastSlotId(slotId);
        }
        else
        {
            PlayerPrefs.DeleteKey(LastSlotIdKey);
            PlayerPrefs.Save();
        }

        return slotId;
    }

    public static void DeleteSlot(int slotId)
    {
        string filePath = GetSaveFilePath(slotId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        SaveManager manager = EnsureInstance();
        if (manager.currentSlotId == slotId)
        {
            manager.currentSlotId = 0;
        }

        if (PlayerPrefs.GetInt(LastSlotIdKey, 0) == slotId)
        {
            int latestSlotId = manager.GetLatestSlotId();
            if (latestSlotId > 0)
            {
                SaveLastSlotId(latestSlotId);
            }
            else
            {
                PlayerPrefs.DeleteKey(LastSlotIdKey);
                PlayerPrefs.Save();
            }
        }
    }

    public static string GetSlotLocationName(int slotId)
    {
        return EnsureInstance().ReadSlot(slotId).locationName;
    }

    public static string GetSlotPlayTimeText(int slotId)
    {
        SaveData data = EnsureInstance().ReadSlot(slotId);
        int totalMinutes = Mathf.FloorToInt(data.playTimeSeconds / 60f);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return string.Format("{0:D2}:{1:D2}", hours, minutes);
    }

    public static int GetSlotCurrentHealth(int slotId)
    {
        return EnsureInstance().ReadSlot(slotId).currentHealth;
    }

    public static int GetSlotMaxHealth(int slotId)
    {
        return EnsureInstance().ReadSlot(slotId).maxHealth;
    }

    public static float GetSlotSoulPowerRate(int slotId)
    {
        SaveData data = EnsureInstance().ReadSlot(slotId);
        return data.currentSoulPower / data.maxSoulPower;
    }

    public static string GetSlotIconPath(int slotId)
    {
        SaveData data = EnsureInstance().ReadSlot(slotId);
        return "SaveIcon/" + data.iconName;
    }

    private void SaveCurrentGame(int slotId)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        PlayerSoulPower playerSoulPower = player.GetComponent<PlayerSoulPower>();
        PlayerController playerController = player.GetComponent<PlayerController>();
        Vector3 savePosition = playerController.GetSavePosition();
        Vector2 safePosition = playerHealth.safePosition;
        string sceneName = SceneManager.GetActiveScene().name;

        SaveData data = new SaveData
        {
            version = CurrentVersion,
            slotId = slotId,
            sceneName = sceneName,
            locationName = GetLocationName(sceneName),
            iconName = GetIconName(sceneName),
            playerX = savePosition.x,
            playerY = savePosition.y,
            playerZ = savePosition.z,
            respawnX = safePosition.x,
            respawnY = safePosition.y,
            respawnZ = savePosition.z,
            currentHealth = playerHealth.current_health,
            maxHealth = playerHealth.max_health,
            currentSoulPower = playerSoulPower.CurrentSoulPower,
            maxSoulPower = playerSoulPower.maxSoulPower,
            playTimeSeconds = Time.realtimeSinceStartup,
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        Directory.CreateDirectory(GetSaveFolderPath());
        File.WriteAllText(GetSaveFilePath(slotId), JsonUtility.ToJson(data, true));
        SetCurrentSlot(slotId);
        Debug.Log($"[SaveManager] Save slot {slotId}: {data.sceneName} ({data.playerX}, {data.playerY}, {data.playerZ})");
    }

    private void LoadSlot(int slotId)
    {
        SetCurrentSlot(slotId);
        pendingLoadData = ReadSlot(slotId);
        Time.timeScale = 1f;
        SceneManager.sceneLoaded -= OnLoadSceneForSave;
        SceneManager.sceneLoaded += OnLoadSceneForSave;
        SceneManager.LoadScene(pendingLoadData.sceneName);
    }

    private void OnLoadSceneForSave(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLoadSceneForSave;
        StartCoroutine(ApplyPendingLoadDataNextFrame());
    }

    private System.Collections.IEnumerator ApplyPendingLoadDataNextFrame()
    {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        PlayerSoulPower playerSoulPower = player.GetComponent<PlayerSoulPower>();
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();

        Vector3 playerPosition = new Vector3(pendingLoadData.playerX, pendingLoadData.playerY, pendingLoadData.playerZ);
        Vector2 respawnPosition = new Vector2(pendingLoadData.respawnX, pendingLoadData.respawnY);
        player.transform.position = playerPosition;
        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0f;
        rb2d.simulated = true;

        playerHealth.ApplySaveData(pendingLoadData.currentHealth, pendingLoadData.maxHealth, respawnPosition);
        playerSoulPower.SetSoulPower(pendingLoadData.currentSoulPower, pendingLoadData.maxSoulPower);

        if (GameManager.instance != null)
        {
            GameManager.instance.player = player;
            GameManager.instance.SetRespawnPoint(respawnPosition);
            GameManager.instance.SetIsPlaying(true);
        }

        pendingLoadData = null;
    }

    private int GetAutoSaveSlotId()
    {
        for (int slotId = 1; slotId <= MaxSlotCount; slotId++)
        {
            if (!HasSlot(slotId))
            {
                return slotId;
            }
        }

        int oldestSlotId = 1;
        DateTime oldestTime = DateTime.MaxValue;
        for (int slotId = 1; slotId <= MaxSlotCount; slotId++)
        {
            DateTime writeTime = File.GetLastWriteTime(GetSaveFilePath(slotId));
            if (writeTime < oldestTime)
            {
                oldestTime = writeTime;
                oldestSlotId = slotId;
            }
        }

        return oldestSlotId;
    }

    private int GetLatestSlotId()
    {
        int latestSlotId = 0;
        DateTime latestTime = DateTime.MinValue;
        for (int slotId = 1; slotId <= MaxSlotCount; slotId++)
        {
            string filePath = GetSaveFilePath(slotId);
            if (!File.Exists(filePath))
            {
                continue;
            }

            DateTime writeTime = File.GetLastWriteTime(filePath);
            if (writeTime > latestTime)
            {
                latestTime = writeTime;
                latestSlotId = slotId;
            }
        }

        return latestSlotId;
    }

    private void SetCurrentSlot(int slotId)
    {
        currentSlotId = slotId;
        SaveLastSlotId(slotId);
    }

    private static void SaveLastSlotId(int slotId)
    {
        PlayerPrefs.SetInt(LastSlotIdKey, slotId);
        PlayerPrefs.Save();
    }

    private SaveData ReadSlot(int slotId)
    {
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(GetSaveFilePath(slotId)));
    }

    private static string GetSaveFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFolderName);
    }

    private static string GetSaveFilePath(int slotId)
    {
        return Path.Combine(GetSaveFolderPath(), SaveFilePrefix + slotId + SaveFileExtension);
    }

    private static string GetLocationName(string sceneName)
    {
        if (sceneName == "Level1")
        {
            return "City of Tears";
        }

        return sceneName;
    }

    private static string GetIconName(string sceneName)
    {
        if (sceneName == "Level1")
        {
            return "Area_Art_City_of_Tears";
        }

        return "Area_Dirtmouth";
    }
}
