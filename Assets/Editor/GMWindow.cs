using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GMWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string[] sceneNames = new string[0];

    [MenuItem("Tools/GM 工具")]
    public static void Open()
    {
        GMWindow window = GetWindow<GMWindow>("GM 工具");
        window.minSize = new Vector2(360f, 420f);
        window.RefreshSceneList();
    }

    private void OnEnable()
    {
        RefreshSceneList();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnFocus()
    {
        RefreshSceneList();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    private void RefreshSceneList()
    {
        string[] scenePaths = Directory.GetFiles(Application.dataPath + "/Scenes", "*.unity", SearchOption.AllDirectories);
        sceneNames = new string[scenePaths.Length];
        for (int i = 0; i < scenePaths.Length; i++)
        {
            sceneNames[i] = Path.GetFileNameWithoutExtension(scenePaths[i]);
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(8f);
        EditorGUILayout.LabelField("GM 工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("用于测试阶段快速切场景和切换主角状态。建议在播放模式下使用运行时功能。", MessageType.Info);

        bool isPlaying = Application.isPlaying;
        string activeSceneName = SceneManager.GetActiveScene().name;

        using (new EditorGUI.DisabledScope(!isPlaying))
        {
            GMService service = GMService.EnsureInstance();
            PlayerHealth playerHealth = service.FindPlayerHealth();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行时状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前场景", string.IsNullOrEmpty(activeSceneName) ? "无" : activeSceneName);
            EditorGUILayout.LabelField("玩家状态", playerHealth != null ? "已找到" : "未找到");
            EditorGUILayout.LabelField("无敌状态", service.InvincibleEnabled ? "开启" : "关闭");

            EditorGUILayout.Space();
            bool invincible = EditorGUILayout.Toggle("主角无敌", service.InvincibleEnabled);
            if (invincible != service.InvincibleEnabled)
            {
                service.SetInvincible(invincible);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("切换无敌", GUILayout.Height(28f)))
            {
                service.ToggleInvincible();
            }
            if (GUILayout.Button("主角满血", GUILayout.Height(28f)))
            {
                service.HealPlayerToFull();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("传送到安全点", GUILayout.Height(28f)))
            {
                service.ResetPlayerToSafePosition();
            }
            if (GUILayout.Button("传送到原点", GUILayout.Height(28f)))
            {
                service.MovePlayerToOrigin();
            }
            EditorGUILayout.EndHorizontal();
        }

        if (!isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("当前不在播放模式。可以先查看场景列表，播放后再使用运行时 GM 功能。", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("场景列表", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新", GUILayout.Width(80f)))
        {
            RefreshSceneList();
        }
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < sceneNames.Length; i++)
        {
            string sceneName = sceneNames[i];
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(sceneName);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("切换到此场景", GUILayout.Width(120f)))
                {
                    GMService.EnsureInstance().LoadScene(sceneName);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}