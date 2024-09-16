using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI.Table;

public sealed class PlayerSynchronizer : NetworkBehaviour
{

    public List<PlayerData> playerIdentities;
    public List<IdPair> idPairs;
    NetworkManager networkManager;

    [SerializeField]
    GameObject square;

    public PlayerBehaviour localSquare;

    ProjectileManager projectileManager;
    LocalSteamData localSteamData;
    ScoreManager scoreManager;

    float serverUpdateTimer;

    [SerializeField]
    GameObject deathParticles;

    public bool hostShutdown = false;

    delegate void UpdatePFPStream();
    List<UpdatePFPStream> updatePFPStream;

    bool stopUpdate;
    private void Awake()
    {

        DontDestroyOnLoad(this);
        updatePFPStream = new List<UpdatePFPStream>();
        networkManager = GameObject.Find("Network").GetComponent<NetworkManager>();
        projectileManager = GetComponent<ProjectileManager>();
        localSteamData = GetComponent<LocalSteamData>();
        networkManager.OnClientConnectedCallback += SetupNewPlayer;
        networkManager.OnClientDisconnectCallback += DisconnectPlayer;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

        scoreManager = GetComponent<ScoreManager>();

    }

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
        }
        else if (arg0.name == "LobbyScene")
        {
            Invoke("LateHudInit", 0.3f);
            stopUpdate = false;
        }

        PlayerController.uiRegs = 0;

        if (!IsHost) return;
        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn && localSquare) localSquare.transform.position = spawn.transform.position;
        int sceneIndex = arg0.buildIndex;
        LoadSceneOnPlayersClientRpc(sceneIndex);

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

                if (player.id == id) playerToRemove = player;
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

    public void DisconnectPlayerLocally()
    {

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
/*
    void SetupPlayerIdsStep1()
    {
        SetupPlayerIdsStep1ClientRpc();
    }

    [ClientRpc]
    void SetupPlayerIdsStep1ClientRpc()
    {

        foreach (IdPair pair in idPairs)
        {
            SetupPlayerIdsStep1ServerRpc(pair.clientId, pair.steamId);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    void SetupPlayerIdsStep1ServerRpc(ulong clientId, ulong steamId)
    {

        bool addId = true;
        foreach (IdPair pair in idPairs)
        {
            if (pair.clientId == clientId) addId = false;
        }
        if (addId)
        {
            idPairs.Add(new IdPair { clientId = clientId, steamId = new SteamId { Value = steamId } });
        }

        foreach (IdPair pair in idPairs)
        {
            SetupPlayerIdsStep2ClientRpc(pair.clientId, pair.steamId);
        }

    }

    [ClientRpc]
    void SetupPlayerIdsStep2ClientRpc(ulong clientId, ulong steamId)
    {

        bool addId = true;
        foreach (IdPair pair in idPairs)
        {
            if (pair.clientId == clientId) addId = false;
        }
        if (addId)
        {
            idPairs.Add(new IdPair { clientId = clientId, steamId = new SteamId { Value = steamId } });
        }

        for (int i = 0; i < playerIdentities.Count; i++)
        {

            if (playerIdentities[i].pfp == null)
            {

                foreach (IdPair pair in idPairs)
                {
                    if (pair.clientId == playerIdentities[i].cId)
                    {
                        PlayerData newData = playerIdentities[i];

                        newData.pfp = MyExtentions.GetImageData(pair.steamId);
                        foreach (Friend friend in SteamNetwork.currentLobby.Value.Members)
                        {
                            if (friend.Id.Value == pair.steamId.Value) newData.name = friend.Name;
                        }

                        playerIdentities[i] = newData;

                    }
                }

            }

        }

    }*/
    public void SetupNewPlayer(ulong id)
    {
        clrUpdate2 = 0;
        if (IsHost)
        {

            PlayerData playerData = new PlayerData();
            playerData.id = id;
            playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
            playerData.square.id = id;
            playerData.pfp = MyExtentions.CreateSpriteFromData(localSteamData.pfpData, localSteamData.imageWidth, localSteamData.imageHeight);
            playerData.texture = playerData.pfp.texture;

            if (playerIdentities == null)
            {
                playerIdentities = new List<PlayerData>();
                idPairs = new List<IdPair>
                {
                    new IdPair { clientId = id, steamId = SteamClient.SteamId }
                };
                SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Single);

            }

            playerIdentities.Add(playerData);

            List<ulong> nowPlayers = new List<ulong>();
            List<bool> nowReady = new List<bool>();

            if (playerData.id == NetworkManager.LocalClientId)
            {


                localSquare = playerData.square;
                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

            }
            foreach (PlayerData player in playerIdentities)
            {

                nowPlayers.Add(player.id);
                nowReady.Add(player.square.ready);

            }

            SetupNewPlayerClientRpc(nowPlayers.ToArray(), nowReady.ToArray(), id, scoreManager.gameMode);

            FetchPFPDataRpc();

            /*SetupPlayerIdsStep1();*/

        }

    }


    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void FetchPFPDataRpc()
    {

        Texture2D texture = localSteamData.pfp.texture;

        // Get the pixel data from the texture
        byte[] textureData = texture.GetRawTextureData();

        int batchSize = 50;
        int pixelCount = texture.width * texture.height; // Assuming you want all pixels
        int pixelIndex = 0;

        while (pixelIndex < pixelCount)
        {
            // Prepare batches of pixels (for this example, we assume the texture is in RGBA format, so 4 bytes per pixel)
            int remainingPixels = pixelCount - pixelIndex;
            int currentBatchSize = Mathf.Min(batchSize, remainingPixels);

            byte[] rgbBatch = new byte[currentBatchSize * 4]; // 4 bytes per pixel (RGBA)
            int[] posXBatch = new int[currentBatchSize];
            int[] posYBatch = new int[currentBatchSize];

            for (int i = 0; i < currentBatchSize; i++)
            {
                int pixelPos = pixelIndex + i;

                // Convert pixelPos into x and y coordinates
                int x = pixelPos % texture.width;
                int y = pixelPos / texture.width;

                // Store the pixel data and positions
                Buffer.BlockCopy(textureData, pixelPos * 4, rgbBatch, i * 4, 4);
                posXBatch[i] = x;
                posYBatch[i] = y;
            }

            // Add the batch to the update stream
            UpdatePFPStream stream = () =>
            {
                StreamPFPDataRpc(rgbBatch, posXBatch, posYBatch, NetworkManager.LocalClientId);
            };

            updatePFPStream.Add(stream);

            pixelIndex += currentBatchSize;
        }

        // Add final stream to apply data
        UpdatePFPStream endStream = () =>
        {
            ApplyPFPDataRpc(NetworkManager.LocalClientId);
        };
        updatePFPStream.Add(endStream);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void StreamPFPDataRpc(byte[] pixelBatch, int[] xBatch, int[] yBatch, ulong id)
    {

        if(playerIdentities != null)
        {

            for (int i = 0; i < playerIdentities.Count; i++)
            {

                if (playerIdentities[i].id == id)
                {
                    PlayerData playerData = playerIdentities[i];

                    // Apply the batch of pixel data
                    for (int j = 0; j < xBatch.Length; j++)
                    {
                        playerData.UpdatePFP(xBatch[j], yBatch[j], pixelBatch.Skip(j * 4).Take(4).ToArray()); // RGBA, 4 bytes per pixel
                    }

                    playerIdentities[i] = playerData;
                }

            }

        }

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void ApplyPFPDataRpc(ulong id)
    {

        for (int i = 0; i < playerIdentities.Count; i++)
        {

            if (playerIdentities[i].id == id)
            {

                playerIdentities[i].ApplyPFP();

            }

        }

    }

    [ClientRpc]
    void SetupNewPlayerClientRpc(ulong[] nowPlayers, bool[] nowReady, ulong connectedId, ScoreManager.Mode gameMode)
    {

        clrUpdate = 0;

        if (IsHost) return;

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

            /*            foreach (ulong id in nowPlayers)
                        {

                            PlayerData playerData = new PlayerData();

                            playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
                            playerData.id = id;
                            playerData.square.id = id;
                            playerData.pfp = MyExtentions.CreateSpriteFromData(localSteamData.pfpData, localSteamData.imageWidth, localSteamData.imageHeight);
                            playerData.texture = playerData.pfp.texture;

                            if (id == NetworkManager.LocalClientId)
                            {

                                localSquare = playerData.square;
                                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

                            }

                            playerIdentities.Add(playerData);

                        }*/

            for (int i = 0; i < nowPlayers.Length; i++)
            {

                PlayerData playerData = new PlayerData();

                playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
                playerData.id = nowPlayers[i];
                playerData.square.id = nowPlayers[i];
                playerData.pfp = MyExtentions.CreateSpriteFromData(localSteamData.pfpData, localSteamData.imageWidth, localSteamData.imageHeight);
                playerData.texture = playerData.pfp.texture;

                if (nowPlayers[i] == NetworkManager.LocalClientId)
                {

                    localSquare = playerData.square;
                    FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

                }

                playerIdentities.Add(playerData);

            }

        }
        else
        {

            PlayerData playerData = new PlayerData();

            playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
            playerData.id = connectedId;
            playerData.square.id = connectedId;
            playerData.pfp = MyExtentions.CreateSpriteFromData(localSteamData.pfpData, localSteamData.imageWidth, localSteamData.imageHeight);
            playerData.texture = playerData.pfp.texture;
            playerData.square.ready = false;

            if (connectedId == NetworkManager.LocalClientId)
            {
                localSquare = playerData.square;
                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

            }

            playerIdentities.Add(playerData);

        }

    }

    private void FixedUpdate() => UpdatePlayerData();

    private void LateUpdate()
    {

        if (updatePFPStream.Count > 0)
        {
            updatePFPStream[0]();
            updatePFPStream.RemoveAt(0);
        }

    }

    float rbUpdate, nzlUpdate, hlthUpdate, scrUpdate, clrUpdate, clrUpdate2;


    void UpdatePlayerData()
    {

        float deltaTime = Time.deltaTime;

        if (stopUpdate) return;
        if (localSquare == null) return;
        if (playerIdentities == null) return;


        if(localSquare.isLocalPlayer) if (localSquare.playerController.quedInput)
            {
                localSquare.playerController.quedInput = false;
                UpdateRigidBody();
                rbUpdate = 0;
            }

        if (rbUpdate > 1)
        {
            UpdateRigidBody();
            rbUpdate -= 1;
        }

        if (nzlUpdate > 1)
        {
            UpdateNozzle();
            nzlUpdate -= 1;
        }

        if (clrUpdate > 1)
        {
            UpdateColor();
            clrUpdate = 0;
        }
        else if (clrUpdate2 < 8)
        {
            clrUpdate += deltaTime * 20;
            clrUpdate2 += deltaTime;
        }

        nzlUpdate += deltaTime * 100f;
        rbUpdate += deltaTime * 50f;

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

    void StorePlayerRigidBodyData(PlayerData player, byte[] data)
    {

        if ((byte) player.id != data[13]) return;


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
    void UpdateNozzle()
    {
        ulong sourceId = networkManager.LocalClientId;
        byte[] compPos = MyExtentions.EncodeNozzlePosition(localSquare.localNozzlePosition.x, localSquare.localNozzlePosition.y);
        byte[] data = new byte[3] { (byte) sourceId, compPos[0], compPos[1] };

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
        if ((byte) player.id != comp[0]) return;

        (float x, float y) = MyExtentions.DecodeNozzlePosition(new byte[2] { comp[1], comp[2] });

        player.square.localNozzlePosition = new Vector2(x, y);
    }

    public void UpdateColor()
    {
        ulong sourceId = networkManager.LocalClientId;
        byte[] data = new byte[4]
        {
            (byte) sourceId,
            (byte) Mathf.RoundToInt(localSquare.playerColor.r * 256),
            (byte) Mathf.RoundToInt(localSquare.playerColor.g * 256),
            (byte) Mathf.RoundToInt(localSquare.playerColor.b * 256)
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

        player.square.playerColor = new Color(data[1] / 256f, data[2] / 256f, data[3] / 256f);
        player.square.newColor = true;

    }

    public void UpdateHealth()
    {
        byte sourceId = (byte) networkManager.LocalClientId;
        float[] data = new float[]
        {
        localSquare.healthPoints
        };
        UpdateHealthRpc(sourceId, data);
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateHealthRpc(byte sourceId, float[] data)
    {
        if ((byte) networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreHealthData(player, sourceId, data);
        }
    }

    void StoreHealthData(PlayerData player, byte sourceId, float[] data)
    {
        if ((byte) player.id != sourceId) return;

        player.square.healthPoints = data[0];
    }

    public void UpdateScore()
    {
        byte sourceId = (byte) networkManager.LocalClientId;
        byte data = (byte) localSquare.score;

        UpdateScoreRpc(sourceId, data);

    }

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateScoreRpc(byte sourceId, byte data)
    {
        if ((byte) networkManager.LocalClientId == sourceId) return;
        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StoreScoreData(player, sourceId, data);
        }
    }

    void StoreScoreData(PlayerData player, byte sourceId, byte data)
    {

        if ((byte) player.id != sourceId) return;

        player.square.score = data;

    }

    public void UpdatePlayerReady(bool ready)
    {

        byte sourceId = (byte)networkManager.LocalClientId;
        byte data = (byte)localSquare.score;

        UpdatePlayerReadyRpc(sourceId, ready);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerReadyRpc(byte sourceId, bool ready)
    {

        if (playerIdentities == null) return;

        foreach (PlayerData player in playerIdentities)
        {
            StorePlayerReady(player, sourceId, ready);
        }

    }

    void StorePlayerReady(PlayerData player, byte sourceId, bool ready)
    {

        if ((byte)player.id != sourceId) return;

        player.square.ready = ready;

    }

    public void UpdateSelectedMap(int map)
    {

        if (!IsHost) return;
        UpdateSelectedMapRpc(map);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdateSelectedMapRpc(int map)
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

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerHealthServerRpc(ulong id, float damage, ulong responsibleId, Vector2 knockBack, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        UpdatePlayerHealthClientRpc(id, damage, responsibleId, knockBack, ignoreId);

        EndUpdatePlayerHealth(id, damage, responsibleId, knockBack);

    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    void UpdatePlayerHealthClientRpc(ulong id, float damage, ulong responsibleId, Vector2 knockBack, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        if (IsHost) return;

        EndUpdatePlayerHealth(id, damage, responsibleId, knockBack);

    }

    void EndUpdatePlayerHealth(ulong id, float damage, ulong responsibleId, Vector2 knockBack)
    {

        foreach (PlayerData player in playerIdentities)
        {

            if (player.id != id) continue;

            if (player.square.isDead) continue;

            player.square.healthPoints -= damage;

            player.square.rb.AddForce(knockBack, ForceMode2D.Impulse);

            if (player.square.healthPoints > 0) continue;

            player.square.KillPlayer();

            if (player.square.id == responsibleId) continue;
            if (scoreManager.gameMode != ScoreManager.Mode.DM) continue;
            foreach (PlayerData data in playerIdentities)
            {

                if (data.id != responsibleId) continue;

                data.square.score++;
                scoreManager.UpdateScoreBoard();

            }

        }

        UpdateHealth();
        UpdateScore();

    }

    public void PlayPlayerDeath(Vector3 particlePosition, Color particleColor)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        if (IsHost)
        {

            PlayPlayerDeathClientRpc(particlePosition, particleColor, ignoreId);

        }
        if (!IsHost)
        {

            PlayPlayerDeathServerRpc(particlePosition, particleColor, ignoreId);

        }

        PlayerDeathEffect(particlePosition, particleColor);

    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void PlayPlayerDeathServerRpc(Vector3 particlePosition, Color particleColor, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        PlayPlayerDeathClientRpc(particlePosition, particleColor, ignoreId);

        PlayerDeathEffect(particlePosition, particleColor);

    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    void PlayPlayerDeathClientRpc(Vector3 particlePosition, Color particleColor, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        PlayerDeathEffect(particlePosition, particleColor);

    }

    void PlayerDeathEffect(Vector3 particlePosition, Color particleColor)
    {

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
            Color pixelColor = new Color(rgb[0] / 256f, rgb[1] / 256f, rgb[2] / 256f, 1f);

            texture.SetPixel(x, y, pixelColor);
        }

        public void ApplyPFP()
        {
            texture.Apply();
        }

    }

    public struct IdPair
    {
        public ulong clientId;
        public SteamId steamId;
    }

}
