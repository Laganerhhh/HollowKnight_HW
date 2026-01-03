using UnityEngine;
using UnityEngine.Rendering;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

public class PostProcessDebugger : MonoBehaviour
{
    [Header("诊断模式")]
    public bool logDetails = true;
    public bool takeScreenshot = false;
    
    void Start()
    {
        Debug.Log("=== 后处理问题深度诊断 ===");
        
        // 1. 检查所有摄像机
        CheckAllCameras();
        
        // 2. 检查渲染管线
        CheckRenderPipeline();
        
        // 3. 检查Volume系统
        CheckVolumeSystem();
        
        // 4. 检查渲染设置
        CheckRenderingSettings();
        
        Debug.Log("=== 诊断完成 ===");
    }
    
    void CheckAllCameras()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        Debug.Log($"场景中共有 {cameras.Length} 个摄像机");
        
        foreach (Camera cam in cameras)
        {
            Debug.Log($"\n检查摄像机: {cam.name}");
            Debug.Log($"- 启用: {cam.enabled}");
            Debug.Log($"- 深度: {cam.depth}");
            Debug.Log($"- Culling Mask: {cam.cullingMask}");
            Debug.Log($"- HDR: {cam.allowHDR}");
            Debug.Log($"- MSAA: {cam.allowMSAA}");
            
            #if USING_URP
            UniversalAdditionalCameraData camData = 
                cam.GetComponent<UniversalAdditionalCameraData>();
            
            if (camData != null)
            {
                Debug.Log($"URP 摄像机数据:");
                Debug.Log($"  - 后处理渲染: {camData.renderPostProcessing}");
                Debug.Log($"  - 渲染类型: {camData.renderType}");
                Debug.Log($"  - 抗锯齿: {camData.antialiasing}");
                Debug.Log($"  - 渲染器索引: {camData.GetRendererIndex()}");
                
                // 关键检查：渲染器是否正确
                UniversalRenderPipelineAsset urpAsset = 
                    GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                
                if (urpAsset != null)
                {
                    var rendererDataList = urpAsset.scriptableRendererDataList;
                    if (rendererDataList != null && rendererDataList.Length > 0)
                    {
                        int rendererIndex = camData.GetRendererIndex();
                        if (rendererIndex >= 0 && rendererIndex < rendererDataList.Length)
                        {
                            Debug.Log($"  - 使用的渲染器: {rendererDataList[rendererIndex].name}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"摄像机 {cam.name} 缺少 UniversalAdditionalCameraData 组件！");
            }
            #endif
        }
    }
    
    void CheckRenderPipeline()
    {
        Debug.Log("\n=== 渲染管线检查 ===");
        
        var currentPipeline = GraphicsSettings.currentRenderPipeline;
        if (currentPipeline == null)
        {
            Debug.LogError("当前没有使用可编程渲染管线！");
            Debug.Log("请检查: Edit -> Project Settings -> Graphics -> Scriptable Render Pipeline Settings");
            return;
        }
        
        Debug.Log($"当前渲染管线: {currentPipeline.name}");
        Debug.Log($"管线类型: {currentPipeline.GetType()}");
        
        #if USING_URP
        UniversalRenderPipelineAsset urpAsset = currentPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            Debug.Log($"URP 设置:");
            Debug.Log($"  - 后处理支持: {urpAsset.supportsPostProcessing}");
            Debug.Log($"  - MSAA: {urpAsset.msaaSampleCount}");
            Debug.Log($"  - HDR: {urpAsset.supportsHDR}");
            Debug.Log($"  - 渲染缩放: {urpAsset.renderScale}");
            
            // 检查渲染器数据
            var rendererDataList = urpAsset.scriptableRendererDataList;
            if (rendererDataList != null)
            {
                Debug.Log($"共有 {rendererDataList.Length} 个渲染器");
                
                for (int i = 0; i < rendererDataList.Length; i++)
                {
                    var rendererData = rendererDataList[i];
                    Debug.Log($"渲染器 [{i}]: {rendererData.name}");
                    
                    // 检查是否启用了后处理渲染器特性
                    var features = rendererData.rendererFeatures;
                    bool hasPostProcessFeature = false;
                    
                    foreach (var feature in features)
                    {
                        if (feature != null && feature.isActive)
                        {
                            Debug.Log($"  - 特性: {feature.name} (启用: {feature.isActive})");
                            if (feature.GetType().Name.Contains("PostProcess"))
                                hasPostProcessFeature = true;
                        }
                    }
                    
                    if (!hasPostProcessFeature)
                    {
                        Debug.LogWarning($"渲染器 {rendererData.name} 可能没有后处理特性！");
                    }
                }
            }
        }
        #endif
    }
    
    void CheckVolumeSystem()
    {
        Debug.Log("\n=== Volume 系统检查 ===");
        
        Volume[] volumes = FindObjectsOfType<Volume>();
        Debug.Log($"找到 {volumes.Length} 个Volume");
        
        bool foundActiveBloom = false;
        
        foreach (Volume volume in volumes)
        {
            Debug.Log($"\nVolume: {volume.name}");
            Debug.Log($"- 启用: {volume.enabled}");
            Debug.Log($"- 权重: {volume.weight}");
            Debug.Log($"- 优先级: {volume.priority}");
            Debug.Log($"- 全局: {volume.isGlobal}");
            
            if (volume.profile == null)
            {
                Debug.LogWarning($"Volume {volume.name} 没有Profile！");
                continue;
            }
            
            #if USING_URP
            // 检查所有后处理效果
            VolumeProfile profile = volume.profile;
            
            if (profile.TryGet<Bloom>(out Bloom bloom))
            {
                Debug.Log($"  ✓ 找到Bloom效果");
                Debug.Log($"    - 激活: {bloom.active}");
                Debug.Log($"    - 强度: {bloom.intensity.value}");
                Debug.Log($"    - 阈值: {bloom.threshold.value}");
                
                if (bloom.active && volume.weight > 0)
                    foundActiveBloom = true;
            }
            
            // 检查其他可能影响Bloom的效果
            if (profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdj))
            {
                Debug.Log($"  - Color Adjustments: {colorAdj.active}");
            }
            
            if (profile.TryGet<Vignette>(out Vignette vignette))
            {
                Debug.Log($"  - Vignette: {vignette.active}");
            }
            
            if (profile.TryGet<FilmGrain>(out FilmGrain filmGrain))
            {
                Debug.Log($"  - Film Grain: {filmGrain.active}");
            }
            #endif
        }
        
        if (!foundActiveBloom)
        {
            Debug.LogError("没有找到激活的Bloom效果！");
        }
        else
        {
            Debug.Log("✓ 找到激活的Bloom效果");
        }
    }
    
    void CheckRenderingSettings()
    {
        Debug.Log("\n=== 渲染设置检查 ===");
        
        // 检查质量设置
        int qualityLevel = QualitySettings.GetQualityLevel();
        Debug.Log($"当前质量等级: {qualityLevel} - {QualitySettings.names[qualityLevel]}");
        Debug.Log($"抗锯齿: {QualitySettings.antiAliasing}");
        
        // 检查Graphics设置
        Debug.Log($"默认帧率: {Application.targetFrameRate}");
        Debug.Log($"垂直同步: {QualitySettings.vSyncCount}");
        
        // 检查平台相关设置
        #if UNITY_EDITOR
        Debug.Log("运行在编辑器中");
        #endif
        
        // 检查是否启用了多相机渲染
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        Debug.Log($"主摄像机深度纹理模式: {Camera.main.depthTextureMode}");
    }
    
    [ContextMenu("强制修复所有设置")]
    void ForceFixAll()
    {
        Debug.Log("开始强制修复...");
        
        // 修复所有摄像机
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            #if USING_URP
            UniversalAdditionalCameraData camData = 
                cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            
            // 强制启用后处理
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            #endif
            
            // 启用HDR
            cam.allowHDR = true;
            cam.allowMSAA = true;
        }
        
        // 确保有激活的Volume
        Volume[] volumes = FindObjectsOfType<Volume>();
        if (volumes.Length == 0)
        {
            CreateDefaultVolume();
        }
        
        Debug.Log("强制修复完成，请重启游戏查看效果");
    }
    
    void CreateDefaultVolume()
    {
        GameObject volumeObj = new GameObject("Global Volume (Auto Created)");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1;
        volume.priority = 100;
        
        #if USING_URP
        // 创建Profile
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // 添加Bloom效果
        Bloom bloom = volume.profile.Add<Bloom>();
        bloom.intensity.Override(1f);
        bloom.threshold.Override(0.9f);
        bloom.active = true;
        #endif
        
        Debug.Log("已创建默认Volume和Bloom效果");
    }
}