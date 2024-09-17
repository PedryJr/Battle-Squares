using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class MapSynchronizer : NetworkBehaviour
{

    [SerializeField]
    public MapTypes[] mapTypes;

    public ObjectivesBehaviour objectives;
    PlayerSynchronizer playerSynchronizer;
    ScoreManager scoreManager;

    public float repeat1S = 1f; 
    public float repeat2S = 2f; 
    public float repeat5S = 5f; 
    public float repeat10S = 10f;
    public float repeat20S = 20f;
    public float repeat30S = 30f;

    public float pingPong1S = 1f;
    public float pingPong2S = 2f; 
    public float pingPong5S = 5f; 
    public float pingPong10S = 10f;
    public float pingPong20S = 20f;
    public float pingPong30S = 30f;

    private float baseTime;
    private float updateTime;

    private void Awake()
    {
        
        scoreManager = FindAnyObjectByType<ScoreManager>();
        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();

    }


    private void Update()
    {

        if (!scoreManager.inGame) return;

        baseTime += Time.deltaTime;

        repeat1S = Mathf.Repeat(baseTime / 1f, 1f);
        repeat2S = Mathf.Repeat(baseTime / 2f, 1f);
        repeat5S = Mathf.Repeat(baseTime / 5f, 1f);
        repeat10S = Mathf.Repeat(baseTime / 10f, 1f);
        repeat20S = Mathf.Repeat(baseTime / 20f, 1f);
        repeat30S = Mathf.Repeat(baseTime / 30f, 1f);

        pingPong1S = Mathf.PingPong(baseTime / 1f, 1f);
        pingPong2S = Mathf.PingPong(baseTime / 2f, 1f);
        pingPong5S = Mathf.PingPong(baseTime / 5f, 1f);
        pingPong10S = Mathf.PingPong(baseTime / 10f, 1f);
        pingPong20S = Mathf.PingPong(baseTime / 20f, 1f);
        pingPong30S = Mathf.PingPong(baseTime / 30f, 1f);

    }
    private void FixedUpdate()
    {

        updateTime += Time.deltaTime;

        if (IsHost && updateTime > 0.1f)
        {
            updateTime = 0;
            SyncWorldParams();
        }
    }

    #region Params

    private void SyncWorldParams()
    {
        SyncWorldParamsClientRpc(baseTime);
    }

    [ClientRpc]
    private void SyncWorldParamsClientRpc(float syncedBaseTime)
    {

        baseTime = syncedBaseTime;

    }
    #endregion Params


    public void FlagStateChange(FlagActivityState newActivityState, int oId, ulong pId, bool condition)
    {

        FlagStateChangeRpc(newActivityState, oId, pId, condition, NetworkManager.LocalClientId);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void FlagStateChangeRpc(FlagActivityState newActivityState, int oId, ulong pId, bool condition, ulong IgnoreId)
    {

        if (NetworkManager.LocalClientId == IgnoreId) return;

        foreach (FlagBehaviour flagBehaviour in objectives.flags)
        {

            if(flagBehaviour.id != oId) continue;

            switch (newActivityState)
            {
                case FlagActivityState.FollowTarget:
                    foreach(PlayerData player in playerSynchronizer.playerIdentities)
                    {
                        if(player.id != pId) continue;
                        flagBehaviour.SetToFollowTarget(player.square, true);
                    }
                    break;
                case FlagActivityState.ReturnToSpawn: flagBehaviour.SetToReturnToSpawnChain(condition, true); break;
            }

        }

    }

    public void FlagPositionUpdate(FlagBehaviour flag, int oId)
    {

        SyncData syncData = flag.FetchRigidBody();

        byte ang = (byte) Mathf.Round(Mathf.Repeat(syncData.ang, 360f) / 2);
        byte angVel = (byte) Mathf.Round( Mathf.Clamp(syncData.angVel, -127, 127) + 127);

        short posX = (short) Mathf.Round(syncData.posX * 100);
        short posY = (short) Mathf.Round(syncData.posY * 100);

        byte[] slowBData = new byte[4];
        short[] slowSData = new short[2];

        slowBData[0] = ang;
        slowBData[1] = angVel;
        slowSData[0] = posX;
        slowSData[1] = posY;

        byte[] fastBData = new byte[2];
        short[] fastSData = new short[4];

        fastBData[0] = ang;
        fastBData[1] = angVel;
        fastSData[0] = posX;
        fastSData[1] = posY;

        if (Mathf.Abs(syncData.velX) < 12f && Mathf.Abs(syncData.velY) < 12f)
        {

            byte bVelX = (byte) Mathf.RoundToInt((syncData.velX * 10) + 127);
            byte bVelY = (byte) Mathf.RoundToInt((syncData.velY * 10) + 127);

            slowBData[2] = bVelX;
            slowBData[3] = bVelY;

            FlagPositionUpdateSlowRpc(slowBData, slowSData, (byte) flag.id);

        }
        else
        {

            short sVelX = (short) Mathf.RoundToInt(syncData.velX * 100);
            short sVelY = (short) Mathf.RoundToInt(syncData.velY * 100);

            fastSData[2] = sVelX;
            fastSData[3] = sVelY;

            FlagPositionUpdateFastRpc(fastBData, fastSData, (byte) flag.id);

        }

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void FlagPositionUpdateSlowRpc(byte[] bData, short[] sData, byte oId)
    {

        SyncData syncData = new SyncData 
        {
            posX = sData[0] / 100f,
            posY = sData[1] / 100f,
            velX = (bData[2] - 127) / 10f,
            velY = (bData[3] - 127) / 10f,
            ang = bData[0] * 2,
            angVel = bData[1] - 127
        };

        foreach (FlagBehaviour flag in objectives.flags)
        {
            if (flag.collected) continue;
            if(flag.GetId() != oId) continue;
            flag.SyncRigidBody(syncData);
        }

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void FlagPositionUpdateFastRpc(byte[] bData, short[] sData, byte oId)
    {

        SyncData syncData = new SyncData
        {
            posX = sData[0] / 100f,
            posY = sData[1] / 100f,
            velX = sData[2] / 100f,
            velY = sData[3] / 100f,
            ang = bData[0] * 2,
            angVel = bData[1] - 127
        };

        foreach (FlagBehaviour flag in objectives.flags)
        {
            if (flag.collected) continue;
            if (flag.GetId() != oId) continue;
            flag.SyncRigidBody(syncData);
        }

    }

}

public enum ObjectiveType
{
    flag
}

public enum OneShotSyncType
{
    One
}
