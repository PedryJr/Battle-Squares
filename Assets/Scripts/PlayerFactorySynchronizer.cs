using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; 
using static PlayerSynchronizer;

public class PlayerFactorySynchronizer : NetworkBehaviour
{

    public byte playerIDIncrementor;

    public float skinFetchesPerSecond = 1f;
    float skinFetchTimer = 0;

    ScoreManager scoreManager;
    PlayerSynchronizer playerSynchronizer;
    PlayerControllerManager playerControllerManager;
    public static PlayerFactorySynchronizer Instance;

    List<M_SkinInit> initEvents;
    List<M_SkinData> dataEvents;
    List<M_SkinFinished> finishedEvents;
    public Dictionary<byte, SkinDataBuffer> activeSkinDataBuffers;

    [SerializeField]
    private PlayerController controllerPrefab;

    [SerializeField]
    private PlayerController AIControllerPrefab;

    private void Awake()
    {
        playerControllerManager = GetComponent<PlayerControllerManager>();
        Instance = this;
        scoreManager = GetComponent<ScoreManager>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        activeSkinDataBuffers = new Dictionary<byte, SkinDataBuffer>();
        initEvents = new List<M_SkinInit>();
        dataEvents = new List<M_SkinData>();
        finishedEvents = new List<M_SkinFinished>();
    }

    [SerializeField]
    int AmountOfAIToTrain = 0;

    List<Action> deleteAIAgents = new List<Action>();

    public PlayerController SpawnAgent()
    {
        playerIDIncrementor++;

        PlayerFactoryDataPacket playerData = default;
        playerData.MMR = 1000;
        playerData.steamId = SteamClient.SteamId.Value;
        playerData.networkId = (byte)NetworkManager.LocalClientId;
        playerData.gameID = playerIDIncrementor;
        playerData.isAI = true;
        playerData.selectedMap = playerSynchronizer.localSquare.selectedMap;

        PlayerBehaviour newPlayer = Instantiate(playerSynchronizer.square);
        newPlayer.rb.simulated = false;
        newPlayer.transform.position = Vector3.zero;
        newPlayer.rb.position = Vector2.zero;
        newPlayer.rb.simulated = true;

        SetPlayerInitialData(ref newPlayer, ref playerData);
        SetPlayerLocality(ref newPlayer, ref playerData);
        SetPlayerSyncData(ref newPlayer, ref playerData);
        //SpawnPlayer(ref newPlayer);

        if (!IsHost && newPlayer.GetNetworkID() == NetworkManager.LocalClientId)
        {
            if (MapStreamSynchronizer.Instance) MapStreamSynchronizer.Instance.RestreamMapByForce();
        }

        playerSynchronizer.UpdateColor();
        playerSynchronizer.UpdateRigidBody(playerData.gameID);
        playerSynchronizer.UpdateHealth();
        playerSynchronizer.UpdatePlayerReady(playerSynchronizer.localSquare.ready);

        if (IsHost) scoreManager.UpdateModeAsHost(scoreManager.gameMode);

        playerSynchronizer.playerIdentities.Sort((a, b) => a.square.GetGameID().CompareTo(b.square.GetGameID()));

        ManagedValue<byte> managedValue = new ManagedValue<byte>();
        managedValue.Value = playerData.gameID;

        deleteAIAgents.Add(
            () =>
            {
                for (int j = playerSynchronizer.playerIdentities.Count - 1; j >= 0; j--)
                {
                    PlayerData pData = playerSynchronizer.playerIdentities[j];
                    PlayerBehaviour player = pData.square;
                    if (player == null) playerSynchronizer.playerIdentities.RemoveAt(j);
                    if (player.GetGameID() == managedValue.Value)
                    {
                        playerSynchronizer.playerIdentities.RemoveAt(j);
                        Destroy(player.gameObject);
                    }
                }
            }
        );

        return newPlayer.playerController;
    }

    private void FixedUpdate()
    {
        SkinDownload();
    }

    void Update()
    {
        CheckLocalCoop();
    }

    void CheckLocalCoop()
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene") return;
        foreach (var item in Gamepad.all)
        {
            if (!playerControllerManager.IsDeviceValidForRegistrationEXTERN(item)) continue;
            if (item.startButton.wasPressedThisFrame) CreateNewPlayerFromControllerRpc(NetworkManager.LocalClientId);
        }
    }

    public void CreateNewPlayerFromFirstController()
    {
        CreateNewPlayerFromControllerRpc(NetworkManager.LocalClientId);
    }

    void SkinDownload()
    {
        skinFetchTimer += Time.deltaTime * skinFetchesPerSecond;
        if (initEvents.Count > 0) RunSkinInitEvents();
        else if (dataEvents.Count > 0) RunSkinDataDispatchEvents();
        else if (finishedEvents.Count > 0) RunSkinDataFinishedEvents();
    }

    void RunSkinInitEvents()
    {
        if (skinFetchTimer >= 1)
        {
            skinFetchTimer = 0;
            SendPlayerSkinMetadata(initEvents[0].requesterID, initEvents[0].skinOwnerId, initEvents[0].squareGameID, initEvents[0].skinFrames, initEvents[0].skinFramerate);
            initEvents.RemoveAt(0);
        }
    }

    void RunSkinDataDispatchEvents()
    {
        if (skinFetchTimer >= 1)
        {
            skinFetchTimer = 0;
            SendPlayerSkinPartialData(dataEvents[0].requesterID, dataEvents[0].skinOwnerId, dataEvents[0].squareGameID, dataEvents[0].frameIndex, dataEvents[0].dataSegment);
            dataEvents.RemoveAt(0);
        }
    }

    void RunSkinDataFinishedEvents()
    {
        if (skinFetchTimer >= 1)
        {
            skinFetchTimer = 0;
            SendPlayerSkinFinishedData(finishedEvents[0].requesterID, finishedEvents[0].skinOwnerId, finishedEvents[0].squareGameID);
            finishedEvents.RemoveAt(0);
        }
    }

    bool FetchSkinValidity()
    {
        bool skinValidCheck = true;
        foreach (var frame in playerSynchronizer.skinData.skinFrames) skinValidCheck = frame.valid && skinValidCheck;
        return skinValidCheck;
    }
    int FetchFrameCount() => FetchSkinValidity() ? playerSynchronizer.skinData.frames : 1;
    float FetchFrameAnimation() => FetchSkinValidity() ? playerSynchronizer.skinData.frameRate : 0F;
    byte[] FetchFramePixels() => FetchSkinValidity() ? GetCustomSkin() : MyExtentions.BoolArrayToByteArray(playerSynchronizer.defaultSkin);
    byte[] GetCustomSkin()
    {
        byte[] frameBuffer;
        List<byte> collectedSkinData = new List<byte>();
        foreach (SkinData.SkinFrame frame in playerSynchronizer.skinData.skinFrames)
        {
            frameBuffer = MyExtentions.BoolArrayToByteArray(frame.frame);
            collectedSkinData.AddRange(frameBuffer);
        }
        return collectedSkinData.ToArray();
    }

    bool IsNewPlayer(byte playerId)
    {
        bool playerExists = false;
        if (playerSynchronizer.playerIdentities == null) playerSynchronizer.playerIdentities = new List<PlayerData>();
        foreach (PlayerData player in playerSynchronizer.playerIdentities)
        {
            if ((byte)player.square.GetGameID() == playerId)
            {
                playerExists = true;
                break;
            }
        }
        return !playerExists;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CreateNewPlayerFromControllerRpc(ulong networkID)
    {
        CreateNewPlayer(networkID);
    }

    [ContextMenu("Create AI")]
    public void CreateAI()
    {
        byte networkID = (byte)NetworkManager.LocalClientId;
        byte gameID = playerIDIncrementor;
        playerIDIncrementor++;

        if (!IsHost) return;
        GameStateDataPacket currentGameState = new GameStateDataPacket();

        currentGameState.newGameID = gameID;
        currentGameState.newNetworkID = networkID;
        currentGameState.currentGameMode = scoreManager.gameMode;
        currentGameState.mods = (float[])Mods.at.Clone();
        currentGameState.isAI = true;

        RoundTripCollectorClientRpc(currentGameState);
    }

    public void CreateNewPlayer(ulong id)
    {
        byte networkID = (byte)id;
        byte gameID = playerIDIncrementor;
        playerIDIncrementor++;

        if (!IsHost) return;
        GameStateDataPacket currentGameState = new GameStateDataPacket();

        currentGameState.newGameID = gameID;
        currentGameState.newNetworkID = networkID;
        currentGameState.currentGameMode = scoreManager.gameMode;
        currentGameState.mods = (float[])Mods.at.Clone();
        currentGameState.isAI = false;

        RoundTripCollectorClientRpc(currentGameState);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void RoundTripCollectorClientRpc(GameStateDataPacket currentGameState)
    {
        RoundTripCollector(ref currentGameState);
    }

    void RoundTripCollector(ref GameStateDataPacket currentGameState)
    {

        scoreManager.gameMode = currentGameState.currentGameMode;
        for (int i = 0; i < currentGameState.mods.Length; i++) Mods.at[i] = currentGameState.mods[i];

        //Ensure the player that triggered the "create player" function is created.
        if(currentGameState.newNetworkID == NetworkManager.LocalClientId)
        {
            PlayerFactory(currentGameState.selectedMap, currentGameState.newNetworkID, currentGameState.newGameID, SteamClient.SteamId, currentGameState.isAI);
        }

        //Ensure all local players are created on clients that might have them missing.
        List<PlayerData> playerIdentities = playerSynchronizer.playerIdentities;
        if(playerIdentities != null)
        {
            for(int i = 0; i < playerIdentities.Count; i++)
            {
                PlayerBehaviour player = playerIdentities[i].square;
                if (!player.isLocalPlayer) continue;
                if (player.isLocalPlayer) PlayerFactory(currentGameState.selectedMap, player.GetNetworkID(), player.GetGameID(), player.SteamID, player.isAI);
            }
        }
    }

    //Generates a player on all clients, should that player not exist..
    void PlayerFactory(int selectedMap, byte networkID, byte gameID, ulong steamID, bool isAI)
    {

        PlayerFactoryDataPacket playerFactoryData = new PlayerFactoryDataPacket();
        playerFactoryData.selectedMap = selectedMap;
        playerFactoryData.steamId = steamID;
        playerFactoryData.MMR = new EncryptedDouble(PlayerBehaviour.MMRlocation, 1000.0).Value;
        playerFactoryData.networkId = networkID;
        playerFactoryData.gameID = gameID;
        playerFactoryData.isAI = isAI;

        //Dispatch player creation on all clients
        PlayerFactoryRpc(playerFactoryData);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void PlayerFactoryRpc(PlayerFactoryDataPacket playerData)
    {
        Debug.Log("Player Factory RPC\n" +
            $"Source ID: {playerData.networkId}\n" +
            $"Source SteamID: {playerData.steamId}\n");

        if (IsNewPlayer(playerData.gameID)) InstantiateNewPlayer(ref playerData);

        playerSynchronizer.UpdateColor();
        playerSynchronizer.UpdateRigidBody(playerData.gameID);
        playerSynchronizer.UpdateHealth();
        playerSynchronizer.UpdatePlayerReady(playerSynchronizer.localSquare.ready);

        if (IsHost) scoreManager.UpdateModeAsHost(scoreManager.gameMode);

        playerSynchronizer.playerIdentities.Sort((a, b) => a.square.GetGameID().CompareTo(b.square.GetGameID()));

    }

    public void InstantiateNewPlayer(ref PlayerFactoryDataPacket playerData)
    {
        PlayerBehaviour newPlayer = Instantiate(playerSynchronizer.square);

        SetPlayerInitialData(ref newPlayer, ref playerData);
        SetPlayerLocality(ref newPlayer, ref playerData);
        SetPlayerSyncData(ref newPlayer, ref playerData);
        SpawnPlayer(ref newPlayer);

        if (!IsHost && newPlayer.GetNetworkID() == NetworkManager.LocalClientId)
        {
            if (MapStreamSynchronizer.Instance) MapStreamSynchronizer.Instance.RestreamMapByForce();
        }
    }

    private void SpawnPlayer(ref PlayerBehaviour newPlayer)
    {
        Debug.Log("Spawning a player on local client!");
        newPlayer.SpawnEffect();
        if (IsHost)
        {
            playerSynchronizer.UpdateSelectedMap(playerSynchronizer.localSquare.selectedMap, playerSynchronizer.localSquare.selectedLegacyMap);
        }
        RequestPlayerSkinServerRpc((byte)NetworkManager.LocalClientId, newPlayer.GetNetworkID(), newPlayer.GetGameID());
        playerSynchronizer.clrUpdate = 1;
        newPlayer.newColor = true;
    }

    private void SetPlayerLocality(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        if (playerData.networkId == (byte) NetworkManager.LocalClientId)
        {
            newPlayer.isLocalPlayer = true;
            if (!playerSynchronizer.localSquare) playerSynchronizer.localSquare = newPlayer;
            PlayerController contrl = Instantiate<PlayerController>(playerData.isAI ? AIControllerPrefab : controllerPrefab);
            contrl.SetTargetController(newPlayer);
            if(!playerData.isAI) playerControllerManager.SpawnController(contrl);
        }
    }

    private void SetPlayerSyncData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        playerSynchronizer.playerIdentities.Add(new PlayerData
        {
            square = newPlayer,
            steamId = playerData.steamId
        });
        newPlayer.AssertSteamDataAvalible(playerData.steamId);
        newPlayer.MMR = playerData.MMR;
    }

    private void SetPlayerInitialData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        newPlayer.isAI = playerData.isAI;
        newPlayer.SetGameID(playerData.gameID);
        newPlayer.SetNetworkID(playerData.networkId);
        newPlayer.selectedMap = playerData.selectedMap;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void RequestPlayerSkinServerRpc(byte requesterID, byte skinOwnerID, byte gameID)
    {
        // Server forwards the request to the specific skin owner client.
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { skinOwnerID };
        RequestPlayerSkinClientRpc(requesterID, skinOwnerID, gameID, clientRpcParams);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void RequestPlayerSkinClientRpc(byte requesterID, byte skinOwnerID, byte gameID, ClientRpcParams clientRpcParams = default)
    {

        string output = $"" +
            $"Skin request from id {requesterID} recieved." +
            $"Attempting to send skin belonging to id {skinOwnerID} - to the requester id {requesterID}";
        Debug.Log(output);

        byte[] rawDataBuffer = FetchFramePixels();
        int skinFrameCount = FetchFrameCount();
        float skinAnimationSpeed = FetchFrameAnimation();

        skinFetchTimer = -0.5f * skinFetchesPerSecond;

        M_SkinInit skinInit = new M_SkinInit();
        skinInit.squareGameID = gameID;
        skinInit.requesterID = requesterID;
        skinInit.skinOwnerId = skinOwnerID;
        skinInit.skinFrames = skinFrameCount;
        skinInit.skinFramerate = skinAnimationSpeed;

        initEvents.Add(skinInit);

        for (int i = 0; i < skinFrameCount; i++)
        {
            byte[] dataSegment = new byte[15];
            for (int j = 0; j < dataSegment.Length; j++) dataSegment[j] = rawDataBuffer[(i * 15) + j];
            M_SkinData skinData = new M_SkinData();
            skinData.squareGameID = gameID;
            skinData.requesterID = requesterID;
            skinData.skinOwnerId = skinOwnerID;
            skinData.frameIndex = i;
            skinData.dataSegment = dataSegment;
            dataEvents.Add(skinData);
        }

        M_SkinFinished skinFinished = new M_SkinFinished();
        skinFinished.squareGameID = gameID;
        skinFinished.requesterID = requesterID;
        skinFinished.skinOwnerId = skinOwnerID;
        finishedEvents.Add(skinFinished);
    }

    public struct M_SkinInit
    {
        public byte requesterID;
        public byte skinOwnerId;
        public byte squareGameID;
        public int skinFrames;
        public float skinFramerate;
    }

    public struct M_SkinData
    {
        public byte requesterID;
        public byte skinOwnerId;
        public byte squareGameID;
        public byte[] dataSegment;
        public int frameIndex;
    }

    public struct M_SkinFinished
    {
        public byte requesterID;
        public byte skinOwnerId;
        public byte squareGameID;
    }

    public void SendPlayerSkinMetadata(ulong requesterID, byte skinOwnerID, byte gameID, int skinFrames, float animationSpeed)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinMetadataClientRpc(requesterID, skinOwnerID, gameID, skinFrames, animationSpeed, clientRpcParams);
        }
        else SendPlayerSkinMetadataServerRpc(requesterID, skinOwnerID, gameID, skinFrames, animationSpeed);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinMetadataServerRpc(ulong requesterID, byte skinOwnerID, byte gameID, int skinFrames, float animationSpeed)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinMetadataClientRpc(requesterID, skinOwnerID, gameID, skinFrames, animationSpeed, clientRpcParams);
    }




    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinMetadataClientRpc(ulong requesterID, byte skinOwnerID, byte gameID, int skinFrames, float animationSpeed, ClientRpcParams clientRpcParams = default)
    {
        SkinDataBuffer skinDataBuffer = new SkinDataBuffer(playerSynchronizer.GetPlayerById(gameID), skinFrames, animationSpeed);
        activeSkinDataBuffers[gameID] = skinDataBuffer;


        string output = $"" +
            $"Skin request processed, recieving metadata from id: {skinOwnerID}." +
            $"Skin request processed, recieving metadata from player named: {playerSynchronizer.GetPlayerById(gameID).name}.";
        Debug.Log(output);
    }



    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinPartialDataClientRpc(byte requesterID, byte skinOwnerID, byte gameID, int frameIndex, byte[] frameData, ClientRpcParams clientRpcParams = default)
    {
        activeSkinDataBuffers[gameID].AssignPartialBuffer(frameIndex, frameData);

        string output = $"" +
            $"Skin request processed, recieving partialData from id: {skinOwnerID}." +
            $"Skin request processed, recieving partialData from player named: {playerSynchronizer.GetPlayerById(skinOwnerID).name}.";
        Debug.Log(output);
    }



    // PSEUDOCODE / PLAN (detailed):
    // - On receiving the "skin finished" client RPC:
    //   1. Resolve the PlayerBehaviour that corresponds to the `gameID` (this is the id used as key in activeSkinDataBuffers).
    //      - If no player found, log a warning and exit early.
    //   2. Try to look up a SkinDataBuffer in `activeSkinDataBuffers` using `gameID`.
    //      - If missing, log a warning and exit early (avoid KeyNotFoundException).
    //   3. If buffer exists, call `AssignBufferDataToPlayerSkin()` inside a try/catch to avoid crashing when data is malformed or incomplete.
    //      - Log any exception details for debugging.
    //   4. Remove the buffer entry from `activeSkinDataBuffers` after applying it.
    //   5. Use `gameID` (not `skinOwnerID`) when resolving the player for safer and consistent lookup.
    //   6. Keep debug messages informative but defensive (check for nulls).
    //
    // This replaces the previous implementation to prevent crashes when the buffer or player is not present
    // (which could happen when re-creating lobbies or when requests are out-of-order).

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinFinishedDataClientRpc(byte requesterID, byte skinOwnerID, byte gameID, ClientRpcParams clientRpcParams = default)
    {
        // Resolve the player by the gameID (this is the key used to store buffers).
        PlayerBehaviour targetPlayer = null;
        try
        {
            targetPlayer = playerSynchronizer.GetPlayerById(gameID);
        }
        catch
        {
            // Defensive: if GetPlayerById throws, ensure we still handle gracefully.
            targetPlayer = null;
        }

        if (targetPlayer == null)
        {
            Debug.Log($"SendPlayerSkinFinishedDataClientRpc: no player found for gameID {gameID}. OwnerNetworkID={skinOwnerID}, Requester={requesterID}");
            // Clean up any stale entry if present
            if (activeSkinDataBuffers.ContainsKey(gameID))
            {
                activeSkinDataBuffers.Remove(gameID);
            }
            return;
        }

        Debug.Log($"Skin request processed, receiving finished notify from id: {skinOwnerID}. Player: {targetPlayer.name}.");

        if (!activeSkinDataBuffers.TryGetValue(gameID, out var buffer))
        {
            Debug.Log($"SendPlayerSkinFinishedDataClientRpc: no skin data buffer found for gameID {gameID}. OwnerNetworkID={skinOwnerID}, Requester={requesterID}");
            return;
        }

        try
        {
            buffer.AssignBufferDataToPlayerSkin();
        }
        catch (System.Exception ex)
        {
            Debug.Log($"SendPlayerSkinFinishedDataClientRpc: error applying skin for gameID {gameID}: {ex}");
        }

        activeSkinDataBuffers.Remove(gameID);
    }



    public void SendPlayerSkinPartialData(byte requesterID, byte skinOwnerID, byte gameID, int frameIndex, byte[] frameData)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinPartialDataClientRpc(requesterID, skinOwnerID, gameID, frameIndex, frameData, clientRpcParams);
        }
        else SendPlayerSkinPartialDataServerRpc(requesterID, skinOwnerID, gameID, frameIndex, frameData);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinPartialDataServerRpc(byte requesterID, byte skinOwnerID, byte gameID, int frameIndex, byte[] frameData)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinPartialDataClientRpc(requesterID, skinOwnerID, gameID, frameIndex, frameData, clientRpcParams);
    }

    public void SendPlayerSkinFinishedData(byte requesterID, byte skinOwnerID, byte gameID)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinFinishedDataClientRpc(requesterID, skinOwnerID, gameID, clientRpcParams);
        }
        else SendPlayerSkinFinishedDataServerRpc(requesterID, skinOwnerID, gameID);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SendPlayerSkinFinishedDataServerRpc(byte requesterID, byte skinOwnerID, byte gameID)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinFinishedDataClientRpc(requesterID, skinOwnerID, gameID, clientRpcParams);
    }

    public struct SkinDataPacket : INetworkSerializable
    {
        public int skinFrameCount;
        public float skinAnimationSpeed;
        public byte[] skinFrames;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref skinFrameCount);
            serializer.SerializeValue(ref skinAnimationSpeed);
            serializer.SerializeValue(ref skinFrames);
        }
    }

    public struct PlayerFactoryDataPacket : INetworkSerializable
    {

        public ulong steamId;
        public byte networkId;
        public byte gameID;
        public double MMR;
        public int selectedMap;
        public bool isAI;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref steamId);
            serializer.SerializeValue(ref networkId);
            serializer.SerializeValue(ref gameID);
            serializer.SerializeValue(ref MMR);
            serializer.SerializeValue(ref selectedMap);
            serializer.SerializeValue(ref isAI);
        }
    }

    public struct GameStateDataPacket : INetworkSerializable
    {
        public byte newGameID;
        public byte newNetworkID;
        public int selectedMap;
        public float[] mods;
        public ScoreManager.Mode currentGameMode;
        internal bool isAI;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref newGameID);
            serializer.SerializeValue(ref newNetworkID);
            serializer.SerializeValue(ref selectedMap);
            serializer.SerializeValue(ref mods);
            serializer.SerializeValue(ref currentGameMode);
            serializer.SerializeValue(ref isAI);
        }
    }

    public class SkinDataBuffer
    {
        PlayerBehaviour skinHolder;
        Dictionary<int, byte[]> bufferRegister;
        int frameCount;
        float frameRate;

        public SkinDataBuffer(PlayerBehaviour skinHolder, int frameCount, float frameRate)
        {
            bufferRegister = new Dictionary<int, byte[]>();
            this.skinHolder = skinHolder;
            this.frameCount = frameCount;
            this.frameRate = frameRate;
        }

        public void AssignPartialBuffer(int frameNumber, byte[] frameData) => bufferRegister[frameNumber] = frameData;

        public void AssignBufferDataToPlayerSkin()
        {
            byte[] rawBuffer = GetWholeBuffer();

            skinHolder.nozzleFrames = new Sprite[frameCount];
            skinHolder.bodyFrames = new Sprite[frameCount];
            skinHolder.frameRate = frameRate;

            byte[] frameBuffer = new byte[15];
            bool[] skinBuffer;
            int frameBufferIndex = 0;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {

                for (int i = 0; i < 15; i++, frameBufferIndex++) frameBuffer[i] = rawBuffer[frameBufferIndex];

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

                skinHolder.CreateTextureFromBoolArray10BY10(bodySkin, frameIndex);
                skinHolder.CreateTextureFromBoolArray4BY4(nozzleSkin, frameIndex);
            }
        }

        byte[] GetWholeBuffer()
        {
            int totalLength = 0;
            for (int i = 0; i < frameCount; i++)
            {
                if (!bufferRegister.TryGetValue(i, out byte[] part) || part == null)
                {
                    throw new System.InvalidOperationException($"SkinDataBuffer: missing frame data for frame index {i}.");
                }

                totalLength += part.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            for (int i = 0; i < frameCount; i++)
            {
                byte[] part = bufferRegister[i];
                if (part.Length > 0)
                {
                    System.Buffer.BlockCopy(part, 0, result, offset, part.Length);
                    offset += part.Length;
                }
            }

            return result;
        }
    }
}