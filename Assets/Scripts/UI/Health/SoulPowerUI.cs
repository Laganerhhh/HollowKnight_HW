using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoulPowerUI : MonoBehaviour
{
    public static SoulPowerUI Instance { get; private set; }

    private Image image;

    private Animator animator;

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
        animator = GetComponent<Animator>();
        SetSoulPower(1f);
    }

    public void SetSoulPower(float value)
    {
        value = Mathf.Clamp01(value);
        image.fillAmount = value;

        if (value >= 1f)
        {
            InvokeRepeating("PlayFullSoulPowerAnimation", 0f, 2f);
        }
        else
        {
            CancelInvoke("PlayFullSoulPowerAnimation");
        }
    }

    private void PlayFullSoulPowerAnimation()
    {
        animator.SetTrigger("full_power");
    }

}
