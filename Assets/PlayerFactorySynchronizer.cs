using Steamworks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;
using static PlayerFactorySynchronizer;
using static PlayerSynchronizer;
using static UnityEngine.PlayerLoop.EarlyUpdate;

[Preserve]
public class PlayerFactorySynchronizer : NetworkBehaviour
{

    public float skinFetchesPerSecond = 1f;
    float skinFetchTimer = 0;

    ScoreManager scoreManager;
    PlayerSynchronizer playerSynchronizer;
    public static PlayerFactorySynchronizer Instance;

    public delegate void SkinSendEvent();
    public List<SkinSendEvent> skinSendEvents;
    public Dictionary<byte, SkinDataBuffer> activeSkinDataBuffers;

    private void Awake()
    {
        Instance = this;
        scoreManager = GetComponent<ScoreManager>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        skinSendEvents = new List<SkinSendEvent>();
        activeSkinDataBuffers = new Dictionary<byte, SkinDataBuffer>();
    }

    private void Update()
    {
        if(skinSendEvents.Count > 0) SkinFetching();
    }

    void SkinFetching()
    {
        skinFetchTimer += Time.deltaTime * skinFetchesPerSecond;
        if (skinFetchTimer >= 1)
        {
            skinFetchTimer = 0;
            skinSendEvents[0]();
            skinSendEvents.RemoveAt(0);
        }
    }

    public void CreateNewPlayer(ulong id)
    {

        if (!IsHost) return;
        GameStateDataPacket currentGameState = new GameStateDataPacket();

        currentGameState.currentGameMode = scoreManager.gameMode;
        currentGameState.mods = (float[])Mods.at.Clone();

        RoundTripCollectorClientRpc(currentGameState);
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

    bool IsNewPlayer(ulong playerId)
    {
        bool playerExists = false;
        if (playerSynchronizer.playerIdentities == null) playerSynchronizer.playerIdentities = new List<PlayerData>();
        foreach (PlayerData player in playerSynchronizer.playerIdentities)
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

        playerFactoryData.selectedMap = currentGameState.selectedMap;
        playerFactoryData.steamId = SteamClient.SteamId.Value;
        playerFactoryData.networkId = NetworkManager.LocalClientId;

        if(IsHost) PlayerFactoryClientRpc(playerFactoryData);
        else PlayerFactoryServerRpc(playerFactoryData);
    }

    [ServerRpc (RequireOwnership = false)]
    void PlayerFactoryServerRpc(PlayerFactoryDataPacket playerData)
    {
        PlayerFactoryServerRpc(playerData);
    }

    [ClientRpc]
    void PlayerFactoryClientRpc(PlayerFactoryDataPacket playerData)
    {
        PlayerFactory(ref playerData);
    }

    void PlayerFactory(ref PlayerFactoryDataPacket playerData)
    { 
        Debug.Log
        (
            "Player Factory RPC\n" +
            $"Source ID: {playerData.networkId}\n" +
            $"Source SteamID: {playerData.steamId}\n"
        );

        if (IsNewPlayer(playerData.networkId)) InstantiateNewPlayer(ref playerData);

        playerSynchronizer.UpdateColor();
        playerSynchronizer.UpdateNozzle();
        playerSynchronizer.UpdateRigidBody();
        playerSynchronizer.UpdateHealth();
        playerSynchronizer.UpdatePlayerReady(playerSynchronizer.localSquare.ready);

        if (IsHost) scoreManager.UpdateModeAsHost(scoreManager.gameMode); 
    }

    public void InstantiateNewPlayer(ref PlayerFactoryDataPacket playerData)
    {
        PlayerBehaviour newPlayer = Instantiate(playerSynchronizer.square);

        SetPlayerInitialData(ref newPlayer, ref playerData);

        SetPlayerLocality(ref newPlayer, ref playerData);

        SetPlayerSyncData(ref newPlayer, ref playerData);

        SpawnPlayer(ref newPlayer);

        if (!IsHost && newPlayer.GetID() == NetworkManager.LocalClientId)
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
            playerSynchronizer.playerIdList.Add(newPlayer.id);
            playerSynchronizer.UpdateSelectedMap(playerSynchronizer.localSquare.selectedMap, playerSynchronizer.localSquare.selectedLegacyMap);
        }
        playerSynchronizer.clrUpdate = 1;
        newPlayer.newColor = true;
        RequestPlayerSkinRpc(NetworkManager.LocalClientId, newPlayer.id);
    }

    private void SetPlayerLocality(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        if (playerData.networkId != NetworkManager.LocalClientId) return;

        playerSynchronizer.localSquare = newPlayer;
        FindAnyObjectByType<PlayerController>().SetTargetController(playerSynchronizer.localSquare);
    }

    private void SetPlayerSyncData(ref PlayerBehaviour newPlayer, ref PlayerFactoryDataPacket playerData)
    {
        playerSynchronizer.playerIdentities.Add(new PlayerData
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
        newPlayer.selectedMap = playerData.selectedMap;
    }


    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RequestPlayerSkinRpc(ulong requesterId, ulong skinOwnerId)
    {
/*        if (playerSynchronizer.localSquare.id != skinOwnerID) return;
        SkinDataPacket skinDataPacket = new SkinDataPacket
        {
            skinFrames = FetchFramePixels(),
            skinFrameCount = FetchFrameCount(),
            skinAnimationSpeed = FetchFrameAnimation(),
        };

        SendPlayerSkinData(requesterID, skinOwnerID, ref skinDataPacket);*/

        byte[] rawDataBuffer = FetchFramePixels();

        ManagedPrimitive<ulong> requesterID = new ManagedPrimitive<ulong>();
        ManagedPrimitive<ulong> skinOwnerID = new ManagedPrimitive<ulong>();
        ManagedPrimitive<int> skinFrameCount = new ManagedPrimitive<int>();
        ManagedPrimitive<float> skinAnimationSpeed = new ManagedPrimitive<float>();

        skinFrameCount.Value = FetchFrameCount();
        skinAnimationSpeed.Value = FetchFrameAnimation();
        requesterID.Value = requesterId;
        skinOwnerID.Value = skinOwnerId;

        //Small delay added to allow Network time to settle down upon player joining.
        skinFetchTimer = -0.5f;

        skinSendEvents.Add(() =>  { SendPlayerSkinMetadata((byte)requesterID.Value, (byte)skinOwnerID.Value, skinFrameCount.Value, skinAnimationSpeed.Value); });

        ManagedPrimitive<int> memoryIncrementor = new ManagedPrimitive<int>();

        for(int i = 0; i < skinFrameCount.Value; i++)
        {
            skinSendEvents.Add(() =>
            {
                byte[] dataSegment = new byte[15];
                int frameIndex = memoryIncrementor.Value;
                for (int j = 0; j < dataSegment.Length; j++) dataSegment[j] = rawDataBuffer[(frameIndex * 15) + j];
                SendPlayerSkinPartialData((byte)requesterID.Value, (byte)skinOwnerID.Value, frameIndex, dataSegment);
                memoryIncrementor.Value++;
            });
        }

        skinSendEvents.Add(() => { SendPlayerSkinFinishedData((byte)requesterID.Value, (byte)skinOwnerID.Value); });
    }

    public void SendPlayerSkinMetadata(byte requesterID, byte skinOwnerID, int skinFrames, float animationSpeed)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinMetadataClientRpc(requesterID, skinOwnerID, skinFrames, animationSpeed, clientRpcParams);
        }
        else SendPlayerSkinMetadataServerRpc(requesterID, skinOwnerID, skinFrames, animationSpeed);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerSkinMetadataServerRpc(byte requesterID, byte skinOwnerID, int skinFrames, float animationSpeed)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinMetadataClientRpc(requesterID, skinOwnerID, skinFrames, animationSpeed, clientRpcParams);
    }

    [ClientRpc]
    public void SendPlayerSkinMetadataClientRpc(byte requesterID, byte skinOwnerID, int skinFrames, float animationSpeed, ClientRpcParams clientRpcParams = default)
    {
        SkinDataBuffer skinDataBuffer = new SkinDataBuffer(playerSynchronizer.GetPlayerById(skinOwnerID), skinFrames, animationSpeed);
        activeSkinDataBuffers[skinOwnerID] = skinDataBuffer;
    }

    public void SendPlayerSkinPartialData(byte requesterID, byte skinOwnerID, int frameIndex, byte[] frameData)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinPartialDataClientRpc(requesterID, skinOwnerID, frameIndex, frameData, clientRpcParams);
        }
        else SendPlayerSkinPartialDataServerRpc(requesterID, skinOwnerID, frameIndex, frameData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerSkinPartialDataServerRpc(byte requesterID, byte skinOwnerID, int frameIndex, byte[] frameData)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinPartialDataClientRpc(requesterID, skinOwnerID, frameIndex, frameData, clientRpcParams);
    }

    [ClientRpc]
    public void SendPlayerSkinPartialDataClientRpc(byte requesterID, byte skinOwnerID, int frameIndex, byte[] frameData, ClientRpcParams clientRpcParams = default)
    {
        activeSkinDataBuffers[skinOwnerID].AssignPartialBuffer(frameIndex, frameData);
    }




    public void SendPlayerSkinFinishedData(byte requesterID, byte skinOwnerID)
    {
        if (IsHost)
        {
            ClientRpcParams clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
            SendPlayerSkinFinishedDataClientRpc(requesterID, skinOwnerID, clientRpcParams);
        }
        else SendPlayerSkinFinishedDataServerRpc(requesterID, skinOwnerID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerSkinFinishedDataServerRpc(byte requesterID, byte skinOwnerID)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { requesterID };
        SendPlayerSkinFinishedDataClientRpc(requesterID, skinOwnerID, clientRpcParams);
    }

    [ClientRpc]
    public void SendPlayerSkinFinishedDataClientRpc(byte requesterID, byte skinOwnerID, ClientRpcParams clientRpcParams = default)
    {
        activeSkinDataBuffers[skinOwnerID].AssignBufferDataToPlayerSkin();
        activeSkinDataBuffers.Remove(skinOwnerID);
    }

    [Preserve]
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

    [Preserve]
    public struct PlayerFactoryDataPacket : INetworkSerializable
    {

        public ulong steamId;
        public ulong networkId;

        public int selectedMap;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref steamId);
            serializer.SerializeValue(ref networkId);

            serializer.SerializeValue(ref selectedMap);
        }
    }

    [Preserve]
    public struct GameStateDataPacket : INetworkSerializable
    {
        public int selectedMap;
        public float[] mods;
        public ScoreManager.Mode currentGameMode;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref selectedMap);
            serializer.SerializeValue(ref mods);
            serializer.SerializeValue(ref currentGameMode);
        }
    }

    [Preserve]
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

                skinHolder.CreateTextureFromBoolArray10BY10(bodySkin, (byte)frameIndex);
                skinHolder.CreateTextureFromBoolArray4BY4(nozzleSkin, (byte)frameIndex);
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

    [Preserve]
    public class ManagedPrimitive<T>
    {
        private T _internal;
        public T Value
        {
            get { return _internal; }
            set { _internal = value; }
        }
    }
}