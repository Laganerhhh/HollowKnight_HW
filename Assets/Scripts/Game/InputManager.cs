using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入管理器，负责输入的映射与处理，同时支持键盘与手柄的输入
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public enum GameButton
    {
        Jump,       // K / A
        Dash,       // L / B
        Attack,     // J / X
        SuperDash,  // I / Y
        FireBall,   // U / RT
        Heal        // P / RB
    }

    [Header("Joystick Button Indices (defaults for Xbox)")]
    public int joyA = 0; // A
    public int joyB = 1; // B
    public int joyX = 2; // X
    public int joyY = 3; // Y
    public int joyRB = 5; // Right bumper

    [Header("Trigger / Right Stick Axis Names")]
    public string rightTriggerAxis = "RT"; // configure in InputManager if needed
    public string rightStickHorizontal = "RightStickHorizontal"; // configure in InputManager
    public string rightStickVertical = "RightStickVertical"; // configure in InputManager

    [Header("Look (right stick) threshold")]
    public float lookThreshold = 0.5f;

    [Header("Keyboard Key Mapping (editable)")]
    public KeyCode jumpKey = KeyCode.K;
    public KeyCode dashKey = KeyCode.L;
    public KeyCode attackKey = KeyCode.J;
    public KeyCode superDashKey = KeyCode.I;
    public KeyCode fireBallKey = KeyCode.U;
    public KeyCode healKey = KeyCode.P;

    [Header("Trigger / analog thresholds")]
    public float triggerThreshold = 0.5f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Generic button checks combining keyboard and joystick
    public bool GetButtonDown(GameButton btn)
    {
        switch (btn)
        {
            case GameButton.Jump:
                if (Input.GetKeyDown(jumpKey)) return true;
                try { if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyA))) return true; } catch { }
                return false;
            case GameButton.Dash:
                if (Input.GetKeyDown(dashKey)) return true;
                try { if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyB))) return true; } catch { }
                return false;
            case GameButton.Attack:
                if (Input.GetKeyDown(attackKey)) return true;
                try { if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyX))) return true; } catch { }
                return false;
            case GameButton.SuperDash:
                if (Input.GetKeyDown(superDashKey)) return true;
                try { if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyY))) return true; } catch { }
                return false;
            case GameButton.FireBall:
                // FireBall: keyboard or right trigger pressed
                if (Input.GetKeyDown(fireBallKey)) return true;
                if (!string.IsNullOrEmpty(rightTriggerAxis))
                {
                    try { return Input.GetAxisRaw(rightTriggerAxis) > triggerThreshold; } catch { }
                }
                return false;
            case GameButton.Heal:
                if (Input.GetKeyDown(healKey)) return true;
                try { if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyRB))) return true; } catch { }
                return false;
            default:
                return false;
        }
    }

    public bool GetButtonUp(GameButton btn)
    {
        switch (btn)
        {
            case GameButton.Jump:
                if (Input.GetKeyUp(jumpKey)) return true;
                try { if (Input.GetKeyUp((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyA))) return true; } catch { }
                return false;
            case GameButton.Dash:
                if (Input.GetKeyUp(dashKey)) return true;
                try { if (Input.GetKeyUp((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyB))) return true; } catch { }
                return false;
            case GameButton.Attack:
                if (Input.GetKeyUp(attackKey)) return true;
                try { if (Input.GetKeyUp((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyX))) return true; } catch { }
                return false;
            case GameButton.SuperDash:
                if (Input.GetKeyUp(superDashKey)) return true;
                try { if (Input.GetKeyUp((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyY))) return true; } catch { }
                return false;
            case GameButton.FireBall:
                if (Input.GetKeyUp(fireBallKey)) return true;
                if (!string.IsNullOrEmpty(rightTriggerAxis))
                {
                    try { return Input.GetAxisRaw(rightTriggerAxis) <= triggerThreshold; } catch { }
                }
                return false;
            case GameButton.Heal:
                if (Input.GetKeyUp(healKey)) return true;
                try { if (Input.GetKeyUp((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyRB))) return true; } catch { }
                return false;
            default:
                return false;
        }
    }

    public bool GetButton(GameButton btn)
    {
        switch (btn)
        {
            case GameButton.Jump:
                if (Input.GetKey(jumpKey)) return true;
                try { if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyA))) return true; } catch { }
                return false;
            case GameButton.Dash:
                if (Input.GetKey(dashKey)) return true;
                try { if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyB))) return true; } catch { }
                return false;
            case GameButton.Attack:
                if (Input.GetKey(attackKey)) return true;
                try { if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyX))) return true; } catch { }
                return false;
            case GameButton.SuperDash:
                if (Input.GetKey(superDashKey)) return true;
                try { if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyY))) return true; } catch { }
                return false;
            case GameButton.FireBall:
                if (Input.GetKey(fireBallKey)) return true;
                if (!string.IsNullOrEmpty(rightTriggerAxis))
                {
                    try { return Input.GetAxisRaw(rightTriggerAxis) > triggerThreshold; } catch { }
                }
                return false;
            case GameButton.Heal:
                if (Input.GetKey(healKey)) return true;
                try { if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + joyRB))) return true; } catch { }
                return false;
            default:
                return false;
        }
    }

    // Right stick look vector (used to emulate arrow keys)
    public Vector2 GetLookVector()
    {
        float rx = 0f, ry = 0f;
        if (!string.IsNullOrEmpty(rightStickHorizontal))
        {
            try { rx = Input.GetAxisRaw(rightStickHorizontal); } catch { rx = 0f; }
        }
        if (!string.IsNullOrEmpty(rightStickVertical))
        {
            try { ry = Input.GetAxisRaw(rightStickVertical); } catch { ry = 0f; }
        }
        return new Vector2(rx, ry);
    }


}
