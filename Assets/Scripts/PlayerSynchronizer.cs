using System;
using System.Collections.Generic; 
using System.Runtime.CompilerServices;
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BinaryVectors;

public unsafe sealed class PlayerSynchronizer : NetworkBehaviour
{
    PlayerFactorySynchronizer playerFactorySynchronizer;
    public static PlayerSynchronizer Instance;

    public SkinData skinData;

    public float ping;
    public float rtt;

    public List<PlayerData> playerIdentities;
    NetworkManager networkManager;

    [SerializeField]
    public PlayerBehaviour square;

    public PlayerBehaviour localSquare;

    MapStreamSynchronizer mapStreamSynchronizer;
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

    public NetworkList<ulong> playerIdList = new NetworkList<ulong>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    bool stopUpdate;

    public bool[] defaultSkin = new bool[116];

    
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        updatePFPStream = new List<UpdatePFPStream>();
        networkManager = GameObject.Find("Network").GetComponent<NetworkManager>();
        projectileManager = GetComponent<ProjectileManager>();
        localSteamData = GetComponent<LocalSteamData>();
        playerFactorySynchronizer = GetComponent<PlayerFactorySynchronizer>();

        networkManager.OnConnectionEvent += NetworkManager_OnConnectionEvent;
        networkManager.ConnectionApprovalCallback += ConnectionApproval;

        hunter = GetComponent<Hunter>();
        scoreManager = GetComponent<ScoreManager>();
        for (int i = 0; i < defaultSkin.Length; i++) defaultSkin[i] = true;
        
    }

    void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
    }

   

    private void NetworkManager_OnConnectionEvent(NetworkManager networkManager, ConnectionEventData arg2)
    {

        if (arg2.EventType == ConnectionEvent.PeerConnected) playerFactorySynchronizer.CreateNewPlayer(arg2.ClientId);
        if (arg2.EventType == ConnectionEvent.ClientConnected) playerFactorySynchronizer.CreateNewPlayer(arg2.ClientId);
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

        if(arg0.name != "MenuScene")
        {
            LoadSceneOnPlayersClientRpc(sceneIndex);
        }

        lastScene = arg0;
    }

    
    [Rpc(SendTo.NotMe)]
    void LoadSceneOnPlayersClientRpc(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn) localSquare.transform.position = spawn.transform.position;
    }

    
    public void DisconnectPlayer(ulong id)
    {

        if (IsHost) DisconnectPlayerRemotely(id);

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

        projectileManager.ClearAllProjectilesFromOwner(id);

        if (playerToRemove.square)
        {
            Destroy(playerToRemove.square.gameObject);
            playerIdentities = refreshedIdentities;
            DisconnectPlayerRemotelyClientRpc(id);
            playerIdList.Remove(id);
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

        projectileManager.ClearAllProjectilesFromOwner(id);

        Destroy(playerToRemove.square.gameObject);
        playerIdentities = refreshedIdentities;

    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void KickPlayerClientRpc(byte id)
    {

        if (localSquare.GetID() != id) return;

        DisconnectPlayerLocally();

    }

    
    public void DisconnectPlayerLocally()
    {
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

/*    public void CreateNewPlayer(ulong id)
    {

        if (!IsHost) return;
        GameStateDataPacket currentGameState = new GameStateDataPacket();

        currentGameState.currentGameMode = scoreManager.gameMode;
        currentGameState.mods = (float[]) Mods.at.Clone();

        RoundTripCollectorClientRpc(currentGameState);
    }*/
/*
    bool FetchSkinValidity()
    {
        bool skinValidCheck = true;
        foreach (var frame in skinData.skinFrames) skinValidCheck = frame.valid && skinValidCheck;
        return skinValidCheck;
    }
    int FetchFrameCount() => FetchSkinValidity() ? skinData.frames : 1;
    float FetchFrameAnimation() => FetchSkinValidity() ? skinData.frameRate : 0F;
    byte[] FetchFramePixels() => FetchSkinValidity() ? GetCustomSkin() : MyExtentions.BoolArrayToByteArray(defaultSkin);*/
/*    byte[] GetCustomSkin()
    {
        byte[] frameBuffer;
        List<byte> collectedSkinData = new List<byte>();
        foreach (SkinData.SkinFrame frame in skinData.skinFrames)
        {
            frameBuffer = MyExtentions.BoolArrayToByteArray(frame.frame);
            collectedSkinData.AddRange(frameBuffer);
        }
        return collectedSkinData.ToArray();
    }*/

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

    [ClientRpc] public void SendModsDataClientRpc(float[] mods)
    {
        if (IsHost) return;
        for (int modIndex = 0; modIndex < mods.Length; modIndex++) Mods.at[modIndex] = mods[modIndex];
    }
    private void FixedUpdate() => UpdatePlayerData();
    private void Update()
    {

        rtt = (float)(NetworkManager.LocalTime.Time - NetworkManager.ServerTime.Time);    
        ping = rtt / 2;
    
    }

    public float clrUpdate, clrUpdate2;
    bool rbFlip = false;
    void UpdatePlayerData()
    {

        float deltaTime = Time.deltaTime;
        rbFlip = !rbFlip;

        if (stopUpdate) return;
        if (localSquare == null) return;
        if (playerIdentities == null) return;

        if(rbFlip) UpdateRigidBody();

        if (clrUpdate > 0)
        {
            UpdateColor();
            UpdatePlayerReady(localSquare.ready);
            clrUpdate -= Time.deltaTime;
        }
    }

    public void UpdateRigidBody()
    {

        byte[] data = MyExtentions.CompressRigidbody(localSquare.rb);

        UpdateRigidBodyRpc(data, localSquare.GetID());

    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    void UpdateRigidBodyRpc(byte[] data, byte id)
    {
        if (localSquare.GetID() == id) return;
        if (playerIdentities == null) return;
        PlayerBehaviour player = null;
        player = GetPlayerById(id);
        if(player) StorePlayerRigidBodyData(player, data);
    }

    void StorePlayerRigidBodyData(PlayerBehaviour player, byte[] data)
    { 
        if (!player.isDead) MyExtentions.DecompressRigidbody(data, player.rb, 200f); 
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
        PlayerBehaviour player = null;
        player = GetPlayerById(data[0]);
        if (player) StoreNozzleData(player, data);
    }
    
    void StoreNozzleData(PlayerBehaviour player, byte[] comp)
    {

        (float fromX, float fromY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[1], comp[2] });
        (float toX, float toY) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[3], comp[4] });

        player.fromPos = new Vector2(fromX, fromY);
        player.toPos = new Vector2(toX, toY);
        player.newNozzleLerp = 0;

    }
    
    public void UpdateColor()
    {
        ulong sourceId = networkManager.LocalClientId;
        byte[] data = new byte[2]
        {
            (byte) sourceId,
            (byte) math.round(localSquare.PlayerColor.ReadColorHue * 256)
        };

        UpdateColortRpc(data); 
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
    void UpdateColortRpc(byte[] data)
    {
        if (networkManager.LocalClientId == data[0]) return;
        if (playerIdentities == null) return;
        PlayerBehaviour player = null;
        player = GetPlayerById(data[0]);
        if (player) StoreColorData(player, data);
    }
    
    void StoreColorData(PlayerBehaviour player, byte[] data)
    {
        player.PlayerColor.SetColorHue(data[1] / 256f);
        player.newColor = true;
    }
    
    public void UpdateHealth()
    {
        byte sourceId = (byte)networkManager.LocalClientId;
        UpdateHealthRpc(sourceId, localSquare.healthPoints);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void UpdateHealthRpc(byte sourceId, float data)
    {
        if ((byte)networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;
        PlayerBehaviour player = null;
        player = GetPlayerById(sourceId);
        if (player) StoreHealthData(player, sourceId, data);
    }
    
    void StoreHealthData(PlayerBehaviour player, byte sourceId, float data)
    {

        player.healthPoints = data;
    }
    
    public void UpdateScore()
    {
        byte sourceId = (byte)networkManager.LocalClientId;
        byte data = (byte)localSquare.score;

        UpdateScoreRpc(sourceId, data);

    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void UpdateScoreRpc(byte sourceId, byte data)
    {
        if ((byte)networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;
        PlayerBehaviour player = null;
        player = GetPlayerById(sourceId);
        if (player) StoreScoreData(player, sourceId, data);
    }

    
    void StoreScoreData(PlayerBehaviour player, byte sourceId, byte data)
    {

        player.score = data;

    }

    
    public void UpdatePlayerReady(bool ready)
    {

        if (!localSquare) return;

        byte sourceId = (byte)localSquare.id;

        if (IsHost) UpdatePlayerReadyClientRpc(sourceId, ready);
        else UpdatePlayerReadyServerRpc(sourceId, ready);

        //UpdatePlayerReadyRpc(sourceId, ready);

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void UpdatePlayerReadyServerRpc(byte sourceId, bool ready)
    {
        UpdatePlayerReadyClientRpc(sourceId, ready);
    }

    [ClientRpc]
    void UpdatePlayerReadyClientRpc(byte sourceId, bool ready)
    { 
        UpdatePlayerReadyFinal(sourceId, ready); 
    }

    void UpdatePlayerReadyFinal(byte sourceId, bool ready)
    {

        if (playerIdentities == null) return;
        PlayerBehaviour player = null;
        player = GetPlayerById(sourceId);
        if (player) StorePlayerReady(player, sourceId, ready);

    }

    
    void StorePlayerReady(PlayerBehaviour player, byte sourceId, bool ready) => player.ready = ready;

    public void FetchMapOnJoin()
    {
    }

    public void UpdateSelectedMap(int map, bool legacy)
    {

        if (!IsHost) return;
        UpdateSelectedMapClientRpc(map, legacy);

    }
    
    [ClientRpc]
    void UpdateSelectedMapClientRpc(int map, bool legacy)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreSelectedMap(player, map, legacy);
        }

    }
    
    void StoreSelectedMap(PlayerData player, int map, bool legacy)
    {

        player.square.selectedMap = map;
        player.square.selectedLegacyMap = legacy;

    }

    public void UpdatePlayerHealth(byte id, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {

        if(IsHost) UpdatePlayerHealthClientRpc(id, damage, slowDownAmount, responsibleId, knockBack);
        else UpdatePlayerHealthServerRpc(id, damage, slowDownAmount, responsibleId, knockBack);

        UpdatePlayerHealthFunc(id, damage, slowDownAmount, responsibleId, knockBack);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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

        PlayerBehaviour affectedPlayer = null;
        PlayerBehaviour responsiblePlayer = null;

        affectedPlayer = GetPlayerById(affectedId);
        responsiblePlayer = GetPlayerById(responsibleId);


        if (affectedPlayer)
        {

            if (!affectedPlayer.isDead)
            {

                affectedPlayer.rb.AddForce(knockBack, ForceMode2D.Impulse);
                affectedPlayer.healthPoints -= damage;
                affectedPlayer.healthPoints = math.clamp(affectedPlayer.healthPoints, 0, affectedPlayer.maxHealthPoints);

                affectedPlayer.rb.linearDamping = math.clamp(affectedPlayer.rb.linearDamping + slowDownAmount, 0.1f, 100f);
                affectedPlayer.rb.angularDamping = math.clamp(affectedPlayer.rb.angularDamping + slowDownAmount, 0.1f, 100f);

            }

            if (affectedPlayer.healthPoints <= 0 && !affectedPlayer.isDead)
            {

                if (responsiblePlayer) affectedPlayer.killStreak++;

                kill = true;
                PlayerDeathEffect(affectedPlayer);
                hunter.Kill(affectedId, responsibleId);
                affectedPlayer.KillPlayer();

            }

        }


        UpdateScore();

        if (kill && 
            scoreManager.gameMode == ScoreManager.Mode.DM && 
            responsiblePlayer.id == localSquare.id &&
            scoreManager.inGame)
        {

            if (responsiblePlayer) responsiblePlayer.score++;

        }

        if (affectedPlayer.id == localSquare.id && !localSquare.isDead) UpdateHealth();

        if (responsiblePlayer.id == localSquare.id) UpdateScore();

    }

    public void PlayerDeathEffect(PlayerBehaviour deadPlayer)
    {

        localSquare.deathSoundInstance.setVolume(MySettings.Volume);
        localSquare.deathSoundInstance.start();

        GameObject newParticle = Instantiate(deathParticles, deadPlayer.rb.position, Quaternion.Euler(0, 0, 0), null);

        ParticleSystemRenderer[] particleSystemRenderers = newParticle.GetComponentsInChildren<ParticleSystemRenderer>();
        ParticleSystem[] particleSystems = newParticle.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < particleSystems.Length; i++)
        {
            deadPlayer.PlayerColor.AssignMaterialToParticleRenderer(particleSystemRenderers[i], particleSystems[i]);
        }
    }
    
    public Color UpdatePlayerColor(float value)
    {

        localSquare.PlayerColor.SetColorHue(value);
        localSquare.newColor = true;

        UpdateColor();

        return localSquare.PlayerColor.PrimaryColor;

    }
    
    public void SpreadInGameMessage(string message)
    {

        byte playerId = (byte)localSquare.id;
        string sanetizedMessage = MyExtentions.SanitizeMessage(message);

        if (IsHost) SpreadInGameMessageClientRpc(sanetizedMessage, playerId);
        else SpreadInGameMessageServerRpc(sanetizedMessage, playerId);

        SpreadIngameMessageFunc(sanetizedMessage, playerId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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

        source = GetPlayerById(playerId);

        messageReciever = FindAnyObjectByType<MessageRecieverBehaviour>();

        if (!source) return;
        if (!messageReciever) return;

        messageReciever.CreateNewMessage(message, source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PlayerBehaviour GetPlayerById(byte id)
    {
        foreach (PlayerData player in playerIdentities) if ((byte)player.id == id) return player.square;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PlayerBehaviour GetPlayerById(ulong id)
    {
        foreach (PlayerData player in playerIdentities) if (player.id == id) return player.square;
        return null;
    }


    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SpreadInGameMessageRpc(string message, byte playerId)
    {

        PlayerBehaviour source = null;
        MessageRecieverBehaviour messageReciever = null;

        source = GetPlayerById(playerId);

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

    public PlayerBehaviour GetClosestPlayer(Vector2 from)
    {
        PlayerBehaviour closest = null;
        float closestDistSqr = Mathf.Infinity;

        for (int i = 0; i < playerIdentities.Count; i++)
        {
            Vector2 playerPos = playerIdentities[i].square.rb.position;
            float distSqr = (playerPos - from).sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = playerIdentities[i].square;
            }
        }

        return closest;
    }

    public void SyncMMR()
    {
        if(IsHost) FetchMMRRpc();
    }

    [Rpc(SendTo.Everyone)]
    void FetchMMRRpc()
    {
        localSquare.StorePreviousMMR();
        StoreMMRRpc(localSquare.GetID(), localSquare.MMR);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone)]
    void StoreMMRRpc(byte playerId, double mmr)
    {
        PlayerBehaviour player = GetPlayerById(playerId);
        player.MMR = mmr;
        player.StorePreviousMMR();
    }


    public void CalculateMMR()
    {
        MMRData[] mMRs = GetPlayerMMRArr();
        SetPlayerMMrArr(MMRSystem.ComputeMMR(mMRs));
    }

    public MMRData[] GetPlayerMMRArr()
    {
        MMRData[] data = new MMRData[playerIdentities.Count];
        for(int i = 0; i < data.Length; i++)
        {
            PlayerBehaviour player = playerIdentities[i].square;
            data[i] = new MMRData()
            {
                UserUniqueId = player.id,
                MMR = player.MMR,
                previousMatchUserScore = player.score,
            };
        }
        return data;
    }

    public void SetPlayerMMrArr(MMRData[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            PlayerBehaviour player = GetPlayerById(arr[i].UserUniqueId);
            player.MMR = arr[i].MMR;
        }
    }

    public void SpawnJumpParticles(Vector2 pos, float rot, byte playerId)
    {

        SByte3 particleCompressor = ProjectileManager.GetParticleCompressor;
        particleCompressor.SetFromVec3(new Vector3(pos.x, pos.y, rot));

        byte[] data = particleCompressor.GetByte3().data;

        SpawnJumpParticlesRpc(data, playerId);
        SpawnJumpParticlesEvent(data, playerId);

    }
    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    void SpawnJumpParticlesRpc(byte[] data, byte playerId)
    {
        SpawnJumpParticlesEvent(data, playerId);
    }

    void SpawnJumpParticlesEvent(byte[] data, byte playerId)
    {
        PlayerBehaviour player = GetPlayerById((byte)playerId);
        if (!player) return;
        
        SByte3 particleCompressor = ProjectileManager.GetParticleCompressor;
        particleCompressor.SetFromByteArr(data);
        Vector3 decom = particleCompressor.GetVec3();


        ParticleBehaviour particleBehaviour = player.jumpParticleRef;
        Vector3 position = new Vector2(decom.x, decom.y);
        Quaternion rotation = Quaternion.Euler(0, 0, decom.z);
        particleBehaviour = ParticlePool.Spawn(particleBehaviour, position, rotation);
        int l = particleBehaviour.ParticleSystemRenderers.Length;
        for (int i = 0; i < l; i++)
        {
            ParticleSystem particleSystem = particleBehaviour.ParticleSystems[i];
            ParticleSystemRenderer particleSystemRenderer = particleBehaviour.ParticleSystemRenderers[i];
            player.PlayerColor.AssignMaterialToParticleRenderer(particleSystemRenderer, particleSystem);
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
