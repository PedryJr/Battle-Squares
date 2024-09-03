using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildConfigurator : EditorWindow
{
    private const string BuildConfigKey = "IsReleaseBuild";

    [MenuItem("Tools/Build Configurator")]
    public static void ShowWindow()
    {
        GetWindow<BuildConfigurator>("Build Configurator");
    }

    private void OnGUI()
    {
        bool isReleaseBuild = EditorPrefs.GetBool(BuildConfigKey, false);

        GUILayout.Label("Build Configuration", EditorStyles.boldLabel);

        // Toggle between Test and Release build configurations
        isReleaseBuild = EditorGUILayout.Toggle("Release Build", isReleaseBuild);
        EditorPrefs.SetBool(BuildConfigKey, isReleaseBuild);

        if (GUILayout.Button("Apply Build Settings"))
        {
            ApplyBuildSettings(isReleaseBuild);
        }

        if (GUILayout.Button("Build Now"))
        {
            BuildPlayer(isReleaseBuild);
        }
    }

    private void ApplyBuildSettings(bool isReleaseBuild)
    {
        if (isReleaseBuild)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.High);
            EditorPrefs.SetString("BuildLocation", @"C:\Users\emild\Desktop\QA Tool\Battle Squares\BuildRelease");
        }
        else
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
            EditorPrefs.SetString("BuildLocation", @"C:\Users\emild\Desktop\QA Tool\Battle Squares\Build");
        }

        Debug.Log($"Applied {(isReleaseBuild ? "Release" : "Test")} Build Settings");
        Debug.Log($"Build target location: {EditorPrefs.GetString("BuildLocation")}");
    }

    private void BuildPlayer(bool isReleaseBuild)
    {
        string buildLocation = EditorPrefs.GetString("BuildLocation");
        string[] levels = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = Path.Combine(buildLocation, "Battle Squares.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log($"Building player to: {buildPlayerOptions.locationPathName}");
    }
}
