using BattleSquaresSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BinaryVectors;
using static UnityEngine.Analytics.IAnalytic;
using static WeaponBuilder;
using Random = System.Random;


public sealed class ProjectileManager : NetworkBehaviour
{

    [SerializeField]
    public WeaponBuilder[] newWeapons;
    public Dictionary<ushort, WeaponBuilder> weapons;

    [SerializeField]
    GameObject nozzleParticles;

    [SerializeField]
    public List<ProjectileBehaviour> projectiles;

    public NozzleBehaviour localNozzle;

    public PlayerSynchronizer playerSynchronizer;
    RuntimePrefabTemplates runtimePrefabTemplates;

    float timer;

    private void Awake()
    {
        runtimePrefabTemplates = GetComponent<RuntimePrefabTemplates>();
        weapons = new Dictionary<ushort, WeaponBuilder>();
        projectiles = new List<ProjectileBehaviour>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        for (ushort i = 0; i < newWeapons.Length; i++) weapons[newWeapons[i].typeID] = newWeapons[i];
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) => projectiles.Clear();

    private void Update()
    {

        timer += Time.deltaTime;

        if (timer > 3)
        {
            timer = 0;
        }

    }

    public void ClearAllProjectilesFromOwner(ulong id)
    {
        List<ProjectileBehaviour> newProjectiles = new List<ProjectileBehaviour>();

        foreach (ProjectileBehaviour projectile in projectiles)
        {

            if(projectile != null)
            {
                if(projectile.ownerId == (byte) id) Destroy(projectile.gameObject);
                else newProjectiles.Add(projectile);
            }

        }

        projectiles = newProjectiles;
    }
    public void SpawnProjectileFromProxy(ushort typeID, Vector2 position, Vector2 direction, PlayerBehaviour shootingPlayer)
    {
        Weapon weapon = GetRawWeaponByTypeID(typeID);

        Vector2 correctedPositionToGround = position;
        Vector2 simulatedNozzlePosition = position + (direction * weapon.spawnOffsetPadding);

        RaycastHit2D groundHit = Physics2D.Linecast(position, simulatedNozzlePosition, ProjectileBehaviour.ENVIRONTMENT_MASK);
        if (groundHit.transform)
        {
            float padding = weapon.spawnOffsetPadding;
            float cutOff = Vector2.Distance(position, simulatedNozzlePosition) - groundHit.distance;
            cutOff += padding;
            correctedPositionToGround -= direction.normalized * cutOff;
        }
        position = correctedPositionToGround;

        if (weapon.burst > 0)
        {

            float directionAngle = Mathf.Atan2(direction.normalized.y, direction.normalized.x) * Mathf.Rad2Deg;
            float burstAngleStep = weapon.burstSpread / weapon.burst;
            float halfSpreadAngle = weapon.burstSpread / 2f;

            float startAngle = directionAngle - halfSpreadAngle;

            for (int i = 0; i <= weapon.burst; i++)
            {

                float angleAtI = startAngle + (burstAngleStep * i);
                if (angleAtI > 180f) angleAtI -= 360f;

                Vector2 newDirection = MyExtentions.DegreesToVector2(angleAtI);

                SpawnProjectileOnAllClients(typeID, position, newDirection, shootingPlayer, weapon, i == 0);
            }
        }
        else SpawnProjectileOnAllClients(typeID, position, direction, shootingPlayer, weapon, true);
    }

    public void SpawnProjectile(ushort typeID, Vector2 position, Vector2 direction, PlayerBehaviour shootingPlayer)
    {
        Weapon weapon = GetRawWeaponByTypeID(typeID);


        Vector2 correctedPositionToGround = position;
        RaycastHit2D groundHit = Physics2D.Linecast(shootingPlayer.transform.position, position, ProjectileBehaviour.ENVIRONTMENT_MASK);
        if (groundHit.transform)
        {
            float padding = weapon.spawnOffsetPadding;
            float cutOff = Vector2.Distance(shootingPlayer.transform.position, position) - groundHit.distance;
            cutOff += padding;
            correctedPositionToGround -= direction.normalized * cutOff;
        }
        position = correctedPositionToGround;

        if (weapon.burst > 0)
        {

            float directionAngle = Mathf.Atan2(direction.normalized.y, direction.normalized.x) * Mathf.Rad2Deg;
            float burstAngleStep = weapon.burstSpread / weapon.burst;
            float halfSpreadAngle = weapon.burstSpread / 2f;
            
            float startAngle = directionAngle - halfSpreadAngle;

            for (int i = 0; i <= weapon.burst; i++)
            {

                float angleAtI = startAngle + (burstAngleStep * i);
                if (angleAtI > 180f) angleAtI -= 360f;

                Vector2 newDirection = MyExtentions.DegreesToVector2(angleAtI);

                SpawnProjectileOnAllClients(typeID, position, newDirection, shootingPlayer, weapon, i == 0);
            }
        }
        else SpawnProjectileOnAllClients(typeID, position, direction, shootingPlayer, weapon, true);

    }

    void SpawnProjectileOnAllClients(ushort typeID, in Vector2 position, in Vector2 direction, PlayerBehaviour shootingPlayer, in Weapon weapon, bool playSound)
    {
        uint projectileId = GenerateProjectileId();
        float[] fluctuation = new float[2];
        for (int i = 0; i < fluctuation.Length; i++) fluctuation[i] = GenerateProjectileFluctuation(weapon);

        if (weapon.flipFlop) shootingPlayer.nozzleBehaviour.flipFlop = !shootingPlayer.nozzleBehaviour.flipFlop;

        Boolean8 bitBool = new Boolean8();
        bitBool.SetBool(0, shootingPlayer.nozzleBehaviour.flipFlop);
        bitBool.SetBool(1, playSound);
        byte bitBoolAsByte = bitBool.GetMask();

        SpawnProjectileRpc(shootingPlayer.GetGameID(), projectileId, typeID, position, direction, fluctuation, bitBoolAsByte);
        SpawnProjectileEvent(shootingPlayer.GetGameID(), projectileId, typeID, position, direction, fluctuation, bitBoolAsByte);
    }


    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void SpawnProjectileRpc(byte sourceId, uint projectileID, ushort typeID, Vector2 position, Vector2 direction, float[] fluctuation, byte bitBoolAsByte)
    {
        if (sourceId == (byte)NetworkManager.LocalClientId) return;
        SpawnProjectileEvent(sourceId, projectileID, typeID, position, direction, fluctuation, bitBoolAsByte);
    }

    void SpawnProjectileEvent(byte sourceId, uint projectileID, ushort typeID, Vector2 position, Vector2 direction, float[] fluctuation, byte bitBoolAsByte)
    {
        bool flipFlop, playSound;
        Boolean8 bitBool = new Boolean8();
        bitBool.SetMask(bitBoolAsByte);
        flipFlop = bitBool.GetBool(0);
        playSound = bitBool.GetBool(1);

        ProjectileBehaviour projectileBehaviour = null;
        PlayerBehaviour owningPlayer = null;

        Weapon weapon = GetRawWeaponByTypeID(typeID);
        Vector2 forceToAdd = new();

        float multiplier1, multiplier2;

        owningPlayer = playerSynchronizer.GetPlayerById(sourceId);

        projectileBehaviour = Instantiate(weapon.projectile, position, Quaternion.identity, null);
        projectileBehaviour.flipFlop = flipFlop;
        projectileBehaviour.playShootSound = playSound;

        ProjectileInitData data = new ProjectileInitData();
        
        WeaponToProjectileData(in weapon, ref data, projectileID, position, direction, fluctuation, owningPlayer);

        projectileBehaviour.ownerId = owningPlayer.GetGameID();
        projectileBehaviour.InitializeBullet(ref data);

        multiplier1 = weapon.recoil * Mods.Recoil;
        multiplier2 = MyExtentions.EaseOutQuad(math.clamp(1 - (playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 28), 0, 1));

        forceToAdd = -direction.normalized * multiplier1 * multiplier2;
        owningPlayer.rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        owningPlayer.AnimatePlayer();
        owningPlayer.PlayNozzleRecoilAnimation();
    }

    void WeaponToProjectileData(in Weapon weapon, ref ProjectileInitData data, uint projectileID, Vector2 position, Vector2 direction, float[] fluctuation, PlayerBehaviour owningPlayer)
    {
        data.projectileManager = this;
        data.owningPlayer = owningPlayer;
        data.IsLocalProjectile = owningPlayer.isLocalPlayer;
        data.id = projectileID;
        data.direction = direction;
        data.acceleration = weapon.projectileAcceleration;
        data.speed = weapon.projectileSpeed;
        data.position = position;
        data.projectileColor = owningPlayer.PlayerColor.ProjectileColor;
        data.projectileDarkerColor = owningPlayer.PlayerColor.ParticleColor;
        data.lifeTime = weapon.lifeTime;
        data.fluctuation = fluctuation;
        data.noGravity = weapon.noGravity;
        data.dieOnImpact = weapon.dieOnImpact;
        data.damageOnImpact = weapon.damageOnImpact;
        data.aoe = weapon.aoe;
        data.knockback = weapon.knockback;
        data.sticky = weapon.sticky;
        data.speedLimit = weapon.speedLimit;
        data.minSpeed = weapon.minSpeed;
        data.aoeDamage = weapon.aoeDamage;
        data.skipAoeOnTargetHit = weapon.skipAoeOnTargetHit;
        data.baseDamage = weapon.baseDamage;
        data.damageTimeScale = weapon.damageTimeScale;
        data.enableMorph = weapon.enableMorph;
        data.targetMorph = weapon.targetMorph;
        data.timeToMorph = weapon.timeToMorph;
        data.sync = weapon.sync;
        data.stickToSender = weapon.stickToSender;
        data.morhpAnimation = weapon.morphAnimation;
        data.melee = weapon.melee;
        data.meleeRange = weapon.meleeRange;
        data.swingDegrees = weapon.swingDegrees;
        data.meleePosAnimation = weapon.meleePosAnimation;
        data.oneTimeHit = weapon.oneTimeHit;
        data.meleeRotAnimation = weapon.meleeRotAnimation;
        data.meleeRotation = weapon.meleeRotation;
        data.homing = weapon.homing;
        data.spinSpeed = weapon.spinSpeed;
        data.homingStrength = weapon.homingStrength;
        data.homingDistance = weapon.homingDistance;
        data.syncSpeed = weapon.syncSpeed;
        data.rotationFlipOnImpact = weapon.rotationFlipOnImpact;
        data.dieFromProjectiles = weapon.dieFromProjectiles;
        data.dontBlockProjectiles = weapon.dontBlockProjectiles;
        data.bounceOfPlayers = weapon.bounceOfPlayers;
        data.slowDownAmount = weapon.slowDownAmount;
        data.senderSpeedOnDeath = weapon.senderSpeedOnDeath;
        data.lingeringDamage = weapon.lingeringDamage;
        data.lingeringFrequency = weapon.lingeringFrequency;
        data.alignDirection = weapon.alignDirection;
        data.bounces = weapon.bounces;
        data.bounceParticle = weapon.bounceParticle;
        data.impactParticle = weapon.impactParticle;
        data.clampMorph = weapon.clampMorph;
        data.bounceSpeedLoss = weapon.bounceSpeedLoss;
        data.bounceAngleTilt = weapon.bounceAngleTilt;
        data.hover = weapon.hover;
        data.hoverDistance = weapon.hoverDistance;
        data.hoverDistanceAttenuation = weapon.hoverDistanceAttenuation;
        data.hoverFloorRadius = weapon.hoverFloorRadius;
        data.hoverStrength = weapon.hoverStrength;
        data.timeForFullHoverEffect = weapon.timeForFullHoverEffect;
        data.projectileSpawnEvents = weapon.projectileSpawnEvents;
        data.setMorphOnBounce = weapon.setMorphOnBounce;
        data.morphTimeOnBounce = weapon.morphTimeOnBounce;
        data.typeID = weapon.typeID;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint GenerateProjectileId() => (uint)new Random().Next(0, 2147483640) + (uint)new System.Random().Next(0, 2147483640);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float GenerateProjectileFluctuation(in Weapon weapon) => UnityEngine.Random.Range(-weapon.fluctuation, weapon.fluctuation);

    #region otherSyncs
    public uint GenerateRandomUInt()
    {
        byte[] buffer = new byte[4];
        new Random().NextBytes(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

    ParticleBehaviour GetNozzleParticle(ushort projectileType)
    {
        ParticleBehaviour particleBehaviour = null;
        particleBehaviour = GetWeaponBuilderByTypeID(projectileType).GetLaunchParticle;
        if(particleBehaviour) return particleBehaviour;
        return null;
    }

    ParticleBehaviour GetBounceParticle(ushort projectileType)
    {
        ParticleBehaviour particleBehaviour = null;
        particleBehaviour = GetWeaponBuilderByTypeID(projectileType).GetBounceParticle;
        if (particleBehaviour) return particleBehaviour;
        return null;
    }

    public void SpawnNozzleParticles(in Vector3 particlePosition, in Quaternion particleRotation, in ushort projectileType, byte ownerID)
    {

        SByte3 sByte3 = GetParticleCompressor;

        Vector3 rawData = new Vector3(particlePosition.x, particlePosition.y, Mathf.Repeat(particleRotation.eulerAngles.z, 360f));
        sByte3.SetFromVec3(rawData);

        Byte3 transformData = sByte3.GetByte3();

        SpawnNozzleParticlesRpc(transformData, ownerID, projectileType);
        SpawnNozzleParticlesEvent(transformData, ownerID, projectileType);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone)]
    void SpawnNozzleParticlesRpc(Byte3 transformData, byte ownerId, ushort projectileType)
    {
        SpawnNozzleParticlesEvent(transformData, ownerId, projectileType);
    }

    void SpawnNozzleParticlesEvent(in Byte3 transformData, in byte ownerId, in ushort projectileType)
    {

        SByte3 sByte3 = GetParticleCompressor;
        sByte3.SetFromByte3(transformData);

        Vector3 rawData = sByte3.GetVec3();
        Vector3 particlePosition = new Vector3(rawData.x, rawData.y, 0);
        Quaternion particleRotation = Quaternion.Euler(0, 0, rawData.z);

        ParticleBehaviour newParticle = ParticlePool.Spawn(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);
        PlayerBehaviour shootingPlayer = playerSynchronizer.GetPlayerById(ownerId);

        for (int i = 0; i < newParticle.ParticleSystems.Length; i++)
        {
            shootingPlayer.PlayerColor.AssignMaterialToParticleRenderer(newParticle.ParticleSystemRenderers[i], newParticle.ParticleSystems[i]);
        }
    }

    public static SByte3 GetParticleCompressor =>
        new SByte3()
        {
            min = new Vector3(-MyExtentions.PosABS, -MyExtentions.PosABS, 0),
            max = new Vector3(MyExtentions.PosABS, MyExtentions.PosABS, MyExtentions.MaxDeg),
            xBytes = 3,
            yBytes = 3,
            zBytes = 2,
        };

    public void DoMorphResetOnBounce(in uint projectileId)
    {

        DoMorphResetOnBounceRpc(projectileId);
        MorphResetOnBounceEvent(projectileId);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone)]
    void DoMorphResetOnBounceRpc(uint projectileId)
    {
        MorphResetOnBounceEvent(projectileId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void MorphResetOnBounceEvent(in uint projectileId)
    {
        ProjectileBehaviour projectile = GetProjectileByID(projectileId);
        projectile.morhpTime = projectile.data.morphTimeOnBounce;
    }

    public void SpawnBounceParticles(in Vector3 particlePosition, in Quaternion particleRotation, in ushort projectileType, byte ownerId)
    {

        SByte3 sByte3 = GetParticleCompressor;

        Vector3 rawData = new Vector3(particlePosition.x, particlePosition.y, Mathf.Repeat(particleRotation.eulerAngles.z, 360f));
        sByte3.SetFromVec3(rawData);

        Byte3 transformData = sByte3.GetByte3();

        SpawnBounceParticlesRpc(transformData, ownerId, projectileType);
        SpawnBounceParticleEvent(transformData, ownerId, projectileType);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone)]
    void SpawnBounceParticlesRpc(Byte3 transformData, byte ownerId, ushort projectileType)
    {
        SpawnBounceParticleEvent(transformData, ownerId, projectileType);
    }

    void SpawnBounceParticleEvent(in Byte3 transformData, in byte ownerId, in ushort projectileType)
    {

        SByte3 sByte3 = GetParticleCompressor;
        sByte3.SetFromByte3(transformData);

        Vector3 rawData = sByte3.GetVec3();
        Vector3 particlePosition = new Vector3(rawData.x, rawData.y, 0);
        Quaternion particleRotation = Quaternion.Euler(0, 0, rawData.z);

        ParticleBehaviour newParticle = ParticlePool.Spawn(GetBounceParticle(projectileType), particlePosition, particleRotation, null);
        PlayerBehaviour shootingPlayer = playerSynchronizer.GetPlayerById(ownerId);

        for (int i = 0; i < newParticle.ParticleSystems.Length; i++)
        {
            shootingPlayer.PlayerColor.AssignMaterialToParticleRenderer(newParticle.ParticleSystemRenderers[i], newParticle.ParticleSystems[i]);
        }
    }

    public void DespawnProjectile(uint projectileID, bool hit)
    {
        DespawnProjectileServerRpc(projectileID, hit);
        DespawnProjectileEvent(projectileID, hit);
    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    void DespawnProjectileServerRpc(uint projectileID, bool hit)
    {
        DespawnProjectileEvent(projectileID, hit);
    }
    void DespawnProjectileEvent(uint projectileID, bool hit)
    {
        ProjectileBehaviour deletedProjectile = GetProjectileByID(projectileID);
        if (!deletedProjectile) return;

        deletedProjectile.OnDespawn(hit);
        projectiles.Remove(deletedProjectile);
    }

/*    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void DespawnProjectileClientRpc(uint projectileID, bool hit)
    {

        if (IsHost) return;

        ProjectileBehaviour deletedProjectile = null;

        foreach (ProjectileBehaviour instance in projectiles)
        {

            if (instance.projectileID == projectileID)
            {

                if (instance != null) instance.OnDespawn(hit);

                deletedProjectile = instance;

                break;

            }

        }

        if (deletedProjectile != null) projectiles.Remove(deletedProjectile);

    }*/

    public void HitRegProjectile(uint projectileID)
    {

        HitRegProjectileServerRpc(projectileID);
        HitRegProjectileEvent(projectileID);

    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Reliable)]
    public void HitRegProjectileServerRpc(uint projectileID)
    {
        HitRegProjectileEvent(projectileID);

    }

    void HitRegProjectileEvent(uint projectileID)
    {
        foreach (ProjectileBehaviour instance in projectiles)
        {
            if (instance.projectileID == projectileID)
            {
                if (instance != null) instance.HitReg();
                break;
            }
        }
    }


    public void UpdateProjectile(ProjectileBehaviour instance)
    {

        /*        Vector2 pos, vel;
                float rot, ang;

                pos = instance.rb.position;
                vel = instance.rb.linearVelocity;
                rot = instance.rb.rotation;
                ang = instance.rb.angularVelocity;

                byte[] compPos = MyExtentions.EncodePosition(pos.x + 64, pos.y + 64);
                byte[] compVel = MyExtentions.EncodePosition(vel.x + 64, vel.y + 64);
                byte[] compRot = MyExtentions.EncodeRotation(rot);
                byte[] compRotVel = MyExtentions.EncodeFloat(ang);

                byte[] data = new byte[14]
                {
                    compPos[0], compPos[1], compPos[2], compPos[3],
                    compVel[0], compVel[1], compVel[2], compVel[3],
                    compRot[0], compRot[1],
                    compRotVel[0], compRotVel[1], compRotVel[2],
                    (byte) NetworkManager.Singleton.LocalClientId
                };*/

        byte[] data = MyExtentions.CompressRigidbody(instance.rb);

        NewUpdateProjectileRpc(data, instance.projectileID);

    }

    [Rpc(SendTo.NotMe, InvokePermission = RpcInvokePermission.Everyone, Delivery = RpcDelivery.Unreliable)]
    public void NewUpdateProjectileRpc(byte[] data, uint projectileId)
    {

        ProjectileBehaviour projectileToSync = GetProjectileByID(projectileId);

        if (!projectileToSync) return;

        MyExtentions.DecompressRigidbody(data, projectileToSync.rb, projectileToSync.data.syncSpeed);

    }

    public ProjectileBehaviour GetProjectileByID(uint projectileID)
    {
        foreach (ProjectileBehaviour instance in projectiles)
        {
            if (instance.projectileID == projectileID)
            {
                return instance;
            }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WeaponBuilder GetWeaponBuilderByTypeID(ushort typeID) => weapons[typeID];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Weapon GetRawWeaponByTypeID(ushort typeID) => weapons[typeID].weapon;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort GetFirstWeaponTypeId() => weapons.ElementAt(0).Key;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort GetSecondWeaponTypeId() => weapons.ElementAt(1).Key;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string GetWeaponName(ushort typeId1) => GetRawWeaponByTypeID(typeId1).weaponName;

    internal ushort GetNextAvailibleID()
    {
        for(ushort i = 1; i < ushort.MaxValue; i++) if (!weapons.ContainsKey(i)) return i;
        return 0;
    }

    internal void CreateWeaponFromMod(ref ProjectileCreator creator)
    {
        WeaponBuilder weapon = runtimePrefabTemplates.CreateNewWeaponPrefab(ref creator);
        weapons[weapon.typeID] = weapon;
    }

    #endregion

    public struct Boolean8
    {
        private byte bits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBool(int index) => (bits & (1 << index)) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBool(int index, bool value) => bits = (byte)((bits & ~(1 << index)) | ((value ? 1 : 0) << index));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetMask() => bits;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMask(byte mask) => bits = mask;
    }
}