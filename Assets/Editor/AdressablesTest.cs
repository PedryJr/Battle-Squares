/*#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

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
            EditorGUILayout.HelpBox("Select a WeaponBuilder ScriptableObject.", MessageType.Info);
            return;
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Build Content", GUILayout.Height(30))) BuildWeaponBuilderBundle(selectedAsset);

        GUILayout.Space(10);
    }

    private void BuildWeaponBuilderBundle(WeaponBuilder asset)
    {
        if (asset == null)
        {
            Debug.LogError("No WeaponBuilder asset selected.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);

        Object[] dependencies = EditorUtility.CollectDependencies(new Object[] { asset });
        var assetDependencies = dependencies
            .Where(o => o != null)
            .Where(o => !(o is MonoScript))
            .ToArray();

        string bundleFolder = Path.Combine(Application.dataPath, "../Mods");
        if (!Directory.Exists(bundleFolder)) Directory.CreateDirectory(bundleFolder);

        string bundleName = asset.WeaponName + ".bsm";
        string bundleFullPath = Path.Combine(bundleFolder, bundleName);

        if (File.Exists(bundleFullPath)) File.Delete(bundleFullPath);

        string[] assetPaths = assetDependencies
            .Select(o => AssetDatabase.GetAssetPath(o))
            .Distinct()
            .ToArray();

        AssetBundleBuild buildMap = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = assetPaths
        };

        BuildPipeline.BuildAssetBundles(bundleFolder,
            new AssetBundleBuild[] { buildMap },
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);

        Debug.Log($"Build complete. Fully-contained bundle: {bundleFullPath}");
    }
}
#endif
*/