// CreateBlurMaterials.cs - 编辑器扩展
using UnityEngine;
using UnityEditor;

public class CreateBlurMaterials : MonoBehaviour
{
    [MenuItem("Assets/空洞骑士工具/创建背景模糊材质")]
    static void CreateBackgroundBlurMaterial()
    {
        // 创建Shader实例
        Shader blurShader = Shader.Find("Custom/HollowKnightBackgroundBlur");
        if (blurShader == null)
        {
            Debug.LogError("请先编译Shader！");
            return;
        }
        
        // 创建材质
        Material material = new Material(blurShader);
        material.name = "Background_Blur_Material";
        
        // 设置默认参数
        material.SetFloat("_BlurRadius", 0.01f);
        material.SetFloat("_ColorDepth", 8f);
        material.SetFloat("_EdgeDarken", 0.3f);
        material.SetFloat("_Darkness", 0.5f);
        
        // 创建噪点纹理（如果没有）
        string noiseTexPath = "Assets/Textures/BackgroundNoise.asset";
        Texture2D noiseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(noiseTexPath);
        if (noiseTex == null)
        {
            noiseTex = CreateNoiseTexture(128, 128);
            AssetDatabase.CreateAsset(noiseTex, noiseTexPath);
            AssetDatabase.SaveAssets();
        }
        material.SetTexture("_NoiseTex", noiseTex);
        
        // 保存材质
        string path = EditorUtility.SaveFilePanelInProject(
            "保存模糊材质",
            "Background_Blur_Material.mat",
            "mat",
            "选择保存位置");
            
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 选中新创建的材质
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
    
    static Texture2D CreateNoiseTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;
        
        Color[] colors = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noise = Random.Range(0.45f, 0.55f);
                colors[y * width + x] = new Color(noise, noise, noise, 1);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}