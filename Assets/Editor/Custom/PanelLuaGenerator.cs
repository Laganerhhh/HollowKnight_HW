using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PanelLuaGenerator
{
    private const string LuaViewDirectory = "Assets/Lua/View";
    private const string LuaLogicDirectory = "Assets/Lua/Logic";
    private const string PanelRegistryPath = "Assets/Lua/Logic/PanelRegistry.lua";

    [MenuItem("Assets/创建Panel Lua", false, 2000)]
    private static void CreatePanelLua()
    {
        string prefabAssetPath = GetSelectedPrefabPath();
        if (string.IsNullOrEmpty(prefabAssetPath))
        {
            EditorUtility.DisplayDialog("创建Panel Lua", "请选择一个Prefab资源。", "确定");
            return;
        }

        string panelName = Path.GetFileNameWithoutExtension(prefabAssetPath);
        string luaFilePath = $"{LuaViewDirectory}/{panelName}.lua";
        string prefabLoadPath = BuildPrefabLoadPath(prefabAssetPath);

        Directory.CreateDirectory(LuaViewDirectory);
        Directory.CreateDirectory(LuaLogicDirectory);

        bool luaExists = File.Exists(luaFilePath);
        if (luaExists)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "创建Panel Lua",
                $"{panelName}.lua 已存在，是否覆盖为最新模板？",
                "覆盖",
                "取消");

            if (!overwrite)
            {
                RebuildPanelRegistry();
                AssetDatabase.Refresh();
                return;
            }
        }

        File.WriteAllText(luaFilePath, BuildPanelLuaContent(panelName, prefabLoadPath), new UTF8Encoding(false));
        RebuildPanelRegistry();
        AssetDatabase.Refresh();

        UnityEngine.Object luaAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(luaFilePath);
        if (luaAsset != null)
        {
            Selection.activeObject = luaAsset;
            EditorGUIUtility.PingObject(luaAsset);
        }

        Debug.Log($"[PanelLuaGenerator] 已生成 {luaFilePath}，并更新 PanelRegistry.lua");
    }

    [MenuItem("Assets/创建Panel Lua", true)]
    private static bool ValidateCreatePanelLua()
    {
        return !string.IsNullOrEmpty(GetSelectedPrefabPath());
    }

    [MenuItem("Lua/Rebuild Panel Registry", false, 58)]
    private static void RebuildPanelRegistryMenu()
    {
        Directory.CreateDirectory(LuaLogicDirectory);
        RebuildPanelRegistry();
        AssetDatabase.Refresh();
        Debug.Log("[PanelLuaGenerator] 已重新生成 PanelRegistry.lua");
    }

    private static string GetSelectedPrefabPath()
    {
        if (Selection.activeObject == null)
        {
            return null;
        }

        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return assetPath.Replace('\\', '/');
    }

    private static string BuildPrefabLoadPath(string prefabAssetPath)
    {
        string normalizedPath = Path.ChangeExtension(prefabAssetPath, null).Replace('\\', '/');

        if (normalizedPath.StartsWith("Assets/Prefab/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath.Substring("Assets/Prefab/".Length);
        }

        if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath.Substring("Assets/".Length);
        }

        return Path.GetFileNameWithoutExtension(prefabAssetPath);
    }

    private static string BuildPanelLuaContent(string panelName, string prefabLoadPath)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("local BasePanel = require \"View.BasePanel\"");
        builder.AppendLine("local ManagerRegistry = require \"Logic.ManagerRegistry\"");
        builder.AppendLine();
        builder.AppendLine($"local {panelName} = BasePanel.New()");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}.New()");
        builder.AppendLine($"\treturn {panelName}:CreateInstance()");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:Ctor()");
        builder.AppendLine($"\tself.panelName = \"{panelName}\"");
        builder.AppendLine($"\tself.prefabPath = \"{prefabLoadPath}\"");
        builder.AppendLine("\tself.gameObject = nil");
        builder.AppendLine("\tself.transform = nil");
        builder.AppendLine("\tself.resourceManager = nil");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:InitUIAndMetaData()");
        builder.AppendLine("\tself.resourceManager = self.resourceManager or ManagerRegistry.Resource()");
        builder.AppendLine();
        builder.AppendLine("\tif self.gameObject == nil and self.resourceManager ~= nil then");
        builder.AppendLine("\t\tself.gameObject = self.resourceManager:InstantiatePrefab(self.prefabPath)");
        builder.AppendLine("\tend");
        builder.AppendLine();
        builder.AppendLine("\tif self.gameObject ~= nil then");
        builder.AppendLine("\t\tself.transform = self.gameObject.transform");
        builder.AppendLine("\t\tself.ui.root = self.transform");
        builder.AppendLine("\tend");
        builder.AppendLine();
        builder.AppendLine("\tself.meta.prefabPath = self.prefabPath");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:InitUIEvent()");
        builder.AppendLine("\t-- 在这里绑定按钮、Toggle、列表项点击等事件。");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:OnOpen(data)");
        builder.AppendLine("\tself.state.openParam = data");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:RefreshView(data)");
        builder.AppendLine("\tif self.gameObject ~= nil then");
        builder.AppendLine("\t\tself.gameObject:SetActive(true)");
        builder.AppendLine("\tend");
        builder.AppendLine();
        builder.AppendLine("\t-- 在这里根据 data 或 self.state 刷新文本、图片、列表和显隐状态。");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:OnHide()");
        builder.AppendLine("\tif self.gameObject ~= nil then");
        builder.AppendLine("\t\tself.gameObject:SetActive(false)");
        builder.AppendLine("\tend");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"function {panelName}:OnDispose()");
        builder.AppendLine("\tif self.gameObject ~= nil and self.resourceManager ~= nil then");
        builder.AppendLine("\t\tself.resourceManager:ReleaseInstance(self.gameObject)");
        builder.AppendLine("\t\tself.gameObject = nil");
        builder.AppendLine("\tend");
        builder.AppendLine();
        builder.AppendLine("\tself.transform = nil");
        builder.AppendLine("\tself.resourceManager = nil");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine($"return {panelName}");
        return builder.ToString();
    }

    private static void RebuildPanelRegistry()
    {
        Directory.CreateDirectory(LuaViewDirectory);
        Directory.CreateDirectory(LuaLogicDirectory);

        string[] panelFiles = Directory.GetFiles(LuaViewDirectory, "*Panel.lua", SearchOption.TopDirectoryOnly)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !path.EndsWith("/BasePanel.lua", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("local UIPanelManager = require \"Logic.UIPanelManager\"");
        builder.AppendLine();
        builder.AppendLine("local definitions = {");

        foreach (string panelFile in panelFiles)
        {
            string panelName = Path.GetFileNameWithoutExtension(panelFile);
            builder.AppendLine($"\t{{ name = \"{panelName}\", module = require \"View.{panelName}\" }},");
        }

        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("PanelRegistry = PanelRegistry or {};");
        builder.AppendLine();
        builder.AppendLine("function PanelRegistry.RegisterAll()");
        builder.AppendLine("\tfor _, definition in ipairs(definitions) do");
        builder.AppendLine("\t\tif UIPanelManager.Get(definition.name) == nil and definition.module and definition.module.New then");
        builder.AppendLine("\t\t\tUIPanelManager.Register(definition.name, definition.module.New())");
        builder.AppendLine("\t\t\tprint(\"[Lua] PanelRegistry.Register:\", definition.name)");
        builder.AppendLine("\t\tend");
        builder.AppendLine("\tend");
        builder.AppendLine("end");
        builder.AppendLine();
        builder.AppendLine("return PanelRegistry");

        File.WriteAllText(PanelRegistryPath, builder.ToString(), new UTF8Encoding(false));
    }
}
