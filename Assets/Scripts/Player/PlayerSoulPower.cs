using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SoulPowerSkill
{
    None,
    FireBall,
    Recovery
}

/// <summary>
/// 玩家灵魂能量管理、使用
/// </summary>
public class PlayerSoulPower : MonoBehaviour
{
    private float currentSoulPower = 100;
    public float CurrentSoulPower { get; private set; }

    public float maxSoulPower = 100;

    [Header("技能消耗")]
    [SerializeField] private float fireBallCost = 10;
    [SerializeField] private float recoveryCost = 20;

    void Start()
    {
        currentSoulPower = maxSoulPower;
    }

    public bool UseSoulPower(SoulPowerSkill skill)
    {
        switch (skill)
        {
            case SoulPowerSkill.None:
                return false;
            case SoulPowerSkill.FireBall:
                if (currentSoulPower >= fireBallCost)
                {
                    currentSoulPower -= fireBallCost;
                }
                else
                {
                    return false;
                }
                break;
            case SoulPowerSkill.Recovery:
                if (currentSoulPower >= recoveryCost)
                {
                    currentSoulPower -= recoveryCost;
                }
                else
                {
                    return false;
                }
                break;
            default:
                return false;
        }
        UpdateSoulPowerUI();
        return true;
    }

    public void AddSoulPower(float value)
    {
        if (currentSoulPower + value > maxSoulPower)
        {
            currentSoulPower = maxSoulPower;
            UpdateSoulPowerUI();
            return;
        }
        currentSoulPower += value;
        UpdateSoulPowerUI();
    }

    public void ResetSoulPower()
    {
        currentSoulPower = maxSoulPower;
        UpdateSoulPowerUI();
    }

    private void UpdateSoulPowerUI()
    {
        SoulPowerUI.Instance.SetSoulPower(currentSoulPower / maxSoulPower);
    }
}
