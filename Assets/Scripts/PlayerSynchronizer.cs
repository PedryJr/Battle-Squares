using Netcode.Transports.Facepunch; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using static BinaryVectors;

public sealed class PlayerSynchronizer : NetworkBehaviour
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
    ParticleBehaviour deathParticles;

    Hunter hunter;

    public bool hostShutdown = false;

    Scene lastScene;

    delegate void UpdatePFPStream();
    List<UpdatePFPStream> updatePFPStream;

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
        playerPool = new List<PlayerBehaviour>();

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
        if(id == NetworkManager.LocalClientId)
        {
            //Host probably had an alt f4 moment..
            DisconnectPlayerLocally();
        }
        else
        {
            if (!IsHost) return;
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
        List<PlayerData> playersToRemove = new List<PlayerData>();

        if (playerIdentities != null)
        {
            foreach (PlayerData player in playerIdentities)
            {

                if (player.square.GetNetworkID() == id)
                {
                    playersToRemove.Add(player);
                }
                else refreshedIdentities.Add(player);

            }
        }

        foreach (PlayerData player in playersToRemove)
        {
            projectileManager.ClearAllProjectilesFromOwner(player.square.GetGameID());

            if (player.square)
            {
                Destroy(player.square.gameObject);
            }
        }
        
        playerIdentities = refreshedIdentities;
        DisconnectPlayerRemotelyClientRpc(id);
    }

    

    [ClientRpc]
    public void DisconnectPlayerRemotelyClientRpc(ulong id)
    {

        if (IsHost) return;

        List<PlayerData> refreshedIdentities = new List<PlayerData>();
        List<PlayerData> playersToRemove = new List<PlayerData>();

        if (playerIdentities != null)
        {
            foreach (PlayerData player in playerIdentities)
            {

                if (player.square.GetNetworkID() == id) playersToRemove.Add(player);
                else refreshedIdentities.Add(player);

            }
        }

        foreach (PlayerData player in playersToRemove)
        {
            projectileManager.ClearAllProjectilesFromOwner(player.square.GetGameID());

            if (player.square) Destroy(player.square.gameObject);
        }

        playerIdentities = refreshedIdentities;

    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void KickPlayerClientRpc(byte id)
    {
        if (localSquare.GetNetworkID() != id) return;
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
        foreach (ProjectileBehaviour projectile in projectileManager.projectiles) if (projectile != null) Destroy(projectile.gameObject);
        projectileManager.projectiles.Clear();
        if (playerIdentities != null) foreach (PlayerData player in playerIdentities) Destroy(player.square.gameObject);
        playerIdentities.Clear();
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

        if (clrUpdate > 0)
        {
            UpdateColor();
            UpdatePlayerReady(localSquare.ready);
            clrUpdate -= Time.deltaTime;
        }
    }

    public void UpdateRigidBody(byte playerId)
    {
        PlayerBehaviour player = GetPlayerById(playerId);
        if (!player) return;
        if (!player.isLocalPlayer) return;

        Vector2 pos = player.rb.position;
        Vector2 vel = player.rb.linearVelocity;
        float ang = player.rb.rotation;
        float angvel = player.rb.angularVelocity;

        UpdateRigidBodyRpc(pos, vel, ang, angvel, player.GetGameID());
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    void UpdateRigidBodyRpc(Vector2 pos, Vector2 vel, float ang, float angvel, byte id)
    {
        
        if (playerIdentities == null)
        {
            VLog.Log("playerIdentities is null...");
            return;
        }
        PlayerBehaviour player = GetPlayerById(id);
        if (player == null) return;
        StorePlayerRigidBodyData(player, pos, vel, ang, angvel);
    }

    void StorePlayerRigidBodyData(PlayerBehaviour player, Vector2 pos, Vector2 vel, float ang, float angvel)
    {
        if(!player.isDead)
        {
            player.rb.position = pos;
            player.rb.linearVelocity = vel;
            player.rb.rotation = ang;
            player.rb.angularVelocity = angvel;
        } 
    }

    public void UpdateNozzle(byte playerID)
    {
        PlayerBehaviour player = GetPlayerById(playerID);
        if (!player) return; 
        byte[] data = new byte[2] { player.GetGameID(), (byte) player.aimDirectionEnum }; 
        UpdateNozzleRpc(data);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void UpdateNozzleRpc(byte[] data)
    {
        if (playerIdentities == null) return;
        PlayerBehaviour player = GetPlayerById(data[0]);
        if (player == null) return;
        player.aimDirectionEnum = (PlayerBehaviour.AimDirection) data[1];
    }
    
    public void UpdateColor()
    {
        foreach (var item in playerIdentities)
        {
            PlayerBehaviour player = item.square;
            if (!player) continue;
            if (!player.isLocalPlayer) continue;

            ulong sourceId = player.GetGameID();
            byte[] data = new byte[2]
            {
                (byte) sourceId,
                (byte) math.round(player.PlayerColor.ReadColorHue * 256)
            };
            UpdateColortRpc(data); 
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    void UpdateColortRpc(byte[] data)
    {
        if (playerIdentities == null) return;

        foreach (var pData in playerIdentities)
        {
            if (pData.square.GetGameID() != data[0]) continue;
            StoreColorData(pData.square, data);
        }
    }

    void StoreColorData(PlayerBehaviour player, byte[] data)
    {
        player.PlayerColor.SetColorHue(data[1] / 256f);
        player.newColor = true;
    }

    public void UpdateHealth(byte targetGameID)
    {
        foreach (var item in playerIdentities)
        {
            if (!item.square.isLocalPlayer) continue;
            
            byte sourceId = item.square.GetGameID();

            if (sourceId != targetGameID) continue;

            UpdateHealthRpc(sourceId, item.square.healthPoints);
        }
    }

    public void UpdateHealth()
    {
        foreach (var item in playerIdentities)
        {
            if(!item.square.isLocalPlayer) continue;
            byte sourceId = item.square.GetGameID();
            UpdateHealthRpc(sourceId, item.square.healthPoints);
        }
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void UpdateHealthRpc(byte sourceId, float data)
    {
        if (playerIdentities == null) return;

        PlayerBehaviour player = GetPlayerById(sourceId);
        if (player == null) return;
        if (player.isLocalPlayer) return;

        StoreHealthData(player, sourceId, data);
    }

    void StoreHealthData(PlayerBehaviour player, byte sourceId, float data)
    {
        player.healthPoints = data;
    }
    
    public void UpdateScore()
    {
        foreach (var item in playerIdentities)
        {
            if (!item.square.isLocalPlayer) continue;
            byte sourceId = item.square.GetGameID();
            byte data = (byte)localSquare.score;

            UpdateScoreRpc(sourceId, data);
        }

    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void UpdateScoreRpc(byte sourceId, byte data)
    {
        if (playerIdentities == null) return;

        PlayerBehaviour player = GetPlayerById(sourceId);
        if (player == null) return;
        if (player.isLocalPlayer) return;
        StoreScoreData(player, sourceId, data);
    }


    void StoreScoreData(PlayerBehaviour player, byte sourceId, byte data)
    {

        player.score = data;

    }

    
    public void UpdatePlayerReady(bool ready)
    {

        if (!localSquare) return;

        byte sourceId = localSquare.GetNetworkID();

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
        foreach (var item in playerIdentities)
        {
            if (item.square.GetNetworkID() != sourceId) continue;
            if (playerIdentities == null) return;
            PlayerBehaviour player = item.square;
            if (player) StorePlayerReady(player, sourceId, ready);
        }
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
        UpdatePlayerHealthServerRpc(id, damage, slowDownAmount, responsibleId, knockBack);
        UpdatePlayerHealthFunc(id, damage, slowDownAmount, responsibleId, knockBack);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdatePlayerHealthServerRpc(byte affectedId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {
        UpdatePlayerHealthFunc(affectedId, damage, slowDownAmount, responsibleId, knockBack);
    }

    void UpdatePlayerHealthFunc(byte victimId, float damage, float slowDownAmount, byte responsibleId, Vector2 knockBack)
    {

        bool kill = false;

        PlayerBehaviour affectedPlayer = null;
        PlayerBehaviour responsiblePlayer = null;

        affectedPlayer = GetPlayerById(victimId);
        responsiblePlayer = GetPlayerById(responsibleId);

        if (affectedPlayer)
        {
            if (!affectedPlayer.isDead)
            {

                affectedPlayer.rb.AddForce(knockBack, ForceMode2D.Impulse);
                affectedPlayer.healthPoints = math.clamp(affectedPlayer.healthPoints - damage, 0, affectedPlayer.maxHealthPoints);

                affectedPlayer.rb.linearDamping = math.clamp(affectedPlayer.rb.linearDamping + slowDownAmount, 0.1f, 100f);
                affectedPlayer.rb.angularDamping = math.clamp(affectedPlayer.rb.angularDamping + slowDownAmount, 0.1f, 100f);

            }

            if (affectedPlayer.healthPoints <= 0 && !affectedPlayer.isDead)
            {

                if (responsiblePlayer) affectedPlayer.killStreak++;

                kill = true;
                PlayerDeathEffect(affectedPlayer);
                hunter.Kill(victimId, responsibleId);
                affectedPlayer.KillPlayer();

            }

        }


        UpdateScore();

        if (kill && 
            scoreManager.gameMode == ScoreManager.Mode.DM && 
            responsiblePlayer.isLocalPlayer &&
            scoreManager.inGame)
        {

            Debug.Log("Score Increment!");

            if (responsiblePlayer) responsiblePlayer.score++;
        }

        if (affectedPlayer.isLocalPlayer && responsiblePlayer.isLocalPlayer && !affectedPlayer.isDead) UpdateHealth();
        if (responsiblePlayer.isLocalPlayer) UpdateScore();

    }

    public void PlayerDeathEffect(PlayerBehaviour deadPlayer)
    {

        localSquare.deathSoundInstance.setVolume(MySettings.Volume);
        localSquare.deathSoundInstance.start();

        ParticleBehaviour newParticle = AutoPooledPool<ParticleBehaviour>.Spawn(deathParticles, deadPlayer.rb.position, Quaternion.Euler(0, 0, 0), null);

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

        byte playerId = (byte)localSquare.GetGameID();
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
        if ((byte)localSquare.GetGameID() == playerId) return;
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
        foreach (PlayerData player in playerIdentities) if (player.square.GetGameID() == id) return player.square;
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

        //public ulong id;
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

    public PlayerBehaviour GetClosestPlayer(Vector2 from, bool includeDead = true)
    {
        if (!mlTrainer) mlTrainer = GetComponent<MLTrainingManager>();
        PlayerBehaviour closest = null;
        float closestDistSqr = float.MaxValue;
        var players = playerIdentities;
        int count = players.Count;
        PlayerBehaviour player;
        for (int i = 0; i < count; i++)
        {
            player = players[i].square;
            if (mlTrainer.isTraining && localSquare.GetGameID() == player.GetGameID()) continue;
            if (!includeDead && player.isDead) continue;
            Vector2 playerPos = player.position;
            float diffX = from.x - playerPos.x;
            float diffY = from.y - playerPos.y;
            float distSqr = diffX * diffX + diffY * diffY;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = player;
            }
        }
        return closest;
    }

    MLTrainingManager mlTrainer = null;

    List<PlayerBehaviour> playerPool;

    public PlayerBehaviour GetRandomPlayer(Vector2 _ignore, byte exclude, bool includeDead = true)
    {
        if (!mlTrainer) mlTrainer = GetComponent<MLTrainingManager>();

        // 1. Create a temporary list to hold players who pass the filters
        playerPool.Clear();

        var players = playerIdentities;
        int count = players.Count;

        for (int i = 0; i < count; i++)
        {
            PlayerBehaviour player = players[i].square;

            // Apply your existing filters
            if (mlTrainer.isTraining && localSquare.GetGameID() == player.GetGameID()) continue;
            if (player.GetGameID() == exclude) continue;
            if (!includeDead && player.isDead) continue;

            // 2. If they pass, add them to the pool
            playerPool.Add(player);
        }

        // 3. Return a random entry from the valid pool, or null if empty
        if (playerPool.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, playerPool.Count);
        return playerPool[randomIndex];
    }

    public PlayerBehaviour GetFarthestPlayer(Vector2 from, byte exclude, bool includeDead = true)
    {
        if (!mlTrainer) mlTrainer = GetComponent<MLTrainingManager>();

        PlayerBehaviour furthest = null;
        float furthestDistSqr = float.MinValue;
        var players = playerIdentities;
        int count = players.Count;
        PlayerBehaviour player;
        for (int i = 0; i < count; i++)
        {
            player = players[i].square;
            if (mlTrainer.isTraining && localSquare.GetGameID() == player.GetGameID()) continue;
            if (player.GetGameID() == exclude) continue;
            if (!includeDead && player.isDead) continue;
            Vector2 playerPos = player.position;
            float diffX = from.x - playerPos.x;
            float diffY = from.y - playerPos.y;
            float distSqr = diffX * diffX + diffY * diffY;
            if (distSqr > furthestDistSqr)
            {
                furthestDistSqr = distSqr;
                furthest = player;
            }
        }
        return furthest;
    }

    public PlayerBehaviour GetClosestPlayer(Vector2 from, byte exclude, bool includeDead = true)
    {
        if(!mlTrainer) mlTrainer = GetComponent<MLTrainingManager>();

        PlayerBehaviour closest = null;
        float closestDistSqr = float.MaxValue;
        var players = playerIdentities;
        int count = players.Count;
        PlayerBehaviour player;
        for (int i = 0; i < count; i++)
        {
            player = players[i].square;
            if (mlTrainer.isTraining && localSquare.GetGameID() == player.GetGameID()) continue;
            if (player.GetGameID() == exclude) continue;
            if (!includeDead && player.isDead) continue;
            Vector2 playerPos = player.position;
            float diffX = from.x - playerPos.x;
            float diffY = from.y - playerPos.y;
            float distSqr = diffX * diffX + diffY * diffY;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = player;
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
        StoreMMRRpc(localSquare.GetGameID(), localSquare.MMR);
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
                UserUniqueId = player.GetGameID(),
                MMR = player.MMR,
                previousMatchUserScore = player.score,
            };
        }
        return data;
    }

    public void SetPlayerMMrArr(MMRData[] arr)
    {
        if (playerIdentities.Any(e => e.square.GetNetworkID() == localSquare.GetNetworkID() && e.square.GetGameID() != localSquare.GetGameID())) return;
        for (int i = 0; i < arr.Length; i++)
        {
            PlayerBehaviour player = GetPlayerById((byte)arr[i].UserUniqueId);
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
        PlayerBehaviour player = GetPlayerById(playerId);
        if (!player) return;
        
        SByte3 particleCompressor = ProjectileManager.GetParticleCompressor;
        particleCompressor.SetFromByteArr(data);
        Vector3 decom = particleCompressor.GetVec3();


        ParticleBehaviour particleBehaviour = player.jumpParticleRef;
        Vector3 position = new Vector2(decom.x, decom.y);
        Quaternion rotation = Quaternion.Euler(0, 0, decom.z);
        particleBehaviour = AutoPooledPool<ParticleBehaviour>.Spawn(particleBehaviour, position, rotation);
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
