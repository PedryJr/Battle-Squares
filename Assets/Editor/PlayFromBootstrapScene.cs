using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class PlayFromBootstrapScene
{
    private static readonly string bootstrapScenePath = "Assets/Scenes/NetworkScene.unity"; 
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
            Debug.Log($"Saving current scene to EditorPrefs: {currentScenePath}");

            if (currentScenePath != bootstrapScenePath)
            {
                Debug.Log("Switching to Bootstrap scene...");
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(bootstrapScenePath);
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string previousScenePath = EditorPrefs.GetString(PreviousSceneKey, string.Empty);
            Debug.Log($"Exiting Play mode, will return to scene from EditorPrefs: {previousScenePath}");
            if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != bootstrapScenePath)
            {
                Debug.Log($"Returning to previous scene: {previousScenePath}");
                EditorSceneManager.OpenScene(previousScenePath);
            }
            else
            {
                Debug.LogWarning("Previous scene path is invalid or same as Bootstrap scene.");
            }
        }
    }
}