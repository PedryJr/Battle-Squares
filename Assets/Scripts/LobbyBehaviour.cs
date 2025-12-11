using Steamworks;
using Steamworks.Data;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class LobbyBehaviour : MonoBehaviour
{

    [SerializeField] ulong lobbyIdSHOW;
    [SerializeField] ulong ownerIdSHOW;

    [SerializeField]
    public TextMeshProUGUI lobbyName;

    [SerializeField]
    public UnityEngine.UI.Image lobbyIcon;

    public LobbyLoader lobbyLoader;

    public ManagedLobby lobby;
    public ref Lobby GetLobby => ref lobby.GetLobby;

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
        lobby = new ManagedLobby();
        lobbyLoader = FindAnyObjectByType<LobbyLoader>();
    }

    public LobbyBehaviour Initialize(Lobby lobbyToInitialize)
    {
        activated = true;

        this.lobby = new ManagedLobby(lobbyToInitialize);

        string steamIdAsString = string.Empty;
        string lobbyName = string.Empty;
        string invalidChars = @"@% ^|\<> ~`";

        StringBuilder sb = new StringBuilder();

        lobbyName = lobby.OwnerName;
        GetImageData(lobby.OwnerId);

        lobbyCapacity = lobbyToInitialize.MaxMembers;
        lobbyPopulation = lobbyToInitialize.MemberCount;

        if(lobbyName == string.Empty) doDestroy = true;

        if (doDestroy || !lobbyToInitialize.Id.IsValid)
        {
            lobbyLoader.failedLobbies.Add(lobby.Id);
            Debug.Log("UhhhhhhhhhhWTF");
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
        }

        lobbyIdSHOW = lobby.Id;
        ownerIdSHOW = lobby.OwnerId;
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

        preview.lobby = lobby;

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
        if(isPreview) PreviewUpdate();
        else ListingUpdate();
    }

    void PreviewUpdate()
    {
        if (lobby.OwnerId == 0) TryLoadOwnLobby();
        else if (!lobby.IsAvalible && !lobby.OwnedBySelf) TryLoadOwnLobby();

        void TryLoadOwnLobby()
        {
            LobbyBehaviour ownLobby = lobbyLoader.GetOwnLobby();
            if(ownLobby) ownLobby.OnClicked();
        }
    }

    

    void ListingUpdate()
    {
        avalibilityUpdateTime += Time.deltaTime;
        if (avalibilityUpdateTime > 0.05f) UpdateAvalible();
    }

    public void UpdateAvalible()
    {
        
        avalibilityUpdateTime = 0;

        lobby.TryRefresh();

        if (!lobby.IsAvalible && lobby.OwnerId != SteamClient.SteamId.Value)
        {
            lobbyLoader.LobbiesV2.Remove(lobby.Id);
            Destroy(gameObject);
        }
    }
}

public class ManagedSteamId
{
    private SteamId steamId;

    public ulong Id
    {
        get { return steamId.Value; }
        set { steamId.Value = value; }
    }
}

public class ManagedLobby
{
    bool _empty = default;
    private Lobby lobby;
    public ManagedLobby()
    {
        _empty = true;
        lobby = new Lobby();
    }

    public ManagedLobby(Lobby lobby)
    {
        _empty = false;
        this.lobby = lobby;
        TryRefresh();
    }

    public ulong Id => lobby.Id;

    public string OwnerName
    {
        get
        {
            if (_empty) return "NaL";
            TryRefresh();
            return lobby.GetData("Name");
        }
    }
    public ulong OwnerId
    {
        get
        {
            if (_empty) return 0;
            TryRefresh();
            return ulong.Parse(lobby.GetData("OwnerId"));
        }
    }

    public bool IsAvalible
    {
        get
        {
            if (_empty) return false;
            TryRefresh();
            return bool.Parse(this.lobby.GetData("Avalible"));
        }
    }

    public bool OwnedBySelf
    {
        get
        {
            if (_empty) return false;
            TryRefresh();
            return OwnerId == SteamClient.SteamId.Value;
        }
    }
    public bool IsValid 
    {
        get
        {
            return lobby.Id.IsValid;
        }
    }


    public void TryRefresh()
    {
        if (!IsCurrentLobby) lobby.Refresh();
    }
    public bool IsCurrentLobby => SteamNetwork.currentLobby?.Id == lobby.Id;
    public ref Lobby GetLobby => ref lobby;

}