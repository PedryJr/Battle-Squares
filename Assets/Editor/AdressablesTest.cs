#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SDKContentBuilder : EditorWindow
{
    private WeaponBuilder selectedAsset;

    [MenuItem("BattleSquares_SDK/Content Builder")]
    public static void OpenWindow()
    {
        var window = GetWindow<SDKContentBuilder>();
        window.titleContent = new GUIContent("SDK Content Builder");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("SDK Content Builder", EditorStyles.boldLabel);
        GUILayout.Space(5);

        selectedAsset = (WeaponBuilder)EditorGUILayout.ObjectField(
            "Content Asset",
            selectedAsset,
            typeof(WeaponBuilder),
            false
        );

        if (selectedAsset == null)
        {
            EditorGUILayout.HelpBox("Select a BuildableContent ScriptableObject.", MessageType.Info);
            return;
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Build Content", GUILayout.Height(30))) BuildContent(selectedAsset);
    }


    private void BuildContent(WeaponBuilder asset)
    {
        if (asset == null)
        {
            Debug.LogError("No asset selected for build.");
            return;
        }

        // Ensure Mods folder exists
        string modsPath = Path.Combine(Application.dataPath, "Mods");
        if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);

        // Create a temporary Addressable group
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found!");
            return;
        }

        string groupName = "TempSDKBuildGroup";
        AddressableAssetGroup tempGroup = settings.FindGroup(groupName);
        if (tempGroup == null)
            tempGroup = settings.CreateGroup(groupName, false, false, false, settings.DefaultGroup.Schemas);

        // Add asset entry to the group
        string assetPath = AssetDatabase.GetAssetPath(asset);
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), tempGroup);
        entry.address = asset.name;
        AddDependencies(settings, tempGroup, asset);

        // Optional: Use default settings for bundle
        var schema = tempGroup.GetSchema<BundledAssetGroupSchema>();
        if (schema != null)
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether; // or PackTogether

        // Build Addressables
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("Addressable build failed: " + result.Error);
            return;
        }

        // Find generated bundle file
        string bundleFileName = Path.GetFileName(result.OutputPath); // e.g., defaultlocalgroup_assets_*.bundle
        string bundlePath = Path.Combine(Addressables.RuntimePath, bundleFileName);

        if (!File.Exists(bundlePath))
        {
            Debug.LogError("Bundle not found at path: " + bundlePath);
            return;
        }

        // Copy the bundle to Mods folder as .bsm
        string modFilePath = Path.Combine(modsPath, asset.name + ".bsm");
        File.Copy(bundlePath, modFilePath, true);

        Debug.Log($"Content build complete: {modFilePath}");

        // Cleanup temporary group
        settings.RemoveGroup(tempGroup);
        AssetDatabase.SaveAssets();
    }

    private void AddDependencies(AddressableAssetSettings settings, AddressableAssetGroup group, ScriptableObject asset)
    {
        HashSet<UnityEngine.Object> visited = new HashSet<UnityEngine.Object>();
        AddDependenciesRecursive(settings, group, asset, visited);
    }

    private void AddDependenciesRecursive(AddressableAssetSettings settings, AddressableAssetGroup group, UnityEngine.Object obj, HashSet<UnityEngine.Object> visited)
    {
        if (obj == null || visited.Contains(obj))
            return;

        visited.Add(obj);

        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
        {
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = obj.name;
        }

        var type = obj.GetType();

        // Only iterate ScriptableObjects or Serializable classes
        if (!typeof(ScriptableObject).IsAssignableFrom(type) && !type.IsSerializable)
            return;

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var value = field.GetValue(obj);
            if (value == null) continue;

            if (value is UnityEngine.Object uObj)
            {
                // Skip self-reference
                if (uObj == obj) continue;

                AddDependenciesRecursive(settings, group, uObj, visited);
            }
            else if (value is Array arr)
            {
                foreach (var element in arr)
                {
                    if (element is UnityEngine.Object elementObj)
                    {
                        if (elementObj == obj) continue;
                        AddDependenciesRecursive(settings, group, elementObj, visited);
                    }
                }
            }
            else if (field.FieldType.IsClass)
            {
                AddDependenciesRecursive(settings, group, value as UnityEngine.Object, visited);
            }
        }
    }

    /*    private void BuildContent(WeaponBuilder asset)
        {
            //Use unity adressables build system to create a mod file for the game.
            //This is part of a WIP SDK, Its meant to be used in isolated unity editors to create additional weapons for the game.
            //WeaponBuilder is a Scriptable Object used by the game to create weapons automatically.
            //Internal system is very simple, where during bootstrap, every WeaponBuilder in the game is assigned to a Dictionary<ushort, WeaponBuilder>
            //Where the key is a unique ID generated by WeaponBuilder itself.
            //This SDK tool will generate .bsm (Battle Squares Mod) files that the modding user can put in the games Mods directory to add
            //their custom weapons to the game.
        }*/
}
#endif
