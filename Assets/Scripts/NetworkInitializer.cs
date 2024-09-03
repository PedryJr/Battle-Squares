using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NetworkInitializer : MonoBehaviour
{

    private void Start()
    {

        Invoke("InvokedSceneInitialization", 0.05f);

    }

    void InvokedSceneInitialization()
    {

        SceneManager.LoadScene("MenuScene");

    }

}
