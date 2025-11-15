using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoulPowerUI : MonoBehaviour
{
    public static SoulPowerUI Instance { get; private set; }

    private Image image;

    private void Awake()
    {
        Instance = this;
        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        image = GetComponent<Image>();
    }

    public void SetSoulPower(float value)
    {
        value = Mathf.Clamp01(value);
        image.fillAmount = value;
    }
}
