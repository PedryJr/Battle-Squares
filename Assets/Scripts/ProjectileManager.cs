using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;
using Random = System.Random;
using Unity.Mathematics;
using Unity.Burst;
using static ProjectileManager;

[BurstCompile]
public sealed class ProjectileManager : NetworkBehaviour
{

    [SerializeField]
    public Weapon[] weapons;

    [SerializeField]
    GameObject nozzleParticles;

    [SerializeField]
    public List<ProjectileBehaviour> projectiles;

    public NozzleBehaviour localNozzle;

    public PlayerSynchronizer playerSynchronizer;

    float timer;
    [BurstCompile]
    private void Awake()
    {

        projectiles = new List<ProjectileBehaviour>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

    }
    [BurstCompile]
    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {

        projectiles.Clear();

    }
    [BurstCompile]
    private void Update()
    {

        timer += Time.deltaTime;

        if (timer > 3)
        {
            timer = 0;
        }

    }
    [BurstCompile]
    public void SpawnProjectile(ProjectileType type, Vector2 position, Vector2 direction, PlayerBehaviour shootingPlayer)
    {

        Weapon weapon = new Weapon();
        uint projectileId = (uint)new System.Random().Next(0, 2147483640) + (uint)new System.Random().Next(0, 2147483640);

        foreach (Weapon usedWeapon in weapons)
        {
            if (usedWeapon.type == type) { weapon = usedWeapon; break; }
        }

        float[] burstData = new float[weapon.burst * 2];
        for (int i = 0; i < burstData.Length; i += 2)
        {
            burstData[i] = UnityEngine.Random.Range(2.7f, 3.45f);
            burstData[i + 1] = UnityEngine.Random.Range(2.7f, 3.45f);
        }

        float[] fluctuation = new float[2];
        for (int i = 0; i < fluctuation.Length; i++)
        {
            fluctuation[i] = UnityEngine.Random.Range(-weapon.fluctuation, weapon.fluctuation);
        }

        if (weapon.flipFlop) shootingPlayer.nozzleBehaviour.flipFlop = !shootingPlayer.nozzleBehaviour.flipFlop;

        SpawnProjectileRpc((byte) NetworkManager.LocalClientId, projectileId, type, position, direction, burstData, fluctuation, shootingPlayer.nozzleBehaviour.flipFlop);
        SpawnProjectileEvent((byte) NetworkManager.LocalClientId, projectileId, type, position, direction, burstData, fluctuation, shootingPlayer.nozzleBehaviour.flipFlop);

    }
    [BurstCompile]
    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void SpawnProjectileRpc(byte sourceId, uint projectileID, ProjectileType type, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, bool flipFlop)
    {
        if (sourceId == (byte) NetworkManager.LocalClientId) return;
        SpawnProjectileEvent(sourceId, projectileID, type, position, direction, burstData, fluctuation, flipFlop);
    }
    [BurstCompile]
    void SpawnProjectileEvent(byte sourceId, uint projectileID, ProjectileType type, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, bool flipFlop)
    {

        ProjectileBehaviour projectileBehaviour = null;
        PlayerBehaviour owningPlayer = null;

        Weapon weapon = new();
        ProjectileInitData data = new();
        Vector2 forceToAdd = new();

        float multiplier1, multiplier2;

        foreach (Weapon usedWeapon in weapons) if (usedWeapon.type == type) { weapon = usedWeapon; break; }
        foreach (PlayerData playerData in playerSynchronizer.playerIdentities) if ((byte)playerData.square.id == sourceId) { owningPlayer = playerData.square; break; }

        projectileBehaviour = Instantiate(weapon.projectile, position, Quaternion.identity, null).GetComponent<ProjectileBehaviour>();
        projectileBehaviour.flipFlop = flipFlop;

        data = WeaponToProjectileData(weapon, projectileID, position, direction, burstData, fluctuation, owningPlayer);

        projectileBehaviour.ownerId = owningPlayer.id;
        projectileBehaviour.InitializeBullet(data);

        multiplier1 = weapon.recoil * Mods.at[13];
        multiplier2 = MyExtentions.EaseOutQuad(math.clamp(1 - (playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 28), 0, 1));

        forceToAdd = -direction.normalized * multiplier1 * multiplier2;
        owningPlayer.rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        owningPlayer.AnimatePlayer();

    }
    [BurstCompile]
    ProjectileInitData WeaponToProjectileData(Weapon weapon, uint projectileID, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, PlayerBehaviour owningPlayer)
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
            projectileColor = owningPlayer.playerColor,
            projectileDarkerColor = owningPlayer.playerDarkerColor,
            burst = weapon.burst,
            lifeTime = weapon.lifeTime,
            burstData = burstData,
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
            retornToSender = weapon.returnToSender,
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

        };

    }

    #region otherSyncs
    [BurstCompile]
    public uint GenerateRandomUInt()
    {
        byte[] buffer = new byte[4];
        new Random().NextBytes(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }
    [BurstCompile]
    GameObject GetNozzleParticle(ProjectileType projectileType)
    {

        foreach (Weapon weapon in weapons)
        {

            if (weapon.type == projectileType) return weapon.launchParticle;

        }
        return null;
    }

    static Dictionary<ulong, Material> particleMaterials = new Dictionary<ulong, Material>();
    byte[] particleData = new byte[7];
    [BurstCompile]
    public void SpawnParticles(Vector3 particlePosition, Quaternion particleRotation, UnityEngine.Color particleColor, ProjectileType projectileType)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        Debug.Log(particleColor);

        byte[] rotation = MyExtentions.EncodeRotation(particleRotation.eulerAngles.z);

        particleData[0] = (byte) Mathf.FloorToInt(particleColor.r * 255);
        particleData[1] = (byte) Mathf.FloorToInt(particleColor.g * 255);
        particleData[2] = (byte) Mathf.FloorToInt(particleColor.b * 255);

        particleData[3] = (byte) ignoreId;

        particleData[4] = (byte)projectileType;

        particleData[5] = rotation[0];
        particleData[6] = rotation[1];

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {

            Material particleMaterial;

            if (particleMaterials.ContainsKey(ignoreId)) particleMaterial = particleMaterials[ignoreId];
            else
            {
                particleMaterial = Instantiate(particle.material);
                particleMaterials.Add(ignoreId, particleMaterial);
            }
            for (int i = 0; i < particle.materials.Length; i++) Destroy(particle.materials[i]);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

        if (IsHost)
        {

            SpawnParticlesClientRpc(particlePosition, particleData);

        }
        if (!IsHost)
        {

            SpawnParticlesServerRpc(particlePosition, particleData);

        }

    }
    [BurstCompile]
    [ServerRpc(RequireOwnership = false)]
    public void SpawnParticlesServerRpc(Vector3 particlePosition, byte[] newParticleData)
    {

        Vector4 particleColor = new Vector4(newParticleData[0] / 255f, newParticleData[1] / 255f, newParticleData[2] / 255f, 1f);
        ulong ignoreId = newParticleData[3];
        ProjectileType projectileType = (ProjectileType) newParticleData[4];
        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[5], newParticleData[6] }));

        if (NetworkManager.LocalClientId == ignoreId) return;

        SpawnParticlesClientRpc(particlePosition, newParticleData);

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial;
            if (particleMaterials.ContainsKey(ignoreId)) particleMaterial = particleMaterials[ignoreId];
            else
            {
                particleMaterial = Instantiate(particle.material);
                particleMaterials.Add(ignoreId, particleMaterial);
            }
            for (int i = 0; i < particle.materials.Length; i++) Destroy(particle.materials[i]);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

    }
    [BurstCompile]
    [ClientRpc]
    public void SpawnParticlesClientRpc(Vector3 particlePosition, byte[] newParticleData)
    {

        Vector4 particleColor = new Vector4(newParticleData[0] / 255f, newParticleData[1] / 255f, newParticleData[2] / 255f, 1f);
        ulong ignoreId = newParticleData[3];
        ProjectileType projectileType = (ProjectileType)newParticleData[4];
        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[5], newParticleData[6] }));

        if (IsHost) return;

        if (NetworkManager.LocalClientId == ignoreId) return;

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial;
            if (particleMaterials.ContainsKey(ignoreId)) particleMaterial = particleMaterials[ignoreId];
            else
            {
                particleMaterial = Instantiate(particle.material);
                particleMaterials.Add(ignoreId, particleMaterial);
            }
            for (int i = 0; i < particle.materials.Length; i++) Destroy(particle.materials[i]);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

    }
    [BurstCompile]
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
    [BurstCompile]
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
    [BurstCompile]
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
    [BurstCompile]
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
    [BurstCompile]
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
    [BurstCompile]
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

    [BurstCompile]
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
    [BurstCompile]
    [Rpc(SendTo.Everyone, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
    public void NewUpdateProjectileRpc(byte[] data, uint projectileId)
    {

        if ((byte) playerSynchronizer.localSquare.id == data[13]) return;

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


    #endregion
    [BurstCompile]
    [Serializable]
    public struct Weapon
    {

        public ProjectileType type; 
        public GameObject projectile;
        public GameObject launchParticle;
        public int projectileAmmo;
        public float reloadTime;
        public float shootingInterval;
        public float projectileSpeed;
        public float projectileAcceleration;
        public float lifeTime;
        public bool holdable;
        public int burst;
        public int bounces;
        public float fluctuation;
        public bool noGravity;
        public bool dieOnImpact;
        public bool damageOnImpact;
        public bool sticky;
        public float aoe;
        public bool skipAoeOnTargetHit;
        public float knockback;
        public float speedLimit;
        public float minSpeed;
        public float aoeDamage;
        public float baseDamage;
        public float damageTimeScale;
        public float recoil;
        public bool enableMorph;
        public Vector3 targetMorph;
        public float timeToMorph;
        public AnimationCurve morphAnimation;
        public bool sync;
        public float syncSpeed;
        public bool returnToSender;
        public bool stickToSender;
        public bool melee;
        public bool oneTimeHit;
        public float meleeRange;
        public float swingDegrees;
        public float meleeRotation;
        public AnimationCurve meleePosAnimation;
        public AnimationCurve meleeRotAnimation;
        public bool flipFlop;
        public bool homing;
        public float homingStrength;
        public float homingDistance;
        public float spinSpeed;
        public bool rotationFlipOnImpact;
        public bool dieFromProjectiles;
        public bool dontBlockProjectiles;
        public bool bounceOfPlayers;
        public float slowDownAmount;
        public float senderSpeedOnDeath;

    }

    public enum ProjectileType
    {

        Revolver,
        Sniper,
        Minigun,
        Shotgun,
        Rocket,
        Granade,
        Raygun,
        Charge,
        Katana,
        Boomerang,
        Hailmaker,
        Scortcher

    }

}
