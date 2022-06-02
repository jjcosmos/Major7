using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class Major7EditorTools : UnityEditor.AssetModificationProcessor
{
    [MenuItem("Major7/Force Addressable Paths Reload")]
    private static void ForceRebuildAddressablePaths()
    {
        RebuildAddressablePaths();
    }
    
    [MenuItem("Major7/Build Player With Bundles")]
    private static void BuildPlayer()
    {
        NeedsAddressableRebuild = true;
        BuildAddressables();
        GenerateClipDefinitionsClass();
        var options = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        options.options &= ~BuildOptions.AutoRunPlayer;
        BuildPipeline.BuildPlayer(options);
    }
    
    [MenuItem("Major7/Build And Run Player With Bundles")]
    private static void BuildAndRunPlayer()
    {
        NeedsAddressableRebuild = true;
        BuildAddressables();
        GenerateClipDefinitionsClass();
        var options = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        options.options |= BuildOptions.AutoRunPlayer;
        BuildPipeline.BuildPlayer(options);
    }
    
    [MenuItem("Major7/Build Addressable Player Content")]
    private static void BuildAddressables()
    {
        if (NeedsAddressableRebuild)
        {
            RebuildAddressablePaths();
        }
        
        AddressableAssetSettings.BuildPlayerContent();
    }
    
    [MenuItem("Major7/Generate Clip Definitions")]
    private static void GenerateClipDefinitionsClass()
    {
        EditorUtility.DisplayProgressBar("Generating Clip Definitions & Refreshing Addressable Paths", "working...", 1.0f);
        
        if (NeedsAddressableRebuild)
        {
            RebuildAddressablePaths();
        }
        
        var generatedPath = Path.Combine(Application.dataPath, "Major7", "Generated", "ClipDefinitions.cs");

        if (!File.Exists(generatedPath))
        {
            File.Create(generatedPath).Dispose();
        }

        var audioClipAssetPaths = AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath);

        var builder = new StringBuilder();
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine("using UnityEngine.AddressableAssets;");
        builder.AppendLine("using UnityEngine.ResourceManagement.AsyncOperations;");
        builder.AppendLine();
        builder.AppendLine("public static class ClipDefinitions {");

        foreach (var assetPath in audioClipAssetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (null == asset || !IsAssetAddressable(asset)) continue;
            
            var cleanedStr = Regex.Replace(asset.name, @"[^\w]*", string.Empty);
                
            builder.AppendLine($"\tpublic static AsyncOperationHandle<AudioClip> {cleanedStr} => Addressables.LoadAssetAsync<AudioClip>(\"{assetPath}\");");
        }

        builder.Append("}");

        File.WriteAllText(generatedPath, builder.ToString());
        
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
    }

    private static bool NeedsAddressableRebuild = false;
    private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
    {
        if (null != AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath))
        {
            NeedsAddressableRebuild = true;
        }

        return AssetMoveResult.DidNotMove;
    }
    
    private static bool IsAssetAddressable(Object obj)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
        return entry != null;
    }

    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (NeedsAddressableRebuild)
        {
            RebuildAddressablePaths();
        }
        return paths;
    }

    private static void RebuildAddressablePaths()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (null == settings)
        {
            Debug.LogError("No addressable asset settings object!");
            return;
        }
        
        var groups = settings.groups;
        
        foreach (var group in groups)
        foreach (var entry in group.entries)
        {
            // Don't want to mess with non-audio addressables
            var isAudioClip = null != AssetDatabase.LoadAssetAtPath<AudioClip>(entry.AssetPath);
                
            if (isAudioClip && entry.address != entry.AssetPath)
            {
                entry.address = entry.AssetPath;
            }
        }

        NeedsAddressableRebuild = false;
    }
}
