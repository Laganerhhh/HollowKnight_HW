using UnityEngine;
using UnityEngine.Rendering;

#if USING_URP
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif


public class VolumeFixManager : MonoBehaviour
{
    [System.Serializable]
    public class VolumeFixSettings
    {
        public bool fixBloomActivation = true;
        public bool createIfMissing = true;
        public float defaultBloomIntensity = 1.2f;
        public float defaultBloomThreshold = 0.85f;
        public bool applyToAllVolumes = true;
    }
    
    public VolumeFixSettings settings = new VolumeFixSettings();
    
    void Start()
    {
        FixAllVolumes();
        
        // 验证修复结果
        StartCoroutine(VerifyFixAfterFrame());
    }
    
    void FixAllVolumes()
    {
        Debug.Log("=== 开始修复Volume设置 ===");
        
        Volume[] volumes = FindObjectsOfType<Volume>();
        
        if (volumes.Length == 0 && settings.createIfMissing)
        {
            Debug.Log("未找到任何Volume，创建默认Volume...");
            CreateDefaultVolume();
            return;
        }
        
        foreach (Volume volume in volumes)
        {
            FixSingleVolume(volume);
        }
        
        Debug.Log($"已处理 {volumes.Length} 个Volume");
    }
    
    void FixSingleVolume(Volume volume)
    {
        if (volume == null) return;
        
        Debug.Log($"处理Volume: {volume.name}");
        
        // 确保Volume基本设置正确
        if (volume.weight <= 0)
        {
            volume.weight = 1f;
            Debug.Log($"  - 修正权重: 0 -> 1");
        }
        
        if (!volume.enabled)
        {
            volume.enabled = true;
            Debug.Log($"  - 启用Volume");
        }
        
        // 检查Profile
        if (volume.profile == null)
        {
            Debug.LogWarning($"Volume '{volume.name}' 没有Profile，正在创建...");
            #if USING_URP
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            #if UNITY_EDITOR
            AssetDatabase.CreateAsset(volume.profile, 
                $"Assets/VolumeProfiles/{volume.name}_Profile.asset");
            #endif
            #endif
        }
        
        #if USING_URP
        // 修复Bloom效果
        if (settings.fixBloomActivation && volume.profile != null)
        {
            FixBloomInProfile(volume.profile, volume.name);
        }
        #endif
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(volume);
        if (volume.profile != null)
            EditorUtility.SetDirty(volume.profile);
        #endif
    }
    
    #if USING_URP
    void FixBloomInProfile(VolumeProfile profile, string volumeName)
    {
        if (profile.TryGet<Bloom>(out Bloom bloom))
        {
            // 检查并修复Bloom
            bool needsFix = false;
            
            if (!bloom.active)
            {
                bloom.active = true;
                needsFix = true;
                Debug.Log($"  - 激活Bloom效果");
            }
            
            if (bloom.intensity.value <= 0)
            {
                bloom.intensity.Override(settings.defaultBloomIntensity);
                needsFix = true;
                Debug.Log($"  - 设置Bloom强度: {settings.defaultBloomIntensity}");
            }
            
            if (needsFix)
            {
                Debug.Log($"✓ Volume '{volumeName}' 中的Bloom已修复");
            }
        }
        else
        {
            // 添加Bloom效果
            bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(settings.defaultBloomIntensity);
            bloom.threshold.Override(settings.defaultBloomThreshold);
            bloom.scatter.Override(0.7f);
            
            Debug.Log($"  + 添加Bloom效果 (强度: {settings.defaultBloomIntensity})");
        }
    }
    #endif
    
    void CreateDefaultVolume()
    {
        GameObject volumeObj = new GameObject("Global_Volume_Fixed");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1f;
        volume.priority = 100;
        
        #if USING_URP
        // 创建Profile
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // 添加并配置Bloom
        Bloom bloom = volume.profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(settings.defaultBloomIntensity);
        bloom.threshold.Override(settings.defaultBloomThreshold);
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(Color.white);
        
        // 添加其他常用效果
        ColorAdjustments color = volume.profile.Add<ColorAdjustments>();
        color.active = true;
        color.postExposure.Override(0.1f);
        color.saturation.Override(5f);
        
        Vignette vignette = volume.profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.3f);
        #endif
        
        Debug.Log("已创建包含完整后处理的默认Volume");
    }
    
    System.Collections.IEnumerator VerifyFixAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log("\n=== 验证修复结果 ===");
        
        bool allBloomActive = true;
        Volume[] volumes = FindObjectsOfType<Volume>();
        
        foreach (Volume volume in volumes)
        {
            if (volume.profile != null)
            {
                #if USING_URP
                if (volume.profile.TryGet<Bloom>(out Bloom bloom))
                {
                    if (!bloom.active)
                    {
                        Debug.LogError($"✗ Volume '{volume.name}' 中的Bloom仍然未激活！");
                        allBloomActive = false;
                    }
                    else
                    {
                        Debug.Log($"✓ Volume '{volume.name}' Bloom已激活 (强度: {bloom.intensity.value})");
                    }
                }
                #endif
            }
        }
        
        if (allBloomActive)
        {
            Debug.Log("✓ 所有Volume中的Bloom效果已激活！");
            
            // 创建测试物体验证效果
            CreateTestObject();
        }
        else
        {
            Debug.LogError("✗ 仍有Bloom未激活，需要手动检查！");
        }
    }
    
    void CreateTestObject()
    {
        // 创建高亮测试物体
        GameObject testObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        testObject.name = "Bloom_Test_Object";
        testObject.transform.position = new Vector3(0, 0, 5);
        
        #if USING_URP
        // 使用URP发光材质
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.white * 3f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        testObject.GetComponent<Renderer>().material = material;
        #endif
        
        // 添加旋转
        testObject.AddComponent<Rotator>();
        
        Debug.Log("已创建测试物体，检查Game视图中是否有Bloom效果");
    }
    
    class Rotator : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }
    
    #if UNITY_EDITOR
    [ContextMenu("立即运行修复")]
    void RunFixNow()
    {
        FixAllVolumes();
    }
    #endif
}