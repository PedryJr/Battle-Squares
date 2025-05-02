using Netcode.Transports.Facepunch;
using Steamworks;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerSynchronizer : NetworkBehaviour
{

    public SkinData skinData;

    public float ping;
    public float rtt;

    public List<PlayerData> playerIdentities;
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

    bool[] defaultSkin = new bool[116];

    
    private void Awake()
    {

        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        updatePFPStream = new List<UpdatePFPStream>();
        networkManager = GameObject.Find("Network").GetComponent<NetworkManager>();
        projectileManager = GetComponent<ProjectileManager>();
        localSteamData = GetComponent<LocalSteamData>();

        networkManager.OnConnectionEvent += NetworkManager_OnConnectionEvent;
        networkManager.ConnectionApprovalCallback += ConnectionApproval;

        hunter = GetComponent<Hunter>();
        scoreManager = GetComponent<ScoreManager>();
        for (int i = 0; i < defaultSkin.Length; i++) defaultSkin[i] = true;

    }

    void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log(request.Payload.Length);
        response.Approved = true;
    }

    private void NetworkManager_OnConnectionEvent(NetworkManager networkManager, ConnectionEventData arg2)
    {

        //Debug.Log("MFFF");

        //networkManager.PendingClients.Clear();

        if (arg2.EventType == ConnectionEvent.PeerConnected) CreateNewPlayer(arg2.ClientId);
        if (arg2.EventType == ConnectionEvent.ClientConnected) CreateNewPlayer(arg2.ClientId);
        if (arg2.EventType == ConnectionEvent.PeerDisconnected) DisconnectPlayer(arg2.ClientId);
        if (arg2.EventType == ConnectionEvent.ClientDisconnected) DisconnectPlayer(arg2.ClientId);
    }

    
    public void ForceReset()
    {

        if (playerIdentities != null)
        {

            foreach (PlayerData player in playerIdentities)
            {

                if (player.square) Destroy(player.square.gameObject);

            }

        }

        foreach (ProjectileBehaviour projectile in projectileManager.projectiles)
        {

            if (projectile != null) Destroy(projectile.gameObject);

        }


        PlayerBehaviour[] remainingPlayers = FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < remainingPlayers.Length; i++)
        {
            Destroy(remainingPlayers[i]);
        }

        //playerIdentities = null;
        projectileManager.projectiles.Clear();

    }
    
    private void SceneManager_sceneUnloaded(Scene arg0)
    {

        if (arg0.name == "GameScene")
        {
            stopUpdate = false;
        }

    }

    
    void LateHudInit()
    {

        AmmoCounterBehaviour[] ammoCounters = FindObjectsByType<AmmoCounterBehaviour>(FindObjectsSortMode.None);
        foreach (AmmoCounterBehaviour ammoCounter in ammoCounters)
        {
            ammoCounter.UnitHUD();
            ammoCounter.UpdateWeaponType();
        }

    }

    
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

            if (SteamNetwork.currentLobby != null)
            {

                if (IsHost && (bool)SteamNetwork.currentLobby?.Owner.IsMe)
                {
                    SteamNetwork.currentLobby?.SetPublic();
                    SteamNetwork.currentLobby?.SetJoinable(true);
                }

            }

        }
        else if (arg0.name == "MenuScene")
        {


        }

        PlayerController.uiRegs = 0;

        if (!IsHost) return;
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn && localSquare) localSquare.transform.position = spawn.transform.position;
        int sceneIndex = arg0.buildIndex;

        LoadSceneOnPlayersClientRpc(sceneIndex);

        lastScene = arg0;

    }

    
    [ClientRpc]
    void LoadSceneOnPlayersClientRpc(int sceneIndex)
    {
        if (IsHost) return;

        SceneManager.LoadScene(sceneIndex);
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn) localSquare.transform.position = spawn.transform.position;

    }

    
    public void DisconnectPlayer(ulong id)
    {

        if (IsHost)
        {

            DisconnectPlayerRemotely(id);

        }

    }

    
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

    
    public void DisconnectPlayerLocally()
    {
        Debug.Log("IMAGINE");
        NetworkManager.Shutdown(true);
        FacepunchTransport ft = GameObject.FindGameObjectWithTag("Net").GetComponent<FacepunchTransport>();
        ft.DisconnectLocalClient();
        ft.Shutdown();

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

            //playerIdentities = null;
        }
        playerIdentities.Clear();

    }

    public void CreateNewPlayer(ulong id)
    {

        if (!IsHost) return;

        GameStateDataPacket currentGameState = new GameStateDataPacket();

        currentGameState.currentGameMode = scoreManager.gameMode;
        currentGameState.mods = (float[]) Mods.at.Clone();

        RoundTripCollectorClientRpc(currentGameState);

    }

    bool FetchSkinValidity()
    {
        bool skinValidCheck = true;
        foreach (var frame in skinData.skinFrames) skinValidCheck = frame.valid && skinValidCheck;
        return skinValidCheck;
    }
    int FetchFrameCount() => FetchSkinValidity() ? skinData.frames : 1;
    float FetchFrameAnimation() => FetchSkinValidity() ? skinData.frameRate : 0F;
    byte[] FetchFramePixels() => FetchSkinValidity() ? GetCustomSkin() : MyExtentions.BoolArrayToByteArray(defaultSkin);
    byte[] GetCustomSkin()
    {
        byte[] frameBuffer;
        List<byte> collectedSkinData = new List<byte>();
        foreach (SkinData.SkinFrame frame in skinData.skinFrames)
        {
            frameBuffer = MyExtentions.BoolArrayToByteArray(frame.frame);
            collectedSkinData.AddRange(frameBuffer);
        }
        return collectedSkinData.ToArray();
    }

    bool IsNewPlayer(ulong playerId)
    {
        bool playerExists = false;
        if(playerIdentities == null) playerIdentities = new List<PlayerData>();
        foreach (PlayerData player in playerIdentities)
        {
            if ((byte)player.id == playerId)
            {
                playerExists = true;
                break;
            }
        }
        return !playerExists;
    }

    [ClientRpc]
    public void RoundTripCollectorClientRpc(GameStateDataPacket currentGameState)
    {
        RoundTripCollector(ref currentGameState);
    }

    void RoundTripCollector(ref GameStateDataPacket currentGameState)
    {
        scoreManager.gameMode = currentGameState.currentGameMode;
        for (int i = 0; i < currentGameState.mods.Length; i++) Mods.at[i] = currentGameState.mods[i];

        PlayerFactoryDataPacket playerFactoryData = new PlayerFactoryDataPacket();

        playerFactoryData.steamId = SteamClient.SteamId.Value;
        playerFactoryData.networkId = NetworkManager.LocalClientId;
        playerFactoryData.skinFrames = FetchFramePixels();
        playerFactoryData.skinFrameCount = FetchFrameCount();
        playerFactoryData.skinAnimationSpeed = FetchFrameAnimation();

        if(IsHost) PlayerFactoryClientRpc(playerFactoryData);
        else PlayerFactoryServerRpc(playerFactoryData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerFactoryServerRpc(PlayerFactoryDataPacket playerData)
    {
        PlayerFactoryClientRpc(playerData);
    }

    [ClientRpc]
    public void PlayerFactoryClientRpc(PlayerFactoryDataPacket playerData)
    {
        PlayerFactory(ref playerData);
    }
    public void PlayerFactory(ref PlayerFactoryDataPacket playerData)
    {
        Debug.Log("Player Factory RPC\n" +
            $"Source ID: {playerData.networkId}\n" +
            $"Source SteamID: {playerData.steamId}\n" +
            $"Skin data: {playerData.skinFrames.Length}\n" +
            $"Skin frames: {playerData.skinFrameCount}\n" +
            $"Skin speed: {playerData.skinAnimationSpeed}");

        if (IsNewPlayer(playerData.networkId)) InstantiateNewPlayer(ref playerData);

        UpdateColor();
        UpdateNozzle();
        UpdateRigidBody();
        UpdateHealth();
        UpdatePlayerReady(localSquare.ready);

    }

    public void InstantiateNewPlayer(ref PlayerFactoryDataPacket playerData)
    {
        PlayerBehaviour newPlayer = Instantiate(square);

        SetPlayerInitialData(ref newPlayer, ref playerData);

        SetPlayerSkinData(ref newPlayer, ref playerData);

        SetPlayerLocality(ref newPlayer, ref playerData);

        SetPlayerSyncData(ref newPlayer, ref playerData);

        SpawnPlayer(ref newPlayer);
    }

    private void SpawnPlayer(ref PlayerBehaviour newPlayer)
    {
        newPlayer.SpawnEffect();
    }

    private void SetPlayerLocality(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        if (playerData.networkId != NetworkManager.LocalClientId) return;

        localSquare = newPlayer;
        FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

    }

    private void SetPlayerSyncData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        playerIdentities.Add(new PlayerData
        {
            square = newPlayer,
            id = playerData.networkId,
            steamId = playerData.steamId
        });
        newPlayer.AssertSteamDataAvalible(playerData.steamId);
    }

    private void SetPlayerInitialData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        newPlayer.id = playerData.networkId;
    }

    public void SetPlayerSkinData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {

        newPlayer.nozzleFrames = new Sprite[playerData.skinFrameCount];
        newPlayer.bodyFrames = new Sprite[playerData.skinFrameCount];
        newPlayer.frameRate = playerData.skinAnimationSpeed;

        byte[] frameBuffer = new byte[15];
        bool[] skinBuffer;
        int frameBufferIndex = 0;

        for (int frameIndex = 0; frameIndex < playerData.skinFrameCount; frameIndex++)
        {

            for (int i = 0; i < 15; i++, frameBufferIndex++)
            {

                frameBuffer[i] = playerData.skinFrames[frameBufferIndex];

            }

            skinBuffer = MyExtentions.ByteArrayToBoolArray(frameBuffer, 116);

            bool[] bodySkin = new bool[100];
            bool[] nozzleSkin = new bool[16];

            for (int i = 0; i < 100; i++)
            {
                bodySkin[i] = skinBuffer[i];
            }

            for (int i = 0; i < 16; i++)
            {
                nozzleSkin[i] = skinBuffer[100 + i];
            }

            newPlayer.CreateTextureFromBoolArray10BY10(bodySkin, (byte)frameIndex);
            newPlayer.CreateTextureFromBoolArray4BY4(nozzleSkin, (byte)frameIndex);

        }

    }

    public struct PlayerFactoryDataPacket : INetworkSerializable
    {

        public ulong steamId;
        public ulong networkId;

        public int skinFrameCount;
        public float skinAnimationSpeed;
        public byte[] skinFrames;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref steamId);
            serializer.SerializeValue(ref networkId);

            serializer.SerializeValue(ref skinFrameCount);
            serializer.SerializeValue(ref skinAnimationSpeed);
            serializer.SerializeValue(ref skinFrames);
        }
    }

    public struct GameStateDataPacket : INetworkSerializable
    {

        public float[] mods;
        public ScoreManager.Mode currentGameMode;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mods);
            serializer.SerializeValue(ref currentGameMode);
        }
    }
    [ClientRpc]
    public void SendModsDataClientRpc(float[] mods)
    {
        if (IsHost) return;
        for (int modIndex = 0; modIndex < mods.Length; modIndex++) Mods.at[modIndex] = mods[modIndex];
    }
    private void FixedUpdate() => UpdatePlayerData();
    float clientConnectionsStatusTimer = 0;
    private void Update()
    {
        clientConnectionsStatusTimer += Time.deltaTime;
        if(clientConnectionsStatusTimer > 0.3f)
        {
            string status = "";
            
            foreach (var item in NetworkManager.Singleton.ConnectedClients)
            {
            
                status += $"|| {item.Value.ClientId}, {item.Key} ||";
            
            }
        
            Debug.Log(status);
            Debug.Log($"Is client: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"Is connected client: {NetworkManager.Singleton.IsConnectedClient}");
            
            clientConnectionsStatusTimer = 0;
        
        }

        rtt = (float)(NetworkManager.LocalTime.Time - NetworkManager.ServerTime.Time);    
        ping = rtt / 2;
    
    }
    float rbUpdate, clrUpdate, clrUpdate2;
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

    }
    
    void StorePlayerRigidBodyData(PlayerData player, byte[] data)
    {

        if ((byte)player.id == data[13])
        {

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

    }
    
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
    
    void StoreNozzleData(PlayerData player, byte[] comp)
    {
        if ((byte)player.id != comp[0]) return;

        (float fromX, float fromY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[1], comp[2] });
        (float toX, float toY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[3], comp[4] });

        player.square.fromPos = new Vector2(fromX, fromY);
        player.square.toPos = new Vector2(toX, toY);
        player.square.newNozzleLerp = 0;

    }
    
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
    
    void StoreColorData(PlayerData player, byte[] data)
    {
        if (player.id != data[0]) return;

        player.square.playerColor = new UnityEngine.Color(data[1] / 256f, data[2] / 256f, data[3] / 256f);
        player.square.newColor = true;

    }
    
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
    
    void StoreHealthData(PlayerData player, byte sourceId, float data)
    {
        if ((byte)player.id != sourceId) return;

        player.square.healthPoints = data;
    }
    
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

    
    void StoreScoreData(PlayerData player, byte sourceId, byte data)
    {

        if ((byte)player.id != sourceId) return;

        player.square.score = data;

    }

    
    public void UpdatePlayerReady(bool ready)
    {

        if (!localSquare) return;

        byte sourceId = (byte)localSquare.id;

        UpdatePlayerReadyRpc(sourceId, ready);

    }

    
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerReadyRpc(byte sourceId, bool ready)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            if (player.id == sourceId) StorePlayerReady(player, sourceId, ready);
        }

    }

    
    void StorePlayerReady(PlayerData player, byte sourceId, bool ready)
    {

        player.square.ready = ready;

    }
    
    public void UpdateSelectedMap(int map)
    {

        if (!IsHost) return;
        UpdateSelectedMapClientRpc(map);

    }
    
    [ClientRpc]
    void UpdateSelectedMapClientRpc(int map)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreSelectedMap(player, map);
        }

    }
    
    void StoreSelectedMap(PlayerData player, int map)
    {

        player.square.selectedMap = map;

    }

    public void UpdatePlayerHealth(byte id, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {

        if(IsHost) UpdatePlayerHealthClientRpc(id, damage, slowDownAmount, responsibleId, knockBack);
        else UpdatePlayerHealthServerRpc(id, damage, slowDownAmount, responsibleId, knockBack);

        UpdatePlayerHealthFunc(id, damage, slowDownAmount, responsibleId, knockBack);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerHealthServerRpc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {
        UpdatePlayerHealthClientRpc(affectedId, damage, slowDownAmount, responsibleId, knockBack);
    }

    [ClientRpc]
    public void UpdatePlayerHealthClientRpc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {
        if ((byte)localSquare.id == responsibleId) return;
        UpdatePlayerHealthFunc(affectedId, damage, slowDownAmount, responsibleId, knockBack);
    }

    void UpdatePlayerHealthFunc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {

        bool kill = false;

        foreach (PlayerData player in playerIdentities)
        {

            if ((byte)player.square.id == affectedId)
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

                    foreach (PlayerData player1 in playerIdentities) if ((byte)player1.id == responsibleId) player.square.killStreak++;

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

                if ((byte)player.id == responsibleId)
                {

                    player.square.score++;

                }

            }

        }

        if (affectedId == (byte)localSquare.id && !localSquare.isDead)
        {

            UpdateHealth();

        }

        if (responsibleId == (byte)localSquare.id)
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
    
    public void SpreadInGameMessage(string message)
    {

        byte playerId = (byte)localSquare.id;
        string sanetizedMessage = MyExtentions.SanitizeMessage(message);

        if (IsHost) SpreadInGameMessageClientRpc(sanetizedMessage, playerId);
        else SpreadInGameMessageServerRpc(sanetizedMessage, playerId);

        SpreadIngameMessageFunc(sanetizedMessage, playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    void SpreadInGameMessageServerRpc(string message, byte playerId)
    {
        SpreadInGameMessageClientRpc(message, playerId);
    }

    [ClientRpc]
    void SpreadInGameMessageClientRpc(string message, byte playerId)
    {
        if ((byte)localSquare.id == playerId) return;
        SpreadIngameMessageFunc(message, playerId);
    }

    void SpreadIngameMessageFunc(string message, byte playerId)
    {
        PlayerBehaviour source = null;
        MessageRecieverBehaviour messageReciever = null;

        foreach (PlayerData player in playerIdentities)
        {

            if (player.id == playerId) source = player.square;

        }

        messageReciever = FindAnyObjectByType<MessageRecieverBehaviour>();

        if (!source) return;
        if (!messageReciever) return;

        messageReciever.CreateNewMessage(message, source);
    }

    
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void SpreadInGameMessageRpc(string message, byte playerId)
    {

        PlayerBehaviour source = null;
        MessageRecieverBehaviour messageReciever = null;

        foreach (PlayerData player in playerIdentities)
        {

            if (player.id == playerId) source = player.square;

        }

        messageReciever = FindAnyObjectByType<MessageRecieverBehaviour>();

        if (!source) return;
        if (!messageReciever) return;

        messageReciever.CreateNewMessage(message, source);

    }
    
    public void SyncMods(int index, float value)
    {

        if (IsHost) SyncModsClientRpc(index, value);

    }
    
    [ClientRpc]
    void SyncModsClientRpc(int index, float value)
    {

        Mods.at[index] = value;

    }
    [Serializable]
    
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
    
}

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