using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class PlayFromBootstrapScene : Editor
{
    private static readonly string bootstrapScenePath = "Assets/Scenes/NetworkScene/NetworkScene.unity";
    private const string PreviousSceneKey = "PreviousScenePath";

    static PlayFromBootstrapScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string currentScenePath = SceneManager.GetActiveScene().path;
            EditorPrefs.SetString(PreviousSceneKey, currentScenePath);

            if (currentScenePath != bootstrapScenePath)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(bootstrapScenePath);
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string previousScenePath = EditorPrefs.GetString(PreviousSceneKey, string.Empty);
            if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != bootstrapScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath);
            }
        }
    }
}