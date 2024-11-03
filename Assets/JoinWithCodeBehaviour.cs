using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class JoinWithCodeBehaviour : MonoBehaviour
{

    [SerializeField]
    LobbyBehaviour lobbyBehaviour;

    public async void JOINWITHCODE(string code)
    {
        char[] aA = code.ToUpper().ToCharArray();
        if (aA.Length != 8) return;

        NetworkManager.Singleton.Shutdown();
        FindAnyObjectByType<PlayerSynchronizer>().ForceReset();

        LobbyQuery bruh = new LobbyQuery();
        Lobby[] lobbyWithCode = await bruh.RequestAsync();

        if (lobbyWithCode != null)
        {

            foreach (Lobby b in lobbyWithCode)
            {

                
                char[] bA = b.GetData("Code").ToCharArray();

                bool doContinue = false;

                for (int i = 0; i < aA.Length; i++)
                {

                    try
                    {
                        if (aA[i] != bA[i]) doContinue = true;
                    } catch { doContinue = true; }

                }

                if (doContinue) continue;

                b.Refresh();

                ApplyDefaultLobby(b);

                break;

            }

        }


    }

    public async void ApplyDefaultLobby(Lobby newLobby)
    {

        Friend friend = new Friend(ulong.Parse(newLobby.GetData("OwnerId").ToString()));
        await friend.RequestInfoAsync();

        lobbyBehaviour.lobbyId = friend.Id;
        lobbyBehaviour.ownerId = friend.Id;

        lobbyBehaviour.lobbyCapacity = 4;
        lobbyBehaviour.lobbyPopulation = newLobby.MemberCount;

        lobbyBehaviour.lobbyName.text = friend.Name;

        GetImageData(friend.Id);

        lobbyBehaviour.lobby = newLobby;

        lobbyBehaviour.activated = true;

    }

    public async void GetImageData(SteamId steamId)
    {

        Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        lobbyBehaviour.lobbyIcon.sprite = Sprite.Create(spriteTexture, spriteRect, spritePivot);

    }

}
