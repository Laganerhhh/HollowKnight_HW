using UnityEngine;
using UnityEngine.SceneManagement;

public class GMService : MonoBehaviour
{
    public static GMService Instance { get; private set; }

    public bool InvincibleEnabled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        EnsureInstance();
    }

    public static GMService EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject gmObject = new GameObject("GMService");
        DontDestroyOnLoad(gmObject);
        return gmObject.AddComponent<GMService>();
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
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentStateToPlayer();
    }

    public void SetInvincible(bool enabled)
    {
        InvincibleEnabled = enabled;
        ApplyCurrentStateToPlayer();
    }

    public void ToggleInvincible()
    {
        SetInvincible(!InvincibleEnabled);
    }

    public void ApplyCurrentStateToPlayer()
    {
        PlayerHealth playerHealth = FindPlayerHealth();
        if (playerHealth != null)
        {
            playerHealth.SetGMInvincible(InvincibleEnabled);
        }
    }

    public PlayerHealth FindPlayerHealth()
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<PlayerHealth>();
        }

        return null;
    }

    public PlayerController FindPlayerController()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            return playerController;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<PlayerController>();
        }

        return null;
    }

    public void HealPlayerToFull()
    {
        PlayerHealth playerHealth = FindPlayerHealth();
        if (playerHealth != null)
        {
            playerHealth.FillHealthToMax();
        }
    }

    public void ResetPlayerToSafePosition()
    {
        PlayerHealth playerHealth = FindPlayerHealth();
        if (playerHealth != null)
        {
            playerHealth.TeleportToSafePosition();
        }
    }

    public void MovePlayerToOrigin()
    {
        PlayerController playerController = FindPlayerController();
        if (playerController != null)
        {
            playerController.transform.position = Vector3.zero;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}