using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimationCurveCreatorTest))]
public class AnimationCurveCreatorTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Curve JSON", EditorStyles.boldLabel);

        AnimationCurveCreatorTest targetScript =
            (AnimationCurveCreatorTest)target;

        if (GUILayout.Button("Export Curve to JSON"))
        {
            targetScript.ExportJson();
        }

        if (GUILayout.Button("Import Curve from JSON"))
        {
            targetScript.ImportJson();
        }
    }
}
