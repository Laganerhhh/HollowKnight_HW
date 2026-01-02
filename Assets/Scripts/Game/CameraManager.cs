using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 提供一些摄像机相关的全局功能
/// </summary>

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    private Camera mainCamera;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    private CinemachineFramingTransposer framingTransposer;

    private Coroutine shakeCoroutine;
    private float originalNoiseAmplitude = 0f;
    private float originalOrthoSize = 5f;
    // 平移（左右查看）相关
    [SerializeField] private float panOffsetX = 3f; // 按键时相对于跟随目标的横向偏移（world units）
    [SerializeField] private float panOffsetY = 2f; // 按键时相对于跟随目标的纵向偏移（world units）
    [SerializeField] private float panDuration = 0.25f; // 平滑过渡时间
    private Coroutine panCoroutine;
    private Vector3 originalFramingOffset = Vector3.zero;
    // 缩放相关（正交摄像机）
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 10f;
    [SerializeField] private float zoomStep = 1f;
    [SerializeField] private float defaultZoomDuration = 0.35f;
    // 室内/室外切换
    [SerializeField] private float outdoorOrthoSize = 8f; // 室外拉远的目标尺寸
    [SerializeField] private float outdoorTransitionDuration = 0.5f;
    private bool isOutdoor = false;
    // 手柄震动相关（通过反射使用 Unity 新输入系统的 Gamepad，如果可用）
    private Coroutine rumbleCoroutine;
    private Vector2 lastPan = Vector2.zero;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        // 尝试查找场景中的 Cinemachine Virtual Camera
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        if (virtualCamera != null)
        {
            noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            // 如果场景中的虚拟摄像机没有 Noise 扩展，则在运行时添加一个，保证 Shake 可用
            if (noise == null)
            {
                noise = virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (noise != null)
                {
                    noise.m_AmplitudeGain = 0f;
                    noise.m_FrequencyGain = 1f;
                }
            }

            originalNoiseAmplitude = noise != null ? noise.m_AmplitudeGain : 0f;
            originalOrthoSize = virtualCamera.m_Lens.OrthographicSize;
            if (framingTransposer != null)
                originalFramingOffset = framingTransposer.m_TrackedObjectOffset;
        }
    }

    void Update()
    {
        // 箭头键四方向查看（按住移动，松开恢复）
        if (Input.GetKeyDown(KeyCode.LeftArrow)) StartPan(new Vector2(-Mathf.Abs(panOffsetX), 0f));
        if (Input.GetKeyDown(KeyCode.RightArrow)) StartPan(new Vector2(Mathf.Abs(panOffsetX), 0f));
        if (Input.GetKeyDown(KeyCode.UpArrow)) StartPan(new Vector2(0f, Mathf.Abs(panOffsetY)));
        if (Input.GetKeyDown(KeyCode.DownArrow)) StartPan(new Vector2(0f, -Mathf.Abs(panOffsetY)));

        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
        {
            // 恢复
            StartPan(Vector2.zero);
        }

        // 手柄右摇杆作为查看方向（通过 InputManager 配置的轴名）
        if (InputManager.instance != null)
        {
            Vector2 look = InputManager.instance.GetLookVector();
            float th = InputManager.instance.lookThreshold;
            Vector2 pan = Vector2.zero;
            if (Mathf.Abs(look.x) > th) pan.x = Mathf.Sign(look.x) * Mathf.Abs(panOffsetX);
            if (Mathf.Abs(look.y) > th) pan.y = Mathf.Sign(look.y) * Mathf.Abs(panOffsetY);

            // 仅在 pan 状态发生改变时才触发，避免每帧重启协程
            if ((pan - lastPan).sqrMagnitude > 0.001f)
            {
                StartPan(pan);
                lastPan = pan;
            }
        }
    }

    /// <summary>
    /// 获取主摄像机
    /// </summary>
    /// <returns></returns>
    public Camera GetMainCamera()
    {
        return mainCamera;
    }

    /// <summary>
    /// 获取 Cinemachine 虚拟摄像机（如果存在）
    /// </summary>
    public CinemachineVirtualCamera GetVirtualCamera()
    {
        return virtualCamera;
    }

    /// <summary>
    /// 触发摄像机抖动（地震效果）。
    /// duration: 持续时间（秒）。
    /// amplitude: 抖动幅度（建议 0.1 - 2.0 之间，取决于需要）。
    /// </summary>
    public void Shake(float duration = 0.5f, float amplitude = 1.0f)
    {
        // 在触发抖动前，尝试通过 CinemachineBrain 获取当前活动的虚拟摄像机（兼容不同版本 API）
        CinemachineBrain brain = null;
        if (mainCamera != null)
            brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = FindObjectOfType<CinemachineBrain>();

        ICinemachineCamera activeCam = null;
        if (brain != null)
            activeCam = brain.ActiveVirtualCamera;

        if (activeCam != null)
        {
            var go = activeCam.VirtualCameraGameObject;
            if (go != null)
            {
                virtualCamera = go.GetComponent<CinemachineVirtualCamera>() ?? virtualCamera;
                noise = virtualCamera != null ? virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>() : noise;
                framingTransposer = virtualCamera != null ? virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() : framingTransposer;
            }
        }

        if (virtualCamera == null || noise == null)
        {
            // 后备方案：对主相机做简单位移抖动
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(BackupShakeRoutine(duration, amplitude));
            return;
        }

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, amplitude));

        // 尝试启动手柄震动（如果支持）——以幅度和时长映射为低/高频振动
        if (rumbleCoroutine != null) StopCoroutine(rumbleCoroutine);
        rumbleCoroutine = StartCoroutine(RumbleRoutine(duration, Mathf.Clamp01(amplitude)));
    }

    /// <summary>
    /// 停止正在进行的抖动
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (noise != null)
            noise.m_AmplitudeGain = originalNoiseAmplitude;

        // 停止手柄震动
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
            rumbleCoroutine = null;
            StopRumbleImmediate();
        }
    }

    private IEnumerator ShakeRoutine(float duration, float amplitude)
    {
        float elapsed = 0f;
        float startAmp = noise != null ? noise.m_AmplitudeGain : 0f;
        bool useFramingJitter = false;
        Vector3 originalFramingOffset = Vector3.zero;
        if (noise == null || noise.m_NoiseProfile == null)
        {
            if (framingTransposer != null)
            {
                useFramingJitter = true;
                originalFramingOffset = framingTransposer.m_TrackedObjectOffset;
            }
            else
            {
                // 如果既没有有效的 Noise 配置，也没有 FramingTransposer，退回到主相机位移抖动
                if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
                shakeCoroutine = StartCoroutine(BackupShakeRoutine(duration, amplitude));
                yield break;
            }
        }
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curAmp = Mathf.Lerp(amplitude, 0f, t);
            if (!useFramingJitter && noise != null && noise.m_NoiseProfile != null)
            {
                noise.m_AmplitudeGain = curAmp;
                noise.m_FrequencyGain = Mathf.Lerp(2f, 0.5f, t);
            }
            else if (useFramingJitter && framingTransposer != null)
            {
                Vector2 jitter = Random.insideUnitCircle * curAmp;
                framingTransposer.m_TrackedObjectOffset = originalFramingOffset + new Vector3(jitter.x, jitter.y, 0f);
            }
            yield return null;
        }

        if (noise != null)
            noise.m_AmplitudeGain = originalNoiseAmplitude;

        if (useFramingJitter && framingTransposer != null)
            framingTransposer.m_TrackedObjectOffset = originalFramingOffset;

        shakeCoroutine = null;
    }

    // 后备抖动：直接对主相机短时偏移（仅在没有 Cinemachine 时使用）
    private IEnumerator BackupShakeRoutine(float duration, float amplitude)
    {
        if (mainCamera == null) yield break;
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration);
            float curAmp = amplitude * t;
            mainCamera.transform.localPosition = originalPos + (Vector3)(Random.insideUnitCircle * curAmp);
            yield return null;
        }
        mainCamera.transform.localPosition = originalPos;
        shakeCoroutine = null;
    }

    // 手柄振动协程（使用反射调用 Unity 新输入系统的 Gamepad API，如果存在）
    private IEnumerator RumbleRoutine(float duration, float amplitude)
    {
        // 通过反射尝试获取 Gamepad.current 并调用 SetMotorSpeeds(low, high)
        System.Type gamepadType = null;
        try
        {
            // 常见类型全名尝试
            gamepadType = System.Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem")
                         ?? System.Type.GetType("UnityEngine.InputSystem.Gamepad, UnityEngine.InputSystem");
        }
        catch { gamepadType = null; }

        object gamepadCurrent = null;
        System.Reflection.MethodInfo setMotor = null;
        System.Reflection.MethodInfo resetHaptics = null;
        if (gamepadType != null)
        {
            var currentProp = gamepadType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (currentProp != null)
            {
                gamepadCurrent = currentProp.GetValue(null, null);
                setMotor = gamepadType.GetMethod("SetMotorSpeeds", new System.Type[] { typeof(float), typeof(float) });
                resetHaptics = gamepadType.GetMethod("ResetHaptics");
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration);
            float low = amplitude * 0.5f * t; // 低频
            float high = amplitude * 1.0f * t; // 高频

            if (gamepadCurrent != null && setMotor != null)
            {
                try { setMotor.Invoke(gamepadCurrent, new object[] { low, high }); } catch { }
            }
            yield return null;
        }

        // 结束时清除 haptics
        if (gamepadCurrent != null && resetHaptics != null)
        {
            try { resetHaptics.Invoke(gamepadCurrent, null); } catch { }
        }

        rumbleCoroutine = null;
    }

    // 如果没有反射到 API，仍然尝试通过反射调用一次 ResetHaptics（安全失败）
    private void StopRumbleImmediate()
    {
        System.Type gamepadType = null;
        try
        {
            gamepadType = System.Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem")
                         ?? System.Type.GetType("UnityEngine.InputSystem.Gamepad, UnityEngine.InputSystem");
        }
        catch { gamepadType = null; }

        if (gamepadType != null)
        {
            var currentProp = gamepadType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (currentProp != null)
            {
                var gamepadCurrent = currentProp.GetValue(null, null);
                var resetHaptics = gamepadType.GetMethod("ResetHaptics");
                if (gamepadCurrent != null && resetHaptics != null)
                {
                    try { resetHaptics.Invoke(gamepadCurrent, null); } catch { }
                }
            }
        }
    }

    /// <summary>
    /// 平滑缩放摄像机到指定正交尺寸（仅当虚拟摄像机存在时）。
    /// </summary>
    public void ZoomTo(float targetOrthoSize, float duration = 0.5f)
    {
        if (virtualCamera == null)
        {
            if (mainCamera != null)
                StartCoroutine(BackupZoomRoutine(mainCamera.orthographicSize, targetOrthoSize, duration));
            return;
        }

        StartCoroutine(ZoomRoutine(targetOrthoSize, duration));
    }

    /// <summary>
    /// 切换到室外摄像机状态（拉远）
    /// </summary>
    public void SetOutdoorCamera()
    {
        if (isOutdoor) return;
        isOutdoor = true;
        // 拉远视野
        SetZoom(outdoorOrthoSize, outdoorTransitionDuration);
    }

    /// <summary>
    /// 切换到室内摄像机状态（恢复）
    /// </summary>
    public void SetIndoorCamera()
    {
        if (!isOutdoor) return;
        isOutdoor = false;
        // 恢复为原始正交尺寸
        SetZoom(originalOrthoSize, outdoorTransitionDuration);
        // 恢复跟随偏移
        StartPan(Vector2.zero);
    }

    /// <summary>
    /// 当前是否为室外状态
    /// </summary>
    public bool IsOutdoor()
    {
        return isOutdoor;
    }

    /// <summary>
    /// 获取当前正交尺寸（优先虚拟摄像机）
    /// </summary>
    public float GetCurrentOrtho()
    {
        if (virtualCamera != null)
            return virtualCamera.m_Lens.OrthographicSize;
        if (mainCamera != null)
            return mainCamera.orthographicSize;
        return originalOrthoSize;
    }

    /// <summary>
    /// 将当前视野按 delta 缩放（正值放远，负值拉近）
    /// </summary>
    public void ZoomBy(float delta, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultZoomDuration;
        float current = GetCurrentOrtho();
        float target = Mathf.Clamp(current + delta, minOrthoSize, maxOrthoSize);
        ZoomTo(target, duration);
    }

    /// <summary>
    /// 快捷：放大（拉近视角）
    /// </summary>
    public void ZoomIn(float duration = -1f)
    {
        ZoomBy(-Mathf.Abs(zoomStep), duration);
    }

    /// <summary>
    /// 快捷：缩小（拉远视角）
    /// </summary>
    public void ZoomOut(float duration = -1f)
    {
        ZoomBy(Mathf.Abs(zoomStep), duration);
    }

    /// <summary>
    /// 直接设置缩放（带限制）
    /// </summary>
    public void SetZoom(float orthoSize, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultZoomDuration;
        float target = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);
        ZoomTo(target, duration);
    }

    private IEnumerator ZoomRoutine(float targetSize, float duration)
    {
        float start = virtualCamera.m_Lens.OrthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(start, targetSize, t);
            yield return null;
        }
        virtualCamera.m_Lens.OrthographicSize = targetSize;
    }

    private IEnumerator BackupZoomRoutine(float startSize, float targetSize, float duration)
    {
        if (mainCamera == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
        mainCamera.orthographicSize = targetSize;
    }

    /// <summary>
    /// 设置虚拟摄像机的跟随偏移（仅在使用 FramingTransposer 时有效）
    /// </summary>
    public void SetFollowOffset(Vector3 offset)
    {
        if (framingTransposer != null)
        {
            framingTransposer.m_TrackedObjectOffset = offset;
        }
    }

    private void StartPan(Vector2 targetLocalOffset)
    {
        if (panCoroutine != null)
            StopCoroutine(panCoroutine);
        panCoroutine = StartCoroutine(PanOffsetRoutine(targetLocalOffset, panDuration));
    }

    private IEnumerator PanOffsetRoutine(Vector2 targetLocalOffset, float duration)
    {
        // 如果有 FramingTransposer，则改变其 m_TrackedObjectOffset.xy
        if (framingTransposer != null)
        {
            Vector3 start = framingTransposer.m_TrackedObjectOffset;
            Vector3 target = originalFramingOffset + new Vector3(targetLocalOffset.x, targetLocalOffset.y, 0f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                framingTransposer.m_TrackedObjectOffset = Vector3.Lerp(start, target, t);
                yield return null;
            }
            framingTransposer.m_TrackedObjectOffset = target;
            panCoroutine = null;
            yield break;
        }

        // 后备方案：在没有 FramingTransposer 时，移动主摄像机的位置（保持 z）
        if (mainCamera != null)
        {
            Vector3 start = mainCamera.transform.position;
            Vector3 target = start + new Vector3(targetLocalOffset.x, targetLocalOffset.y, 0f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainCamera.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            mainCamera.transform.position = target;
        }
        panCoroutine = null;
    }

    /// <summary>
    /// 平滑将摄像机移动到指定世界位置（会临时替换 virtualCamera.Follow 为一个临时 Transform）。
    /// 适用于短时的镜头移动/剧情镜头。
    /// </summary>
    public void PanToPosition(Vector3 worldPos, float duration = 0.5f, bool restoreFollow = true)
    {
        if (virtualCamera == null)
        {
            // 直接移动主摄像机（后备）
            StartCoroutine(BackupPanRoutine(worldPos, duration));
            return;
        }

        StartCoroutine(PanRoutine(worldPos, duration, restoreFollow));
    }

    private IEnumerator PanRoutine(Vector3 worldPos, float duration, bool restoreFollow)
    {
        Transform prevFollow = virtualCamera.Follow;
        GameObject temp = new GameObject("CamPanTarget");
        temp.transform.position = virtualCamera.Follow != null ? virtualCamera.Follow.position : mainCamera.transform.position;
        virtualCamera.Follow = temp.transform;

        float elapsed = 0f;
        Vector3 start = temp.transform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            temp.transform.position = Vector3.Lerp(start, worldPos, t);
            yield return null;
        }
        temp.transform.position = worldPos;

        if (restoreFollow)
        {
            virtualCamera.Follow = prevFollow;
        }
        Destroy(temp);
    }

    private IEnumerator BackupPanRoutine(Vector3 worldPos, float duration)
    {
        if (mainCamera == null) yield break;
        float elapsed = 0f;
        Vector3 start = mainCamera.transform.position;
        Vector3 target = new Vector3(worldPos.x, worldPos.y, start.z);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        mainCamera.transform.position = target;
    }

    /// <summary>
    /// 直接设置虚拟摄像机的 Follow 目标
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        if (virtualCamera != null)
            virtualCamera.Follow = target;
    }
}
