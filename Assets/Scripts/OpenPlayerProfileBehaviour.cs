using Steamworks;
using UnityEngine;

public class OpenPlayerProfileBehaviour : MonoBehaviour
{

    [SerializeField]
    LobbyPlayerDisplayBehaviour display;

    public async void SELECT()
    {

        await display.assignedPlayer.friend.RequestInfoAsync();

        SteamFriends.OpenUserOverlay(display.assignedPlayer.friend.Id, "steamid");

    }

}
