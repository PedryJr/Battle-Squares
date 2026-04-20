using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class AutoSceneSwitcher : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private float delayInSeconds = 5.0f;

    void Start()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Start the timer
            StartCoroutine(DelayedLoad());
        }
        else
        {
            Debug.LogWarning("AutoSceneSwitcher: No scene name provided in the Inspector.");
        }
    }

    private IEnumerator DelayedLoad()
    {
        // Wait for the specified amount of time
        yield return new WaitForSeconds(delayInSeconds);

        // Switch the scene
        SceneManager.LoadScene(sceneToLoad);
    }
}