using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class LobbyBehaviour : MonoBehaviour
{

    [SerializeField]
    public TextMeshProUGUI lobbyName;

    [SerializeField]
    public UnityEngine.UI.Image lobbyIcon;

    public LobbyLoader lobbyLoader;

    public SteamId lobbyId = new SteamId();
    public SteamId ownerId = new SteamId();

    public Lobby lobby;

    public int lobbyCapacity;
    public int lobbyPopulation;

    [SerializeField]
    public bool isPreview;

    public bool activated = false;

    bool doDestroy = false;

    [SerializeField]
    UnityEngine.UI.Image borderImage;

    bool firstLoad;
    private void Awake()
    {
        firstLoad = true;
    }

    public LobbyBehaviour Initialize(Lobby lobby, LobbyLoader lobbyLoader)
    {

        activated = true;

        this.lobby = lobby;

        string steamIdAsString = string.Empty;
        string lobbyName = string.Empty;
        string invalidChars = @"@% ^|\<> ~`";

        IEnumerator<KeyValuePair<string, string>> enumerableData = lobby.Data.GetEnumerator();

        StringBuilder sb = new StringBuilder();

        lobbyName = lobby.GetData("Name");
        ownerId.Value = ulong.Parse(lobby.GetData("OwnerId").ToString());
        GetImageData(ownerId);
        lobbyId = lobby.Id;

        lobbyCapacity = lobby.MaxMembers;
        lobbyPopulation = lobby.MemberCount;

        if(lobbyName == string.Empty) doDestroy = true;

        if (doDestroy)
        {

            lobbyLoader.failedLobbies.Add(this);

        }
        else
        {

            foreach (char lobbyNameChar in lobbyName)
            {

                bool isValid = true;

                foreach (char invalidNameChar in invalidChars) if(lobbyNameChar == invalidNameChar) isValid = false;

                if (isValid) sb.Append(lobbyNameChar);
                else sb.Append('*');

            }

            this.lobbyName.text = sb.ToString();
            this.lobbyLoader = lobbyLoader;

        }

        UpdateAvalible();

        return this;
    
    }

    public async void GetImageData(SteamId steamId)
    {

        Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);
        if (image == null) doDestroy = true;
        if (doDestroy) return;

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        lobbyIcon.sprite = Sprite.Create(spriteTexture, spriteRect, spritePivot);

    }

    public void OnClicked()
    {

        LobbyBehaviour preview = lobbyLoader.lobbyPreview.GetComponent<LobbyBehaviour>();

        preview.lobbyId = lobbyId;
        preview.ownerId = ownerId;

        preview.lobbyCapacity = lobbyCapacity;
        preview.lobbyPopulation = lobbyPopulation;

        preview.lobbyName.text = lobbyName.text;

        preview.lobbyIcon.sprite = lobbyIcon.sprite;

        preview.lobby = lobby;

        preview.activated = activated;

    }

    float avalibilityUpdateTime;

    private void Update()
    {

        avalibilityUpdateTime += Time.deltaTime;

        if (avalibilityUpdateTime > 0.5f) UpdateAvalible();

    }

    public void UpdateAvalible()
    {

        avalibilityUpdateTime = 0;

        if (isPreview) return;

        lobby.Refresh();

        if(ownerId.Value == SteamClient.SteamId.Value)
        {

            borderImage.color = new UnityEngine.Color(0.4627451f, 0.4627451f, 0.4627451f, 1f);

            if (firstLoad && !lobbyLoader.lobbyPreview.GetComponent<LobbyBehaviour>().activated)
            {
                OnClicked();
                firstLoad = false;
            }

        }else if (lobby.GetData("Avalible").Equals("true"))
        {

            borderImage.color = new UnityEngine.Color(0.213937f, 0.349f, 0.2229412f, 1f);
        }
        else
        {

            borderImage.color = new UnityEngine.Color(0.3490196f, 0.2156863f, 0.2156863f, 1f);

        }

    }
}
