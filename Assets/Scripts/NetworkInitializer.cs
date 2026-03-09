using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NetworkInitializer : MonoBehaviour
{

    private void Start()
    {
        StartCoroutine(InvokedSceneInitialization());
    }

    IEnumerator InvokedSceneInitialization()
    {
        AsyncOperation asyncLoadMenu = SceneManager.LoadSceneAsync("MenuScene");
        asyncLoadMenu.allowSceneActivation = false;

        yield return new WaitForSeconds(0.2f);

        while (!asyncLoadMenu.isDone)
        {
            if (asyncLoadMenu.progress >= 0.9f)
            {
                asyncLoadMenu.allowSceneActivation = true;
            }
            yield return null;
        }
    }

}
