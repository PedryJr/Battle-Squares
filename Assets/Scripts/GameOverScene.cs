using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverScene : MonoBehaviour
{

    float timer;

    bool loaded = true;

    private void Update()
    {

        if (!loaded) return;

        timer += Time.deltaTime;

        if(timer >= 1) loaded = false;

        if (!loaded && NetworkManager.Singleton.IsHost) SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Single);

    }

}
