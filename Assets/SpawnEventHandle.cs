using System;
using UnityEngine;

public class SpawnEventHandle : MonoBehaviour
{
    ProjectileSpawnEvent spawnEvent;
    public void Initialize(ref ProjectileSpawnEvent spawnEvent)
    {
        this.spawnEvent = spawnEvent;
    }

    float timeDelayed = 0;

    private void Update()
    {
        timeDelayed += Time.deltaTime;
        spawnEvent.Poll(ref spawnEvent);
        if(timeDelayed > spawnEvent.eventDelay) RunEvent();
    }

    public void RunEvent()
    {
        ushort type = spawnEvent.toSpawn.typeID;
        Vector2 position = spawnEvent.spawnPosition;
        Vector2 direction = spawnEvent.spawnDirection;
        PlayerBehaviour player = spawnEvent.shootingPlayer;
        spawnEvent.manager.SpawnProjectileFromProxy(type, position, direction, player);
        Destroy(gameObject);
    }
}

[Serializable]
public struct ProjectileSpawnEvent
{

    public delegate Vector2 GetSetVec2Stream(Vector2 oldValue);
    GetSetVec2Stream setPositionStream;
    GetSetVec2Stream setDirectionStream;

    [HideInInspector] public PlayerBehaviour shootingPlayer;
    public void SetShootingPlayer(PlayerBehaviour shootingPlayer) => this.shootingPlayer = shootingPlayer;
    [HideInInspector] public ProjectileManager manager;
    public void SetManager(ProjectileManager manager) => this.manager = manager;
    [HideInInspector] public Vector2 spawnDirection;
    public void SetGetSpawnDirection(GetSetVec2Stream stream) => this.setDirectionStream = stream;
    [HideInInspector] public Vector2 spawnPosition;
    public void SetGetSpawnPosition(GetSetVec2Stream stream) => this.setPositionStream = stream;
    public void Ensure(ref ProjectileSpawnEvent self)
    {
        self.setPositionStream = (oldValue) => { return new Vector2(); };
        self.setDirectionStream = (oldValue) => { return new Vector2(); };
    }

    public void Poll(ref ProjectileSpawnEvent self)
    {
        self.spawnDirection = self.setDirectionStream(self.spawnDirection);
        self.spawnPosition = self.setPositionStream(self.spawnPosition);
        if (self.flipDirection) self.spawnDirection = -self.spawnDirection;
    }

    public EventDirection eventDirection;
    public bool flipDirection;

    public EventType eventType;
    public float eventDelay;

    public WeaponBuilder toSpawn;

    public enum EventDirection
    {
        ClosestGround,
        ClosestPlayer,
        Velocity,
    }

    public enum EventType
    {
        Death,
        Birth,
    }
}