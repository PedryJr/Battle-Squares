using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    float timer;

    private void Awake()
    {
        weapons = new Dictionary<ushort, WeaponBuilder>();
        projectiles = new List<ProjectileBehaviour>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        for (ushort i = 0; i < newWeapons.Length; i++)
        {
            weapons[newWeapons[i].typeID] = newWeapons[i];
        }
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {

        projectiles.Clear();

    }

    private void Update()
    {

        timer += Time.deltaTime;

        if (timer > 3)
        {
            timer = 0;
        }

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

                Vector2 newDirection = MyExtentions.AngleToNormalizedCoordinate(angleAtI);

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

                Vector2 newDirection = MyExtentions.AngleToNormalizedCoordinate(angleAtI);

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

        SpawnProjectileRpc((byte)NetworkManager.LocalClientId, projectileId, typeID, position, direction, fluctuation, bitBoolAsByte);
        SpawnProjectileEvent((byte)NetworkManager.LocalClientId, projectileId, typeID, position, direction, fluctuation, bitBoolAsByte);
    }


    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
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

        ProjectileInitData data = WeaponToProjectileData(ref weapon, projectileID, position, direction, fluctuation, owningPlayer);

        projectileBehaviour.ownerId = owningPlayer.GetID();
        projectileBehaviour.InitializeBullet(ref data);

        multiplier1 = weapon.recoil * Mods.at[13];
        multiplier2 = MyExtentions.EaseOutQuad(math.clamp(1 - (playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 28), 0, 1));

        forceToAdd = -direction.normalized * multiplier1 * multiplier2;
        owningPlayer.rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        owningPlayer.AnimatePlayer();
    }

    ProjectileInitData WeaponToProjectileData(ref Weapon weapon, uint projectileID, Vector2 position, Vector2 direction, float[] fluctuation, PlayerBehaviour owningPlayer)
    {

        return new()
        {
            projectileManager = this,
            owningPlayer = owningPlayer,
            IsLocalProjectile = owningPlayer.isLocalPlayer,
            id = projectileID,
            direction = direction,
            acceleration = weapon.projectileAcceleration,
            speed = weapon.projectileSpeed,
            position = position,
            projectileColor = owningPlayer.PlayerColor.ProjectileColor,
            projectileDarkerColor = owningPlayer.PlayerColor.ParticleColor,
            lifeTime = weapon.lifeTime,
            fluctuation = fluctuation,
            noGravity = weapon.noGravity,
            dieOnImpact = weapon.dieOnImpact,
            damageOnImpact = weapon.damageOnImpact,
            aoe = weapon.aoe,
            knockback = weapon.knockback,
            sticky = weapon.sticky,
            speedLimit = weapon.speedLimit,
            minSpeed = weapon.minSpeed,
            aoeDamage = weapon.aoeDamage,
            skipAoeOnTargetHit = weapon.skipAoeOnTargetHit,
            baseDamage = weapon.baseDamage,
            damageTimeScale = weapon.damageTimeScale,
            enableMorph = weapon.enableMorph,
            targetMorph = weapon.targetMorph,
            timeToMorph = weapon.timeToMorph,
            sync = weapon.sync,
            stickToSender = weapon.stickToSender,
            morhpAnimation = weapon.morphAnimation,
            melee = weapon.melee,
            meleeRange = weapon.meleeRange,
            swingDegrees = weapon.swingDegrees,
            meleePosAnimation = weapon.meleePosAnimation,
            oneTimeHit = weapon.oneTimeHit,
            meleeRotAnimation = weapon.meleeRotAnimation,
            meleeRotation = weapon.meleeRotation,
            homing = weapon.homing,
            spinSpeed = weapon.spinSpeed,
            homingStrength = weapon.homingStrength,
            homingDistance = weapon.homingDistance,
            syncSpeed = weapon.syncSpeed,
            rotationFlipOnImpact = weapon.rotationFlipOnImpact,
            dieFromProjectiles = weapon.dieFromProjectiles,
            dontBlockProjectiles = weapon.dontBlockProjectiles,
            bounceOfPlayers = weapon.bounceOfPlayers,
            slowDownAmount = weapon.slowDownAmount,
            senderSpeedOnDeath = weapon.senderSpeedOnDeath,
            lingeringDamage = weapon.lingeringDamage,
            lingeringFrequency = weapon.lingeringFrequency,
            alignDirection = weapon.alignDirection,
            bounces = weapon.bounces,
            bounceParticle = weapon.bounceParticle,
            impactParticle = weapon.impactParticle,
            clampMorph = weapon.clampMorph,
            bounceSpeedLoss = weapon.bounceSpeedLoss,
            bounceAngleTilt = weapon.bounceAngleTilt,
            hover = weapon.hover,
            hoverDistance = weapon.hoverDistance,
            hoverDistanceAttenuation = weapon.hoverDistanceAttenuation,
            hoverFloorRadius = weapon.hoverFloorRadius,
            hoverStrength = weapon.hoverStrength,
            timeForFullHoverEffect = weapon.timeForFullHoverEffect,
            projectileSpawnEvents = weapon.projectileSpawnEvents,
            
        };

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

    public void SpawnParticles(Vector3 particlePosition, Quaternion particleRotation, ushort projectileType)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        byte[] rotation = MyExtentions.EncodeRotation(particleRotation.eulerAngles.z);

        byte[] particleData = new byte[4];
        particleData[0] = (byte)ignoreId;
        particleData[1] = rotation[0];
        particleData[2] = rotation[1];

        ParticleBehaviour newParticle = ParticlePool.Spawn(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);
        PlayerBehaviour shootingPlayer = playerSynchronizer.GetPlayerById(ignoreId);

        for(int i = 0; i < newParticle.ParticleSystems.Length; i++)
        {
            shootingPlayer.PlayerColor.AssignMaterialToParticleRenderer(newParticle.ParticleSystemRenderers[i], newParticle.ParticleSystems[i]);
        }

        if (IsHost) SpawnParticlesClientRpc(particlePosition, particleData, projectileType);
        if (!IsHost) SpawnParticlesServerRpc(particlePosition, particleData, projectileType);

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnParticlesServerRpc(Vector3 particlePosition, byte[] newParticleData, ushort projectileType)
    {

        ulong ignoreId = newParticleData[0];
        if (NetworkManager.LocalClientId == ignoreId) return;

        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[1], newParticleData[1] }));


        SpawnParticlesClientRpc(particlePosition, newParticleData, projectileType);

        ParticleBehaviour newParticle = ParticlePool.Spawn(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);
        PlayerBehaviour shootingPlayer = playerSynchronizer.GetPlayerById(ignoreId);

        for (int i = 0; i < newParticle.ParticleSystems.Length; i++)
        {
            shootingPlayer.PlayerColor.AssignMaterialToParticleRenderer(newParticle.ParticleSystemRenderers[i], newParticle.ParticleSystems[i]);
        }

    }

    [ClientRpc]
    public void SpawnParticlesClientRpc(Vector3 particlePosition, byte[] newParticleData, ushort projectileType)
    {

        ulong ignoreId = newParticleData[0];
        if (IsHost) return;
        if (NetworkManager.LocalClientId == ignoreId) return;

        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[1], newParticleData[2] }));

        ParticleBehaviour newParticle = ParticlePool.Spawn(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);
        PlayerBehaviour shootingPlayer = playerSynchronizer.GetPlayerById(ignoreId);

        for (int i = 0; i < newParticle.ParticleSystems.Length; i++)
        {
            shootingPlayer.PlayerColor.AssignMaterialToParticleRenderer(newParticle.ParticleSystemRenderers[i], newParticle.ParticleSystems[i]);
        }

    }

    public void DespawnProjectile(uint projectileID, bool hit)
    {

        if (IsHost)
        {

            DespawnProjectileClientRpc(projectileID, hit);

        }

        if (!IsHost)
        {

            DespawnProjectileServerRpc(projectileID, hit);

        }

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

    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void DespawnProjectileServerRpc(uint projectileID, bool hit)
    {

        DespawnProjectileClientRpc(projectileID, hit);

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

    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
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

    }

    public void HitRegProjectile(uint projectileID)
    {

        if (IsHost)
        {

            HitRegProjectileClientRpc(projectileID);

        }

        if (!IsHost)
        {

            HitRegProjectileServerRpc(projectileID);

        }

        foreach (ProjectileBehaviour instance in projectiles)
        {

            if (instance.projectileID == projectileID)
            {

                if (instance != null) instance.HitReg();

                break;

            }

        }

    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void HitRegProjectileServerRpc(uint projectileID)
    {

        HitRegProjectileClientRpc(projectileID);

        foreach (ProjectileBehaviour instance in projectiles)
        {

            if (instance.projectileID == projectileID)
            {

                if (instance != null) instance.HitReg();

                break;

            }

        }

    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void HitRegProjectileClientRpc(uint projectileID)
    {

        if (IsHost) return;

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

        Vector2 pos, vel;
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
        };

        NewUpdateProjectileRpc(data, instance.projectileID);

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void NewUpdateProjectileRpc(byte[] data, uint projectileId)
    {

        if ((byte)playerSynchronizer.localSquare.id == data[13]) return;

        ProjectileBehaviour projectileToSync = null;
        foreach (ProjectileBehaviour instance in projectiles)
        {
            if (!(projectileId == instance.projectileID)) continue;
            projectileToSync = instance;
            break;
        }

        if (!projectileToSync) return;

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

        projectileToSync.rb.position = new Vector2(xPos, yPos);
        projectileToSync.rb.rotation = rot;
        projectileToSync.rb.linearVelocity = new Vector2(xVel, yVel);
        projectileToSync.rb.angularVelocity = rotVel;

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