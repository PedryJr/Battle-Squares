using UnityEngine;

public class InactivityBehaviour : MonoBehaviour
{

    [SerializeField]
    LobbyStateBehaviour lobbyState;

    public const float MAX = 600.0f;

    public static float inactivityTimer = MAX;

    private void Start()
    {
        inactivityTimer = MAX;
    }

    private void OnDisable()
    {
        inactivityTimer = MAX;
    }

    // Update is called once per frame
    void Update()
    {
        
        inactivityTimer -= Time.deltaTime;

        if(inactivityTimer < 0)
        {
            EnsurePrivate();
        }
        else
        {
            privateEnsured = false;
        }

    }

    bool privateEnsured = false;

    void EnsurePrivate()
    {
        if(privateEnsured) return;
        privateEnsured = true;

        lobbyState.FORCEACESS(false);

    }

}
