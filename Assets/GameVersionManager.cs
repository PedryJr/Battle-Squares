using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "GameVersion", menuName = "Config/Game Version")]
public class GameVersion : ScriptableObject
{
    public string version = "1.0.0";
}

#if UNITY_EDITOR

[CustomEditor(typeof(GameVersion))]
public class GameVersionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameVersion versionScript = (GameVersion)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Version Control", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Increment Major"))
        {
            versionScript.version = IncrementVersion(versionScript.version, 0);
            EditorUtility.SetDirty(versionScript);
        }
        if (GUILayout.Button("Decrement Major"))
        {
            versionScript.version = DecrementVersion(versionScript.version, 0);
            EditorUtility.SetDirty(versionScript);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Increment Minor"))
        {
            versionScript.version = IncrementVersion(versionScript.version, 1);
            EditorUtility.SetDirty(versionScript);
        }
        if (GUILayout.Button("Decrement Minor"))
        {
            versionScript.version = DecrementVersion(versionScript.version, 1);
            EditorUtility.SetDirty(versionScript);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Increment Patch"))
        {
            versionScript.version = IncrementVersion(versionScript.version, 2);
            EditorUtility.SetDirty(versionScript);
        }
        if (GUILayout.Button("Decrement Patch"))
        {
            versionScript.version = DecrementVersion(versionScript.version, 2);
            EditorUtility.SetDirty(versionScript);
        }
        EditorGUILayout.EndHorizontal();
    }

    private string IncrementVersion(string currentVersion, int index)
    {
        string[] parts = currentVersion.Split('.');
        int number = int.Parse(parts[index]);
        number++;
        parts[index] = number.ToString();
        return string.Join(".", parts);
    }

    private string DecrementVersion(string currentVersion, int index)
    {
        string[] parts = currentVersion.Split('.');
        int number = int.Parse(parts[index]);
        number = Mathf.Max(0, number - 1);
        parts[index] = number.ToString();
        return string.Join(".", parts);
    }
}
#endif