using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class LuaHotUpdateSyncTool
{
    private const string SourceLuaDir = "Assets/Lua";
    private const string TargetLuaHotUpdateDir = "Assets/LuaHotUpdate";
    private const string ManifestFileName = "LuaManifest.json";
    private const string LuaAddressPrefix = "Lua";
    private const string AddressableGroupName = "RemoteGroup";
    private const string LocalPrefabGroupName = "Prefab";
    private const string LuaLabel = "lua";
    private const string UiLabel = "UI";
    private const string LuaUpdateUIPrefabPath = "Assets/Prefab/UI/LuaUpdateUI.prefab";
    private const string LuaUpdateUIPrefabAddress = "UI/LuaUpdateUI";

    [MenuItem("Tools/Lua HotUpdate/Sync Lua To LuaHotUpdate")]
    public static void SyncLuaToLuaHotUpdate()
    {
        if (!Directory.Exists(SourceLuaDir))
        {
            EditorUtility.DisplayDialog("Lua 热更同步失败", $"找不到 Lua 开发目录：{SourceLuaDir}", "确定");
            return;
        }

        try
        {
            PrepareTargetDirectory();

            string[] luaFiles = Directory.GetFiles(SourceLuaDir, "*.lua", SearchOption.AllDirectories);
            Array.Sort(luaFiles, StringComparer.OrdinalIgnoreCase);

            List<ManifestEntry> manifestEntries = new List<ManifestEntry>();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                string sourcePath = NormalizeAssetPath(luaFiles[i]);
                string relativePath = GetRelativePath(SourceLuaDir, sourcePath);
                if (string.IsNullOrEmpty(relativePath))
                {
                    continue;
                }

                string targetPath = Path.Combine(TargetLuaHotUpdateDir, relativePath + ".bytes").Replace('\\', '/');
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
                manifestEntries.Add(new ManifestEntry
                {
                    address = $"{LuaAddressPrefix}/{relativePath}",
                    path = relativePath
                });
            }

            WriteManifest(manifestEntries);
            AssetDatabase.Refresh();
            ConfigureAddressables(manifestEntries);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Lua 热更同步完成",
                $"已同步 {manifestEntries.Count} 个 Lua 文件到：{TargetLuaHotUpdateDir}\n\n已自动配置 Addressables Group：{AddressableGroupName}\n已自动设置 Label：{LuaLabel}",
                "确定");

            Debug.Log($"[LuaHotUpdateSyncTool] 已同步 {manifestEntries.Count} 个 Lua 文件到 {TargetLuaHotUpdateDir}，并完成 Addressables 配置。" );
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LuaHotUpdateSyncTool] 同步失败：{exception}");
            EditorUtility.DisplayDialog("Lua 热更同步失败", exception.Message, "确定");
        }
    }

    [MenuItem("Tools/Lua HotUpdate/Clear LuaHotUpdate Directory")]
    public static void ClearLuaHotUpdateDirectory()
    {
        if (!EditorUtility.DisplayDialog("清空 LuaHotUpdate", $"确定要清空目录吗？\n{TargetLuaHotUpdateDir}", "清空", "取消"))
        {
            return;
        }

        DeleteDirectoryIfExists(TargetLuaHotUpdateDir);
        AssetDatabase.Refresh();
        Debug.Log($"[LuaHotUpdateSyncTool] 已清空 {TargetLuaHotUpdateDir}");
    }

    private static void PrepareTargetDirectory()
    {
        DeleteDirectoryIfExists(TargetLuaHotUpdateDir);
        Directory.CreateDirectory(TargetLuaHotUpdateDir);
    }

    private static void DeleteDirectoryIfExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        Directory.Delete(directory, true);

        string metaPath = directory + ".meta";
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }
    }

    private static void WriteManifest(List<ManifestEntry> entries)
    {
        string manifestPath = Path.Combine(TargetLuaHotUpdateDir, ManifestFileName).Replace('\\', '/');
        string version = ReadLuaVersion();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"version\": \"{EscapeJson(version)}\",");
        builder.AppendLine("  \"files\": [");

        for (int i = 0; i < entries.Count; i++)
        {
            ManifestEntry entry = entries[i];
            builder.AppendLine("    {");
            builder.AppendLine($"      \"address\": \"{EscapeJson(entry.address)}\",");
            builder.AppendLine($"      \"path\": \"{EscapeJson(entry.path)}\"");
            builder.Append("    }");
            if (i < entries.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        File.WriteAllText(manifestPath, builder.ToString(), new UTF8Encoding(false));
    }

    private static void ConfigureAddressables(List<ManifestEntry> entries)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            throw new InvalidOperationException("找不到 Addressables Settings，请先初始化 Addressables。" );
        }

        AddressableAssetGroup group = settings.FindGroup(AddressableGroupName);
        if (group == null)
        {
            throw new InvalidOperationException($"找不到 Addressables Group：{AddressableGroupName}。" );
        }

        settings.AddLabel(LuaLabel);
        settings.AddLabel(UiLabel);
        RemoveLuaHotUpdateEntries(settings);
        ConfigureLuaUpdateUIPrefab(settings);

        AddOrMoveAddressableAsset(settings, group, GetManifestPath(), $"{LuaAddressPrefix}/{ManifestFileName}");
        for (int i = 0; i < entries.Count; i++)
        {
            ManifestEntry entry = entries[i];
            string assetPath = GetHotUpdateLuaAssetPath(entry.path);
            AddOrMoveAddressableAsset(settings, group, assetPath, entry.address);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
        Debug.Log($"[LuaHotUpdateSyncTool] 已配置 Addressables：Group={AddressableGroupName}, Label={LuaLabel}, 文件数={entries.Count + 1}");
    }

    private static void AddOrMoveAddressableAsset(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
    {
        AddOrMoveAddressableAsset(settings, group, assetPath, address, LuaLabel);
    }

    private static void AddOrMoveAddressableAsset(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address, string label)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            throw new FileNotFoundException($"找不到要配置为 Addressable 的资源：{assetPath}");
        }

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.SetAddress(address, false);
        entry.SetLabel(label, true, true, false);
    }

    private static void ConfigureLuaUpdateUIPrefab(AddressableAssetSettings settings)
    {
        if (!File.Exists(LuaUpdateUIPrefabPath))
        {
            Debug.LogWarning($"[LuaHotUpdateSyncTool] LuaUpdateUI prefab not found: {LuaUpdateUIPrefabPath}");
            return;
        }

        AddressableAssetGroup localPrefabGroup = settings.FindGroup(LocalPrefabGroupName);
        if (localPrefabGroup == null)
        {
            Debug.LogWarning($"[LuaHotUpdateSyncTool] Addressables group not found: {LocalPrefabGroupName}");
            return;
        }

        AddOrMoveAddressableAsset(settings, localPrefabGroup, LuaUpdateUIPrefabPath, LuaUpdateUIPrefabAddress, UiLabel);
    }

    private static void RemoveLuaHotUpdateEntries(AddressableAssetSettings settings)
    {
        List<string> removeGuids = new List<string>();
        for (int i = 0; i < settings.groups.Count; i++)
        {
            AddressableAssetGroup group = settings.groups[i];
            if (group == null)
            {
                continue;
            }

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.AssetPath))
                {
                    continue;
                }

                string assetPath = NormalizeAssetPath(entry.AssetPath);
                if (assetPath.Equals(TargetLuaHotUpdateDir, StringComparison.OrdinalIgnoreCase) ||
                    assetPath.StartsWith(TargetLuaHotUpdateDir + "/", StringComparison.OrdinalIgnoreCase))
                {
                    removeGuids.Add(entry.guid);
                }
            }
        }

        for (int i = 0; i < removeGuids.Count; i++)
        {
            settings.RemoveAssetEntry(removeGuids[i], false);
        }
    }

    private static string GetManifestPath()
    {
        return Path.Combine(TargetLuaHotUpdateDir, ManifestFileName).Replace('\\', '/');
    }

    private static string GetHotUpdateLuaAssetPath(string relativePath)
    {
        return Path.Combine(TargetLuaHotUpdateDir, relativePath + ".bytes").Replace('\\', '/');
    }

    private static string ReadLuaVersion()
    {
        string mainLuaPath = Path.Combine(SourceLuaDir, "Main.lua").Replace('\\', '/');
        if (!File.Exists(mainLuaPath))
        {
            return "0.0.0";
        }

        string content = File.ReadAllText(mainLuaPath, Encoding.UTF8);
        Match match = Regex.Match(content, "\\bVERSION\\s*=\\s*[\"'](?<version>[^\"']+)[\"']", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["version"].Value.Trim() : "0.0.0";
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        string normalizedRoot = NormalizeAssetPath(root).TrimEnd('/') + "/";
        string normalizedFullPath = NormalizeAssetPath(fullPath);
        if (!normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalizedFullPath.Substring(normalizedRoot.Length);
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    private class ManifestEntry
    {
        public string address;
        public string path;
    }
}
