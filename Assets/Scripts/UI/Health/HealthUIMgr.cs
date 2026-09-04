using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthUIMgr : MonoBehaviour
{
    public static HealthUIMgr Instance { get; private set; }
    [SerializeField] private HealthIcon[] healthIcons;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoseHealth(int current_health, int amount = 1, int max_health = 5)
    {
        if (healthIcons.Length != max_health)
        {
            Debug.LogError("Health Icons array length does not match max health!");
            return;
        }
        int startIndex = current_health;
        for (int i = 0; i < amount; i++)
        {
            HealthIcon healthIcon = healthIcons[startIndex + i];
            healthIcon.SetIsLoseHealth(true);
        }
    }

    public void GainHealth(int current_health, int amount = 1, int max_health = 5)
    {
        if (current_health == max_health)
        {
            return;
        }
        int startIndex = current_health; //这里指向了下一个要恢复的生命图标
        if (current_health + amount > max_health)
        {
            amount = max_health - current_health;
        }
        for (int i = 0; i < amount; i++)
        {
            HealthIcon healthIcon = healthIcons[startIndex + i];
            healthIcon.SetIsLoseHealth(false);
        }
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        int validMaxHealth = Mathf.Min(maxHealth, healthIcons.Length);
        currentHealth = Mathf.Clamp(currentHealth, 0, validMaxHealth);

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null)
            {
                continue;
            }

            healthIcons[i].SetIsLoseHealthImmediate(i >= currentHealth || i >= validMaxHealth);
        }
    }
}
