using Netcode.Transports.Facepunch;
using Steamworks;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerSynchronizer : NetworkBehaviour
{

    public List<PlayerData> playerIdentities;
    public List<IdPair> idPairs;
    NetworkManager networkManager;

    [SerializeField]
    GameObject square;

    public PlayerBehaviour localSquare;

    ProjectileManager projectileManager;

    ScoreManager scoreManager;

    float serverUpdateTimer;

    [SerializeField]
    GameObject deathParticles;

    public bool hostShutdown = false;

    bool stopUpdate;
    private void Awake()
    {

        DontDestroyOnLoad(this);
        networkManager = GameObject.Find("Network").GetComponent<NetworkManager>();
        projectileManager = GetComponent<ProjectileManager>();
        networkManager.OnClientConnectedCallback += SetupNewPlayer;
        networkManager.OnClientDisconnectCallback += DisconnectPlayer;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

        QualitySettings.vSyncCount = 0;

        scoreManager = GetComponent<ScoreManager>();

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

    public void SetupNewPlayer(ulong id)
    {
        clrUpdate = 0;
        if (IsHost)
        {

            PlayerData playerData = new PlayerData();
            playerData.id = id;
            playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
            playerData.square.id = id;

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
            if (playerData.id == NetworkManager.LocalClientId)
            {


                localSquare = playerData.square;
                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

            }
            foreach (PlayerData player in playerIdentities)
            {

                nowPlayers.Add(player.id);

            }

            SetupNewPlayerClientRpc(nowPlayers.ToArray(), id, scoreManager.gameMode);

            Invoke("SetupPlayerIdsStep1", 0.1f);

        }

    }

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
                    if (pair.clientId == playerIdentities[i].id)
                    {
                        PlayerData newData = playerIdentities[i];

                        newData.pfp = MyExtentions.GetImageData(pair.steamId).Result;
                        foreach (Friend friend in SteamNetwork.currentLobby.Value.Members)
                        {
                            if (friend.Id.Value == pair.steamId.Value) newData.name = friend.Name;
                        }

                        playerIdentities[i] = newData;

                    }
                }

            }

        }

    }

    [ClientRpc]
    void SetupNewPlayerClientRpc(ulong[] nowPlayers, ulong connectedId, ScoreManager.Mode gameMode)
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

            foreach (ulong id in nowPlayers)
            {

                PlayerData playerData = new PlayerData();

                playerData.square = Instantiate(square).GetComponent<PlayerBehaviour>();
                playerData.id = id;
                playerData.square.id = id;

                if (id == NetworkManager.LocalClientId)
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

            if (connectedId == NetworkManager.LocalClientId)
            {
                localSquare = playerData.square;
                FindAnyObjectByType<PlayerController>().SetTargetController(localSquare);

            }

            playerIdentities.Add(playerData);

        }

    }

    private void FixedUpdate() => UpdatePlayerData();

    float rbUpdate, nzlUpdate, hlthUpdate, scrUpdate, clrUpdate, clrUpdate2;

    void UpdatePlayerData()
    {

        if (stopUpdate) return;
        if (localSquare == null) return;
        if (playerIdentities == null) return;

        rbUpdate += Time.deltaTime * 100f;
        nzlUpdate += Time.deltaTime * 100f;
        clrUpdate += Time.deltaTime;
        clrUpdate2 += Time.deltaTime * 20f;
        /*hlthUpdate += Time.deltaTime * 5;*/
        /*scrUpdate += Time.deltaTime * 3;*/

        if (rbUpdate > 1)
        {
            UpdateRigidBody();
            rbUpdate = 0;
        }

        if (nzlUpdate > 1)
        {
            UpdateNozzle();
            nzlUpdate = 0;
        }

        if (scrUpdate > 1)
        {
            UpdateScore();
            scrUpdate = 0;
        }

        if (clrUpdate < 8 && clrUpdate2 > 1)
        {
            UpdateColor();
            clrUpdate2 = 0;
        }

    }

    void UpdateRigidBody()
    {
        ulong sourceId = networkManager.LocalClientId;
/*        float[] data = new float[]
            {

                localSquare.position.x, localSquare.position.y,
                localSquare.rotation,
                localSquare.velocity.x, localSquare.velocity.y,
                localSquare.angularVelocity

            };*/

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

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
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

    }

    void UpdateHealth()
    {
        byte sourceId = (byte) networkManager.LocalClientId;
        float[] data = new float[]
        {
        localSquare.healthPoints
        };
        UpdateHealthRpc(sourceId, data);
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
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

    void UpdateScore()
    {
        byte sourceId = (byte) networkManager.LocalClientId;
        byte data = (byte) localSquare.score;

        UpdateScoreRpc(sourceId, data);

    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
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

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    void UpdatePlayerHealthServerRpc(ulong id, float damage, ulong responsibleId, Vector2 knockBack, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        UpdatePlayerHealthClientRpc(id, damage, responsibleId, knockBack, ignoreId);

        EndUpdatePlayerHealth(id, damage, responsibleId, knockBack);

    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
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

    [ServerRpc(RequireOwnership = false)]
    void PlayPlayerDeathServerRpc(Vector3 particlePosition, Color particleColor, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        PlayPlayerDeathClientRpc(particlePosition, particleColor, ignoreId);

        PlayerDeathEffect(particlePosition, particleColor);

    }

    [ClientRpc]
    void PlayPlayerDeathClientRpc(Vector3 particlePosition, Color particleColor, ulong ignoreId)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        PlayerDeathEffect(particlePosition, particleColor);

    }

    void PlayerDeathEffect(Vector3 particlePosition, Color particleColor)
    {

        localSquare.deathSoundInstance.start();

        GameObject newParticle = Instantiate(deathParticles, particlePosition, Quaternion.Euler(0, 0, 0), null);
        Material particleMaterial = Instantiate(newParticle.GetComponent<ParticleSystemRenderer>().material);
        newParticle.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        newParticle.GetComponent<ParticleSystemRenderer>().material.color = particleColor;

    }

    public Color UpdatePlayerColor(float value)
    {

        float h, s, v;
        Color.RGBToHSV(localSquare.playerColor, out h, out s, out v);
        h = value;

        localSquare.playerColor = Color.HSVToRGB(h, s, v);

        UpdateColor();

        return localSquare.playerColor;

    }

    public struct PlayerData
    {

        public ulong id;
        public PlayerBehaviour square;
        public string name;
        public Sprite pfp;

    }

    public struct IdPair
    {
        public ulong clientId;
        public SteamId steamId;
    }

}
