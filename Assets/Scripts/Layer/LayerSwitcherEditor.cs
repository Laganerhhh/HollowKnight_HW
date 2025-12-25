using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LayerSwitcherEditor : EditorWindow
{
    private static Dictionary<string, LayerSettings> layerSettings;
    private Vector2 scrollPos;
    private bool showAdvancedSettings = false;

    [System.Serializable]
    public class LayerSettings
    {
        public string sortingLayer = "Default";
        public int orderInLayer = 0;
        public Color tintColor = Color.white;
        public Material material;
        public float darkness = 0.3f;
        public bool hasCollider = false;
        public bool isParallax = false;
        public float parallaxFactor = 0.5f;
        public string tag = "Untagged";
    }

    [MenuItem("Window/图层切换工具 %#L")]
    public static void ShowWindow()
    {
        GetWindow<LayerSwitcherEditor>("图层切换工具");
    }

    private void OnEnable()
    {
        InitializeSettings();
        Selection.selectionChanged += Repaint;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
    }

    private static void InitializeSettings()
    {
        if (layerSettings == null)
        {
            layerSettings = new Dictionary<string, LayerSettings>
            {
                ["最前黑色层"] = new LayerSettings
                {
                    sortingLayer = "ForegroundBlack",
                    orderInLayer = 100,
                    tintColor = Color.black,
                    darkness = 1.0f
                },
                ["前景层"] = new LayerSettings
                {
                    sortingLayer = "Foreground",
                    orderInLayer = 50,
                    tintColor = Color.white,
                    hasCollider = true,
                    tag = "Ground"
                },
                ["玩家层"] = new LayerSettings
                {
                    sortingLayer = "PlayerLayer",
                    orderInLayer = 0,
                    tintColor = Color.white,
                    hasCollider = true,
                    tag = "Player"
                },
                ["背景层"] = new LayerSettings
                {
                    sortingLayer = "Background",
                    orderInLayer = -50,
                    tintColor = new Color(0.7f, 0.7f, 0.7f, 1f),
                    darkness = 0.3f,
                    isParallax = true,
                    parallaxFactor = 0.7f
                },
                ["最远背景层"] = new LayerSettings
                {
                    sortingLayer = "FarBackground",
                    orderInLayer = -100,
                    tintColor = new Color(0.5f, 0.5f, 0.5f, 1f),
                    darkness = 0.5f,
                    isParallax = true,
                    parallaxFactor = 0.9f
                }
            };
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("图层快速切换工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("请先选择场景中的物体", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 显示当前选择信息
        EditorGUILayout.LabelField($"已选择 {Selection.gameObjects.Length} 个物体", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        // 快速切换按钮
        EditorGUILayout.LabelField("快速切换:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        foreach (var layer in layerSettings.Keys)
        {
            if (GUILayout.Button(layer, GUILayout.Height(30)))
            {
                ApplyLayerSettings(layer);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);

        // 高级设置
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置");
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;

            // 自定义设置
            foreach (var kvp in layerSettings)
            {
                EditorGUILayout.LabelField($"{kvp.Key} 设置:", EditorStyles.boldLabel);

                var settings = kvp.Value;
                settings.sortingLayer = EditorGUILayout.TextField("Sorting Layer", settings.sortingLayer);
                settings.orderInLayer = EditorGUILayout.IntField("Order in Layer", settings.orderInLayer);
                settings.tintColor = EditorGUILayout.ColorField("颜色", settings.tintColor);
                settings.darkness = EditorGUILayout.Slider("变暗程度", settings.darkness, 0f, 1f);
                settings.hasCollider = EditorGUILayout.Toggle("添加碰撞器", settings.hasCollider);
                settings.isParallax = EditorGUILayout.Toggle("视差效果", settings.isParallax);
                settings.parallaxFactor = EditorGUILayout.Slider("视差系数", settings.parallaxFactor, 0f, 1f);
                settings.tag = EditorGUILayout.TagField("标签", settings.tag);

                if (GUILayout.Button($"保存 {kvp.Key} 预设"))
                {
                    SavePreset(kvp.Key, settings);
                }

                EditorGUILayout.Space(10);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();

        // 批量操作
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("批量操作:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("重置所有预设"))
        {
            InitializeSettings();
        }

        if (GUILayout.Button("导出预设为JSON"))
        {
            ExportSettingsToJson();
        }

        if (GUILayout.Button("从JSON导入预设"))
        {
            ImportSettingsFromJson();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ApplyLayerSettings(string layerName)
    {
        if (!layerSettings.ContainsKey(layerName)) return;

        var settings = layerSettings[layerName];
        Undo.RecordObjects(Selection.gameObjects, $"应用 {layerName} 设置");

        foreach (GameObject obj in Selection.gameObjects)
        {
            ApplySettingsToObject(obj, settings);
        }

        EditorUtility.DisplayDialog("完成", $"已将 {Selection.gameObjects.Length} 个物体设置为 {layerName}", "确定");
    }

    private void ApplySettingsToObject(GameObject obj, LayerSettings settings)
    {
        // 获取或添加SpriteRenderer
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = obj.AddComponent<SpriteRenderer>();
        }

        // 应用渲染设置
        sr.sortingLayerName = settings.sortingLayer;
        sr.sortingOrder = settings.orderInLayer;
        
        // 应用颜色（考虑变暗）
        Color finalColor = settings.tintColor * (1 - settings.darkness);
        finalColor.a = 1;
        sr.color = finalColor;

        // 设置标签
        obj.tag = settings.tag;

        // 处理碰撞器
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (settings.hasCollider && collider == null)
        {
            obj.AddComponent<BoxCollider2D>();
        }
        else if (!settings.hasCollider && collider != null)
        {
            DestroyImmediate(collider);
        }

        // 处理视差组件
        ParallaxEffect parallax = obj.GetComponent<ParallaxEffect>();
        if (settings.isParallax)
        {
            if (parallax == null)
            {
                parallax = obj.AddComponent<ParallaxEffect>();
            }
            parallax.parallaxFactor = settings.parallaxFactor;
        }
        else if (parallax != null)
        {
            DestroyImmediate(parallax);
        }

        // 设置材质（如果需要）
        if (settings.material != null)
        {
            sr.material = settings.material;
        }

        EditorUtility.SetDirty(obj);
    }

    private void SavePreset(string presetName, LayerSettings settings)
    {
        string path = EditorUtility.SaveFilePanel("保存预设", "Assets/", presetName, "asset");
        if (!string.IsNullOrEmpty(path))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
            ScriptableObject preset = ScriptableObject.CreateInstance<LayerPreset>();
            //EditorUtility.CopySerialized(settings, preset);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"预设已保存到: {path}");
        }
    }

    private void ExportSettingsToJson()
    {
        string json = JsonUtility.ToJson(new LayerSettingsWrapper { settings = layerSettings });
        string path = EditorUtility.SaveFilePanel("导出JSON", Application.dataPath, "LayerPresets", "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"设置已导出到: {path}");
        }
    }

    private void ImportSettingsFromJson()
    {
        string path = EditorUtility.OpenFilePanel("导入JSON", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = System.IO.File.ReadAllText(path);
            layerSettings = JsonUtility.FromJson<Dictionary<string, LayerSettings>>(json);
            Debug.Log("设置已从JSON导入");
        }
    }

    [System.Serializable]
    public class LayerSettingsWrapper
    {
        public Dictionary<string, LayerSettings> settings;
    }
}