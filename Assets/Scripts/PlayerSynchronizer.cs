using FMODUnity;
using Steamworks;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
[BurstCompile]
public sealed class PlayerSynchronizer : NetworkBehaviour
{

    public SkinData skinData;

    public float ping;
    public float rtt;

    public List<PlayerData> playerIdentities;
    public List<IdPair> idPairs;
    NetworkManager networkManager;

    [SerializeField]
    public PlayerBehaviour square;

    public PlayerBehaviour localSquare;

    ProjectileManager projectileManager;
    LocalSteamData localSteamData;
    ScoreManager scoreManager;

    float serverUpdateTimer;

    [SerializeField]
    GameObject deathParticles;

    Hunter hunter;

    public bool hostShutdown = false;

    Scene lastScene;

    delegate void UpdatePFPStream();
    List<UpdatePFPStream> updatePFPStream;

    public NetworkList<IdMatch> playerIdList = new NetworkList<IdMatch>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    bool stopUpdate;
    [BurstCompile]
    private void Awake()
    {

        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        updatePFPStream = new List<UpdatePFPStream>();
        networkManager = GameObject.Find("Network").GetComponent<NetworkManager>();
        projectileManager = GetComponent<ProjectileManager>();
        localSteamData = GetComponent<LocalSteamData>();
        networkManager.OnClientConnectedCallback += SetupNewPlayer;
        networkManager.OnClientDisconnectCallback += DisconnectPlayer;
        hunter = GetComponent<Hunter>();
        scoreManager = GetComponent<ScoreManager>();

    }

    [BurstCompile]
    public void ForceReset()
    {

        if (playerIdentities != null)
        {

            foreach (PlayerData player in playerIdentities)
            {

                if (player.square) Destroy(player.square.gameObject);

            }

            playerIdentities = null;
        }

        foreach (ProjectileBehaviour projectile in projectileManager.projectiles)
        {

            if (projectile != null) Destroy(projectile.gameObject);

        }

        projectileManager.projectiles.Clear();

        PlayerBehaviour[] remainingPlayers = FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < remainingPlayers.Length; i++)
        {
            Destroy(remainingPlayers[i]);
        }

    }
    [BurstCompile]
    private void SceneManager_sceneUnloaded(Scene arg0)
    {

        if (arg0.name == "GameScene")
        {
            stopUpdate = false;
        }

    }

    [BurstCompile]
    void LateHudInit()
    {

        AmmoCounterBehaviour[] ammoCounters = FindObjectsByType<AmmoCounterBehaviour>(FindObjectsSortMode.None);
        foreach (AmmoCounterBehaviour ammoCounter in ammoCounters)
        {
            ammoCounter.UnitHUD();
            ammoCounter.UpdateWeaponType();
        }

    }

    [BurstCompile]
    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        if (arg0.name == "GameScene")
        {

            Invoke("LateHudInit", 0.3f);
            stopUpdate = false;
            FindAnyObjectByType<MapInitiator>().InitPresetMap(localSquare.selectedMap, scoreManager.gameMode);

            if (IsHost)
            {
                SteamNetwork.currentLobby?.SetData("Avalible", "false");
                SteamNetwork.currentLobby?.SetPrivate();
                SteamNetwork.currentLobby?.SetInvisible();
                SteamNetwork.currentLobby?.SetJoinable(false);
            }

        }
        else if (arg0.name == "LobbyScene")
        {
            Invoke("LateHudInit", 0.3f);
            stopUpdate = false;
            FindAnyObjectByType<PlayerController>().EnableController();

            if(SteamNetwork.currentLobby != null)
            {

                if (IsHost && (bool) SteamNetwork.currentLobby?.Owner.IsMe)
                {
                    SteamNetwork.currentLobby?.SetPublic();
                    SteamNetwork.currentLobby?.SetJoinable(true);
                }

            }

        }

        PlayerController.uiRegs = 0;

        if (!IsHost) return;
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn && localSquare) localSquare.transform.position = spawn.transform.position;
        int sceneIndex = arg0.buildIndex;

        LoadSceneOnPlayersClientRpc(sceneIndex);

        lastScene = arg0;

    }

    [BurstCompile]
    [ClientRpc]
    void LoadSceneOnPlayersClientRpc(int sceneIndex)
    {
        if (IsHost) return;

        SceneManager.LoadScene(sceneIndex);
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn) localSquare.transform.position = spawn.transform.position;

    }

    [BurstCompile]
    public void DisconnectPlayer(ulong id)
    {

        if (IsHost)
        {

            DisconnectPlayerRemotely(id);

        }

    }

    [BurstCompile]
    void DisconnectPlayerRemotely(ulong id)
    {

        if (hostShutdown)
        {

            DisconnectPlayerLocally();
            return;

        }

        List<PlayerData> refreshedIdentities = new List<PlayerData>();
        PlayerData playerToRemove = new PlayerData();

        if (playerIdentities != null)
        {
            foreach (PlayerData player in playerIdentities)
            {

                if (player.id == id)
                {
                    playerToRemove = player;

                    IdMatch idMatch = new IdMatch();
                    idMatch.clientId = player.id;
                    idMatch.steamId = player.steamId;

                }
                else refreshedIdentities.Add(player);

            }
        }

        List<ProjectileBehaviour> newProjectiles = new List<ProjectileBehaviour>();

        foreach (ProjectileBehaviour projectile in projectileManager.projectiles)
        {

            if (projectile.IsLocalProjectile) newProjectiles.Add(projectile);
            else if (projectile != null) Destroy(projectile.gameObject);

        }

        projectileManager.projectiles = newProjectiles;

        if (playerToRemove.square)
        {
            Destroy(playerToRemove.square.gameObject);
            playerIdentities = refreshedIdentities;

            DisconnectPlayerRemotelyClientRpc(id);
        }

    }

    [BurstCompile]

    [ClientRpc]
    public void DisconnectPlayerRemotelyClientRpc(ulong id)
    {

        if (IsHost) return;

        List<PlayerData> refreshedIdentities = new List<PlayerData>();
        PlayerData playerToRemove = new PlayerData();

        foreach (PlayerData player in playerIdentities)
        {

            if (player.id == id)
            {

                playerToRemove = player;

            }
            else
            {

                refreshedIdentities.Add(player);

            }

        }

        List<ProjectileBehaviour> newProjectiles = new List<ProjectileBehaviour>();

        foreach (ProjectileBehaviour projectile in projectileManager.projectiles)
        {

            if (projectile.IsLocalProjectile) newProjectiles.Add(projectile);
            else if (projectile != null) Destroy(projectile.gameObject);

        }

        projectileManager.projectiles = newProjectiles;

        Destroy(playerToRemove.square.gameObject);
        playerIdentities = refreshedIdentities;

    }

    [ClientRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
    public void KickPlayerClientRpc(byte id)
    {

        if ((byte)localSquare.id != id) return;

        DisconnectPlayerLocally();

    }

    [BurstCompile]
    public void DisconnectPlayerLocally()
    {

        NetworkManager.Shutdown();

        SceneManager.LoadSceneAsync("MenuScene");

        SteamNetwork.CreateNewLobby();

        foreach (ProjectileBehaviour projectile in projectileManager.projectiles)
        {

            if (projectile != null) Destroy(projectile.gameObject);

        }

        projectileManager.projectiles.Clear();

        if (playerIdentities != null)
        {
            foreach (PlayerData player in playerIdentities)
            {

                Destroy(player.square.gameObject);

            }

            playerIdentities = null;
        }

    }

    [BurstCompile]
    public void SetupNewPlayer(ulong id)
    {

        clrUpdate2 = 0;
        if (IsHost)
        {

            bool freshHost = false;

            if (playerIdentities == null)
            {

                playerIdentities = new List<PlayerData>();
                idPairs = new List<IdPair>
                {
                    new IdPair { clientId = id, steamId = SteamClient.SteamId }
                };
                freshHost = true;

            }


            PlayerData playerData = new PlayerData();
            playerData.square = Instantiate(square);
            playerData.square.id = id;
            playerData.id = id;

            playerIdentities.Add(playerData);

            List<ulong> nowPlayers = new List<ulong>();

            if (playerData.id == NetworkManager.LocalClientId)
            {

                localSquare = playerData.square;
                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);
                localSquare.AssertSteamDataAvalible(SteamClient.SteamId.Value);


                localSquare.nozzleFrames = new Sprite[skinData.skinFrames.Length];
                localSquare.bodyFrames = new Sprite[skinData.skinFrames.Length];
                localSquare.frameRate = skinData.animate ? skinData.frameRate : 0;

                bool validSkin = true;

                for (int j = 0; j < skinData.skinFrames.Length; j++)
                {

                    bool validFrame = skinData.skinFrames[j].valid;

                    if(!validFrame) validSkin = false;

                }

                if (validSkin)
                {

                    for (int j = 0; j < skinData.skinFrames.Length; j++)
                    {

                        bool[] bodySkin = new bool[100];
                        bool[] nozzleSkin = new bool[16];

                        for (int i = 0; i < 100; i++)
                        {
                            bodySkin[i] = skinData.skinFrames[j].frame[i];
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            nozzleSkin[i] = skinData.skinFrames[j].frame[100 + i];
                        }

                        localSquare.CreateTextureFromBoolArray10BY10(bodySkin, (byte)j);
                        localSquare.CreateTextureFromBoolArray4BY4(nozzleSkin, (byte)j);

                    }

                }
                else
                {

                    bool[] bodySkin = new bool[100];
                    bool[] nozzleSkin = new bool[16];

                    for (int i = 0; i < 100; i++)
                    {
                        bodySkin[i] = true;
                    }

                    for (int i = 0; i < 16; i++)
                    {
                        nozzleSkin[i] = true;
                    }

                    localSquare.CreateTextureFromBoolArray10BY10(bodySkin, 0);
                    localSquare.CreateTextureFromBoolArray4BY4(nozzleSkin, 0);

                }

            }
            

            foreach (PlayerData player in playerIdentities)
            {

                nowPlayers.Add(player.id);

            }

            SetupNewPlayerClientRpc(nowPlayers.ToArray(), id, scoreManager.gameMode);

            scoreManager.UpdateModeAsHost(scoreManager.gameMode);
            UpdateSelectedMap(localSquare.selectedMap);
            SendModsDataRpc(Mods.at);

            if (freshHost)
            {
                playerIdList.Clear();
                playerIdList.Add(new IdMatch { clientId = id, steamId = SteamClient.SteamId });
            }

            playerData.square.SpawnEffect();
/*
            DontDestroyOnLoad(playerData.square);*/



            foreach (PlayerData player in playerIdentities)
            {

                if(player.id != id) SyncPlayerDataClientRpc((byte)player.id, player.square.isDead);

            }

        }

    }

    [ClientRpc]
    void SyncPlayerDataClientRpc(byte id, bool isDead)
    {

        foreach (PlayerData player in playerIdentities)
        {

            if((byte) player.id == id)
            {

                player.square.isDead = isDead;

            }

        }

        UpdateColor();
        UpdateNozzle();
        UpdateHealth();

    }

    [BurstCompile]
    [ClientRpc]
    void SetupNewPlayerClientRpc(ulong[] nowPlayers, ulong connectedId, ScoreManager.Mode gameMode)
    {

        clrUpdate = 0;

        if (!IsHost)
        {

            if (playerIdentities == null)
            {

                GameModeDisplayBehaviour modeDisplay = FindAnyObjectByType<GameModeDisplayBehaviour>();
                scoreManager.gameMode = gameMode;
                if (modeDisplay) modeDisplay.DisplayGameMode(gameMode);

                playerIdentities = new List<PlayerData>();
                idPairs = new List<IdPair>
                {
                new IdPair { clientId = connectedId, steamId = SteamClient.SteamId }
                };

                for (int i = 0; i < nowPlayers.Length; i++)
                {

                    PlayerData playerData = new PlayerData();

                    playerData.square = Instantiate(square);
                    playerData.id = nowPlayers[i];
                    playerData.square.id = nowPlayers[i];

                    if (nowPlayers[i] == NetworkManager.LocalClientId)
                    {

                        localSquare = playerData.square;
                        FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);
                        localSquare.SpawnEffect();

                    }

                    playerIdentities.Add(playerData);

                }

                RequestAddPlayerServerRpc(connectedId, SteamNetwork.playerSteamID);

            }
            else
            { 

                PlayerData playerData = new PlayerData();

                playerData.square = Instantiate(square);
                playerData.id = connectedId;
                playerData.square.id = connectedId;
                playerData.square.SpawnEffect();

                playerIdentities.Add(playerData);

                if (!localSquare)
                {

                    foreach (PlayerData player in playerIdentities)
                    {

                        if(player.id == connectedId)
                        {
                            localSquare = playerData.square;
                            FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);
                        }

                    }

                }

            }

        }

        bool validSkin = true;

        for (int j = 0; j < skinData.skinFrames.Length; j++)
        {

            bool validFrame = skinData.skinFrames[j].valid;

            if (!validFrame) validSkin = false;

        }

        if (validSkin)
        {

            float frameRate = skinData.animate ? skinData.frameRate : 0;

            RequestSkinLengthServerRpc((byte)localSquare.id, (byte)skinData.skinFrames.Length, frameRate);

            for (int i = 0; i < skinData.skinFrames.Length; i++)
            {

                byte[] skinAsData = MyExtentions.BoolArrayToByteArray(skinData.skinFrames[i].frame);

                RequestSkinUpdateServerRpc((byte)localSquare.id, skinAsData, (byte)i);

            }

        }
        else
        {

            bool[] skin = new bool[116];

            for (int i = 0; i < skin.Length; i++)
            {
                skin[i] = true;
            }

            byte[] skinAsData = MyExtentions.BoolArrayToByteArray(skin);

            RequestSkinLengthServerRpc((byte)localSquare.id, 1, 0);
            RequestSkinUpdateServerRpc((byte)localSquare.id, skinAsData, 0);
        }

        RequestSteamDataServerRpc((byte)localSquare.id, SteamClient.SteamId.Value);

    }

    [BurstCompile]
    [Rpc(SendTo.Everyone)]
    public void SendModsDataRpc(float[] mods)
    {

        if (IsHost) return;

        for (int i = 0; i < mods.Length; i++)
        {

            Mods.at[i] = mods[i];

        }


    }

    [BurstCompile]
    [ServerRpc(RequireOwnership = false)]
    public void RequestAddPlayerServerRpc(ulong clientId, ulong steamId)
    {

        if (!IsHost) return;

        IdMatch newIdMatch = new IdMatch { clientId = clientId, steamId = steamId };
        bool addNewId = true;

        for (int i = 0; i < playerIdList.Count; i++)
        {
            if (playerIdList[i].clientId == clientId)
            {
                playerIdList[i] = newIdMatch;
                addNewId = false;
            }
        }

        if(addNewId) playerIdList.Add(new IdMatch { clientId = clientId, steamId = steamId });

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSkinUpdateServerRpc(byte id, byte[] skin, byte index)
    {

        RequestSkinUpdateClientRpc(id, skin, index);

    }

    [ClientRpc]
    public void RequestSkinUpdateClientRpc(byte id, byte[] skinAsData, byte index)
    {

        bool[] skin = MyExtentions.ByteArrayToBoolArray(skinAsData, 116);

        bool[] bodySkin = new bool[100];
        bool[] nozzleSkin = new bool[16];

        for (int i = 0; i < 100; i++)
        {
            bodySkin[i] = skin[i];
        }

        for (int i = 0; i < 16; i++)
        {
            nozzleSkin[i] = skin[100 + i];
        }

        foreach (PlayerData player in playerIdentities)
        {
            if((byte) player.square.id == id)
            {

                player.square.CreateTextureFromBoolArray10BY10(bodySkin, index);
                player.square.CreateTextureFromBoolArray4BY4(nozzleSkin, index);

            }

        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSkinLengthServerRpc(byte id, byte length, float frameRate)
    {
        RequestSkinLengthClientRpc(id, length, frameRate);
    }

    [ClientRpc]
    public void RequestSkinLengthClientRpc(byte id, byte length, float frameRate)
    {

        foreach (PlayerData player in playerIdentities)
        {
            if ((byte)player.square.id == id)
            {

                player.square.bodyFrames = new Sprite[length];
                player.square.nozzleFrames = new Sprite[length];
                player.square.frameRate = frameRate;

            }

        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSteamDataServerRpc(byte id, ulong steamId)
    {
        RequestSteamDataClientRpc(id, steamId);
    }

    [ClientRpc]
    public void RequestSteamDataClientRpc(byte id, ulong steamId)
    {

        foreach (PlayerData player in playerIdentities)
        {
            if ((byte)player.square.id == id)
            {

                player.square.AssertSteamDataAvalible(steamId);

            }

        }

    }

    [BurstCompile]
    private void FixedUpdate() => UpdatePlayerData();

    float miniTimer;

    private void Update()
    {

        rtt = (float) (NetworkManager.LocalTime.Time - NetworkManager.ServerTime.Time);
        ping = rtt / 2;

    }


    float rbUpdate, scrUpdate, clrUpdate, clrUpdate2;

    [BurstCompile]
    void UpdatePlayerData()
    {

        float deltaTime = Time.deltaTime;

        if (stopUpdate) return;
        if (localSquare == null) return;
        if (playerIdentities == null) return;

        if (rbUpdate > 1)
        {
            UpdateRigidBody();
            rbUpdate -= 1;
        }

        if (clrUpdate > 1)
        {
            UpdateColor();
            UpdatePlayerReady(localSquare.ready);
            clrUpdate = 0;
        }
        else if (clrUpdate2 < 8)
        {
            clrUpdate += deltaTime * 20;
            clrUpdate2 += deltaTime;
        }
        rbUpdate += deltaTime * 100f;

    }/*
    [BurstCompile]
    void UpdateRigidBody()
    {
        byte sourceId = (byte) localSquare.id;

        float[] data = new float[] 
        { 
            localSquare.rb.position.x, localSquare.rb.position.y,
            localSquare.rb.linearVelocity.x, localSquare.rb.linearVelocity.y,
            localSquare.rb.rotation, localSquare.rb.angularVelocity
        };

        UpdateRigidBodyRpc(data, sourceId);

    }
    [BurstCompile]

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    void UpdateRigidBodyRpc(float[] data, byte sourceId)
    {

        if (playerIdentities == null) return;
        if ((byte) localSquare.id == sourceId) return;

        foreach (PlayerData player in playerIdentities)
        {

            if ((byte)player.square.id != sourceId) continue;

            StorePlayerRigidBodyData(player, data);

        }
    }
    [BurstCompile]
    void StorePlayerRigidBodyData(PlayerData player, float[] data)
    {

        if (player.square.isDead) return;

        player.square.rb.position = new Vector2(data[0], data[1]);
        player.square.rb.linearVelocity = new Vector2(data[2], data[3]);
        player.square.rb.rotation = data[4];
        player.square.rb.angularVelocity = data[5];

    }*/
        [BurstCompile]
        void UpdateRigidBody()
        {
            ulong sourceId = networkManager.LocalClientId;

            byte[] compPos = MyExtentions.EncodePosition(localSquare.position.x + 64, localSquare.position.y + 64);
            byte[] compVel = MyExtentions.EncodePosition(localSquare.velocity.x + 64, localSquare.velocity.y + 64);
            byte[] compRot = MyExtentions.EncodeRotation(localSquare.rotation);
            byte[] compRotVel = MyExtentions.EncodeFloat(localSquare.angularVelocity);

            byte[] data = new byte[14]
            {
                compPos[0], compPos[1], compPos[2], compPos[3],
                compVel[0], compVel[1], compVel[2], compVel[3],
                compRot[0], compRot[1],
                compRotVel[0], compRotVel[1], compRotVel[2],
                (byte) sourceId
            };

            UpdateRigidBodyRpc(data);

        }
        [BurstCompile]

        [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
        void UpdateRigidBodyRpc(byte[] data)
        {

            if ((byte)networkManager.LocalClientId == data[13]) return;
            if (playerIdentities == null) return;

            foreach (PlayerData player in playerIdentities)
            {

                StorePlayerRigidBodyData(player, data);

            }
        }
        [BurstCompile]
        void StorePlayerRigidBodyData(PlayerData player, byte[] data)
        {

            if ((byte)player.id != data[13]) return;
            if (player.square.isDead) return;

            byte[] compPos = new byte[4] { data[0], data[1], data[2], data[3] };
            byte[] compVel = new byte[4] { data[4], data[5], data[6], data[7] };
            byte[] compRot = new byte[2] { data[8], data[9] };
            byte[] compRotVel = new byte[3] { data[10], data[11], data[12] };


            (float xPos, float yPos) = MyExtentions.DecodePosition(compPos);
            xPos -= 64;
            yPos -= 64;
            (float xVel, float yVel) = MyExtentions.DecodePosition(compVel);
            xVel -= 64;
            yVel -= 64;
            float rot = MyExtentions.DecodeRotation(compRot);
            float rotVel = MyExtentions.DecodeFloat(compRotVel);

            player.square.rb.position = new Vector2(xPos, yPos);
            player.square.rb.rotation = rot;
            player.square.rb.linearVelocity = new Vector2(xVel, yVel);
            player.square.rb.angularVelocity = rotVel;

        }
    [BurstCompile]
    public void UpdateNozzle()
    {
        ulong sourceId = networkManager.LocalClientId;
        byte[] compFromPos = MyExtentions.EncodeNozzlePosition(localSquare.fromPos.x, localSquare.fromPos.y);
        byte[] compToPos = MyExtentions.EncodeNozzlePosition(localSquare.toPos.x, localSquare.toPos.y);

        byte[] data = new byte[5] { (byte)sourceId, compFromPos[0], compFromPos[1], compToPos[0], compToPos[1] };

        UpdateNozzleRpc(data);

    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
    void UpdateNozzleRpc(byte[] data)
    {
        if (networkManager.LocalClientId == data[0]) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreNozzleData(player, data);
        }
    }
    [BurstCompile]
    void StoreNozzleData(PlayerData player, byte[] comp)
    {
        if ((byte)player.id != comp[0]) return;

        (float fromX, float fromY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[1], comp[2] });
        (float toX, float toY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[3], comp[4] });

        player.square.fromPos = new Vector2(fromX, fromY);
        player.square.toPos = new Vector2(toX, toY);
        player.square.newNozzleLerp = 0;

    }
    [BurstCompile]
    public void UpdateColor()
    {
        ulong sourceId = networkManager.LocalClientId;
        byte[] data = new byte[4]
        {
            (byte) sourceId,
            (byte) math.round(localSquare.playerColor.r * 256),
            (byte) math.round(localSquare.playerColor.g * 256),
            (byte) math.round(localSquare.playerColor.b * 256)
        };
        UpdateColortRpc(data);
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
    void UpdateColortRpc(byte[] data)
    {
        if (networkManager.LocalClientId == data[0]) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreColorData(player, data);
        }
    }
    [BurstCompile]
    void StoreColorData(PlayerData player, byte[] data)
    {
        if (player.id != data[0]) return;

        player.square.playerColor = new UnityEngine.Color(data[1] / 256f, data[2] / 256f, data[3] / 256f);
        player.square.newColor = true;

    }
    [BurstCompile]
    public void UpdateHealth()
    {
        byte sourceId = (byte)networkManager.LocalClientId;
        UpdateHealthRpc(sourceId, localSquare.healthPoints);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateHealthRpc(byte sourceId, float data)
    {
        if ((byte)networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreHealthData(player, sourceId, data);
        }
    }
    [BurstCompile]
    void StoreHealthData(PlayerData player, byte sourceId, float data)
    {
        if ((byte)player.id != sourceId) return;

        player.square.healthPoints = data;
    }
    [BurstCompile]
    public void UpdateScore()
    {
        byte sourceId = (byte)networkManager.LocalClientId;
        byte data = (byte)localSquare.score;

        UpdateScoreRpc(sourceId, data);

    }

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateScoreRpc(byte sourceId, byte data)
    {
        if ((byte)networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreScoreData(player, sourceId, data);
        }
    }

    [BurstCompile]
    void StoreScoreData(PlayerData player, byte sourceId, byte data)
    {

        if ((byte)player.id != sourceId) return;

        player.square.score = data;

    }

    [BurstCompile]
    public void UpdatePlayerReady(bool ready)
    {

        byte sourceId = (byte) localSquare.id;

        UpdatePlayerReadyRpc(sourceId, ready);

    }

    [BurstCompile]
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerReadyRpc(byte sourceId, bool ready)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            if(player.id == sourceId) StorePlayerReady(player, sourceId, ready);
        }

    }

    [BurstCompile]
    void StorePlayerReady(PlayerData player, byte sourceId, bool ready)
    {

        player.square.ready = ready;

    }
    [BurstCompile]
    public void UpdateSelectedMap(int map)
    {

        if (!IsHost) return;
        UpdateSelectedMapRpc(map);

    }
    [BurstCompile]
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateSelectedMapRpc(int map)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreSelectedMap(player, map);
        }

    }
    [BurstCompile]
    void StoreSelectedMap(PlayerData player, int map)
    {

        player.square.selectedMap = map;

    }
    /*
    [BurstCompile]
    public void UpdatePlayerHealth(ulong id, float modifier, ulong responsibleId, Vector2 knockBack)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        if (IsHost)
        {

            UpdatePlayerHealthClientRpc(id, modifier, responsibleId, knockBack, ignoreId);

        }

        if (!IsHost)
        {

            UpdatePlayerHealthServerRpc(id, modifier, responsibleId, knockBack, ignoreId);

        }

        EndUpdatePlayerHealth(id, modifier, responsibleId, knockBack);

    }
    [BurstCompile]
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerHealthServerRpc(ulong id, float damage, ulong responsibleId, Vector2 knockBack, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        UpdatePlayerHealthClientRpc(id, damage, responsibleId, knockBack, ignoreId);

        EndUpdatePlayerHealth(id, damage, responsibleId, knockBack);

    }
    [BurstCompile]
    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerHealthClientRpc(ulong id, float damage, ulong responsibleId, Vector2 knockBack, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        if (IsHost) return;

        EndUpdatePlayerHealth(id, damage, responsibleId, knockBack);

    }
    [BurstCompile]
    void EndUpdatePlayerHealth(ulong id, float damage, ulong responsibleId, Vector2 knockBack)
    {

        bool kill = false;

        foreach (PlayerData player in playerIdentities)
        {

            if (player.id != id) continue;

            if (player.square.isDead) continue;

            player.square.healthPoints -= damage;

            player.square.rb.AddForce(knockBack, ForceMode2D.Impulse);

            if (player.square.healthPoints > 0) continue;

            player.square.KillPlayer();
            kill = true;

        }

        if (kill && scoreManager.gameMode == ScoreManager.Mode.DM)
        {

            foreach (PlayerData data in playerIdentities)
            {

                if (data.id != responsibleId) continue;

                data.square.score++;
                scoreManager.UpdateScoreBoard();

            }
        }

        UpdateHealth();
        UpdateScore();

    }*/


    public void UpdatePlayerHealth(byte id, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {
        UpdatePlayerHealthRpc(id, damage, slowDownAmount, responsibleId, knockBack);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void UpdatePlayerHealthRpc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {
        UpdatePlayerHealthFunc(affectedId, damage, slowDownAmount, responsibleId, knockBack);
        
    }


    void UpdatePlayerHealthFunc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {

        bool kill = false;

        foreach (PlayerData player in playerIdentities)
        {

            if ((byte) player.square.id == affectedId)
            {

                if (!player.square.isDead)
                {

                    player.square.rb.AddForce(knockBack, ForceMode2D.Impulse);
                    player.square.healthPoints -= damage;
                    player.square.healthPoints = math.clamp(player.square.healthPoints, 0, player.square.maxHealthPoints);

                    player.square.rb.linearDamping = math.clamp(player.square.rb.linearDamping + slowDownAmount, 0.1f, 100f);
                    player.square.rb.angularDamping = math.clamp(player.square.rb.angularDamping + slowDownAmount, 0.1f, 100f);

                }

                if (player.square.healthPoints <= 0 && !player.square.isDead)
                {

                    foreach (PlayerData player1 in playerIdentities) if ((byte) player1.id == responsibleId) player.square.killStreak++;

                    kill = true;
                    PlayerDeathEffect(player.square.rb.position, player.square.playerDarkerColor);
                    hunter.Kill(affectedId, responsibleId);
                    player.square.KillPlayer();

                }

            }

        }

        UpdateScore();

        if (kill && scoreManager.gameMode == ScoreManager.Mode.DM && responsibleId == NetworkManager.Singleton.LocalClientId)
        {

            foreach (PlayerData player in playerIdentities)
            {

                if ((byte) player.id == responsibleId)
                {

                    player.square.score++;

                }

            }

        }

        if (affectedId == (byte) localSquare.id && !localSquare.isDead)
        {

            UpdateHealth();

        }

        if (responsibleId == (byte) localSquare.id)
        {

            UpdateScore();

        }

    }

    public void PlayerDeathEffect(Vector3 particlePosition, Color particleColor)
    {

        localSquare.deathSoundInstance.setVolume(MySettings.volume);
        localSquare.deathSoundInstance.start();

        GameObject newParticle = Instantiate(deathParticles, particlePosition, Quaternion.Euler(0, 0, 0), null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial = Instantiate(particle.material);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

    }
    [BurstCompile]
    public Color UpdatePlayerColor(float value)
    {

        float h, s, v;
        Color.RGBToHSV(localSquare.playerColor, out h, out s, out v);
        h = value;
        localSquare.playerColor = Color.HSVToRGB(h, s, v);
        localSquare.newColor = true;

        UpdateColor();

        return localSquare.playerColor;

    }
    [BurstCompile]
    public void SpreadInGameMessage(string message)
    {

        byte playerId = (byte) localSquare.id;
        string sanetizedMessage = MyExtentions.SanitizeMessage(message);

        SpreadInGameMessageRpc(sanetizedMessage, playerId);

    }
    [BurstCompile]
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void SpreadInGameMessageRpc(string message, byte playerId)
    {

        PlayerBehaviour source = null;
        MessageRecieverBehaviour messageReciever = null;

        foreach(PlayerData player in playerIdentities)
        {

            if (player.id == playerId) source = player.square;

        }

        messageReciever = FindAnyObjectByType<MessageRecieverBehaviour>();

        if (!source) return;
        if (!messageReciever) return;

        messageReciever.CreateNewMessage(message, source);

    }
    [BurstCompile]
    public void SyncMods(int index, float value)
    {

        if (!IsHost) return;

        SyncModsRpc(index, value);

    }
    [BurstCompile]
    [Rpc(SendTo.Everyone)]
    void SyncModsRpc(int index, float value)
    {

        Mods.at[index] = value;

    }
    [BurstCompile]
    public struct PlayerData
    {

        public ulong id;
        public ulong steamId;
        public PlayerBehaviour square;
        public string name;

        public Sprite pfp;
        public Texture2D texture;
        public void UpdatePFP(int x, int y, byte[] rgb)
        {
            UnityEngine.Color pixelColor = new UnityEngine.Color(rgb[0] / 256f, rgb[1] / 256f, rgb[2] / 256f, 1f);

            texture.SetPixel(x, y, pixelColor);
        }

        public void ApplyPFP()
        {
            texture.Apply();
        }

    }
    [BurstCompile]
    public struct IdPair
    {
        public ulong clientId;
        public SteamId steamId;
    }

}
[BurstCompile]
public struct IdMatch : INetworkSerializable, IEquatable<IdMatch>
{
    public ulong clientId;
    public ulong steamId;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref steamId);
    }
    public bool Equals(IdMatch other)
    {
        return clientId == other.clientId && steamId == other.steamId;
    }
    public override bool Equals(object obj)
    {
        return obj is IdMatch other && Equals(other);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return (clientId.GetHashCode() * 397) ^ steamId.GetHashCode();
        }
    }
    public static bool operator ==(IdMatch left, IdMatch right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(IdMatch left, IdMatch right)
    {
        return !(left == right);
    }
}