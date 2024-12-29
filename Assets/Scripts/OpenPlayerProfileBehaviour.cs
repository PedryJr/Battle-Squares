using UnityEngine;

public sealed class OpenPlayerProfileBehaviour : MonoBehaviour
{

    [SerializeField]
    LobbyPlayerDisplayBehaviour display;

    PlayerSettingsBehaviour playerSettingsBehaviour;

    LoadPlayerImagesBehaviour loadPlayerImagesBehaviour;

    private void Start()
    {

        loadPlayerImagesBehaviour = GetComponentInParent<LoadPlayerImagesBehaviour>();
        playerSettingsBehaviour = loadPlayerImagesBehaviour.playerSettingsBehaviour;

    }

    public void SELECT()
    {

        playerSettingsBehaviour.SHOW(display.assignedPlayer);

    }

}
