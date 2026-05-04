using UnityEngine;
using UnityEngine.UIElements;
using System;
using Unity.Collections;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(GameSimCaptureBehaviour))]
public class GameSimCaptureBehaviourEditor : Editor
{

    SerializedProperty simSpeed;
    SerializedProperty captureDestination;

    Texture2D progressCol;

    void OnEnable()
    {
        simSpeed = serializedObject.FindProperty("simSpeed");
        captureDestination = serializedObject.FindProperty("captureDestination");
        progressCol = new Texture2D(1, 1);
        progressCol.SetPixel(0, 0, Color.aliceBlue);
        progressCol.wrapMode = TextureWrapMode.Repeat;
        progressCol.Apply();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GameSimCaptureBehaviour behaviour = (GameSimCaptureBehaviour) target;
        if (!behaviour)
        {
            Debug.LogWarning("Opps, behaviour is null/invalid");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Space(50);
        behaviour.simSpeed = EditorGUILayout.Slider("Sim Speed", simSpeed.floatValue, 0f, 1f);
        GUIStyle butstyl = GUI.skin.button;
        butstyl.fixedHeight = 25;
        GUILayout.Space(50);
        GUILayout.EndHorizontal();

        //Button block
        GUILayout.BeginHorizontal();
        GUILayout.Space(50);
        if (GUILayout.Button("<-", butstyl)) behaviour.DecrementSimSpeed();
        GUILayout.Space(25);
        if (GUILayout.Button("0", butstyl)) behaviour.AddToSimSpeed(-1);
        GUILayout.Space(25);
        if (GUILayout.Button("1", butstyl)) behaviour.AddToSimSpeed(1);
        GUILayout.Space(25);
        if (GUILayout.Button("->", butstyl)) behaviour.IncrementSimSpeed();
        GUILayout.Space(50);
        GUILayout.EndHorizontal();

        //Directory location block
        GUILayout.Space(50);
        GUILayout.BeginHorizontal();
        GUILayout.Space(50);
        EditorGUILayout.LabelField("Screen shot directory:");
        behaviour.captureDestination = EditorGUILayout.TextField(captureDestination.stringValue);
        GUILayout.Space(50);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(50);
        if (GUILayout.Button("CAPTURE", butstyl)) behaviour.CaptureScreen();
        GUILayout.Space(50);
        GUILayout.EndHorizontal();
    }
}

#endif