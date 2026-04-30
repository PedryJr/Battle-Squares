using UnityEngine;
using UnityEditor;
using UnityEditor.UI; // Required to inherit from ButtonEditor

[CustomEditor(typeof(MartinButtonScript))]
[CanEditMultipleObjects]
public class MartinButtonEditor : ButtonEditor
{
    // SerializedProperties are the "proper" way to handle undo/redo and prefab overrides
    SerializedProperty standardThicknessProp;
    SerializedProperty hoverThicknessProp;
    SerializedProperty borderImageProp;

    protected override void OnEnable()
    {
        base.OnEnable(); // Crucial: ButtonEditor uses this to setup its own properties

        // Link our properties to the variables in MartinButtonScript
        standardThicknessProp = serializedObject.FindProperty("standardOutlineThickness");
        hoverThicknessProp = serializedObject.FindProperty("hoverOutlineThickness");
        borderImageProp = serializedObject.FindProperty("borderImage");
    }

    public override void OnInspectorGUI()
    {
        // 1. Update the serialized object's representation
        serializedObject.Update();

        // 2. Draw your custom fields

        EditorGUILayout.PropertyField(standardThicknessProp);
        EditorGUILayout.PropertyField(hoverThicknessProp);
        EditorGUILayout.PropertyField(borderImageProp);

        EditorGUILayout.Space();

        // 3. Draw the original Button inspector (Transition, OnClick, etc.)
        base.OnInspectorGUI();

        // 4. Apply any changes back to the actual script
        serializedObject.ApplyModifiedProperties();
    }
}