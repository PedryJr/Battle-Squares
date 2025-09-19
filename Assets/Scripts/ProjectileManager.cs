using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Pool;
using System.Runtime.CompilerServices;


public sealed class ProjectileManager : NetworkBehaviour
{

    private static ObjectPool<ProjectileBehaviour> projectilePool;

    [SerializeField]
    public Weapon[] weapons;

    [SerializeField]
    GameObject nozzleParticles;

    [SerializeField]
    public List<ProjectileBehaviour> projectiles;

    public NozzleBehaviour localNozzle;

    public PlayerSynchronizer playerSynchronizer;

    float timer;

    private void Awake()
    {
        projectiles = new List<ProjectileBehaviour>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].pool = new MyObjectPool<ProjectileBehaviour>();
            weapons[i].pool.Initialize(weapons[i].projectile.GetComponent<ProjectileBehaviour>());
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

    public void SpawnProjectile(ProjectileType type, Vector2 position, Vector2 direction, PlayerBehaviour shootingPlayer)
    {

        int weaponIndex = 0;
        uint projectileId = (uint)new System.Random().Next(0, 2147483640) + (uint)new System.Random().Next(0, 2147483640);

        for(int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == type)
            {
                weaponIndex = i;
                break;
            }
        }

        float[] burstData = new float[weapons[weaponIndex].burst * 2];
        float[] fluctuation = new float[2];

        for (int i = 0; i < burstData.Length; i += 2)
        {
            burstData[i] = UnityEngine.Random.Range(2.7f, 3.45f);
            burstData[i + 1] = UnityEngine.Random.Range(2.7f, 3.45f);
        }

        for (int i = 0; i < fluctuation.Length; i++)
        {
            fluctuation[i] = UnityEngine.Random.Range(-weapons[weaponIndex].fluctuation, weapons[weaponIndex].fluctuation);
        }

        if (weapons[weaponIndex].flipFlop) shootingPlayer.nozzleBehaviour.flipFlop = !shootingPlayer.nozzleBehaviour.flipFlop;

        SpawnProjectileRpc((byte) NetworkManager.LocalClientId, projectileId, type, position, direction, burstData, fluctuation, shootingPlayer.nozzleBehaviour.flipFlop);
        SpawnProjectileEvent((byte) NetworkManager.LocalClientId, projectileId, type, position, direction, burstData, fluctuation, shootingPlayer.nozzleBehaviour.flipFlop);

    }

    [Rpc(SendTo.NotMe, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    void SpawnProjectileRpc(byte sourceId, uint projectileID, ProjectileType type, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, bool flipFlop)
    {
        if (sourceId == (byte) NetworkManager.LocalClientId) return;
        SpawnProjectileEvent(sourceId, projectileID, type, position, direction, burstData, fluctuation, flipFlop);
    }

    void SpawnProjectileEvent(byte sourceId, uint projectileID, ProjectileType type, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, bool flipFlop)
    {

        ProjectileBehaviour projectileBehaviour = null;
        PlayerBehaviour owningPlayer = null;

        int weaponIndex = 0;
        Vector2 forceToAdd = new();

        float multiplier1, multiplier2;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == type)
            {
                weaponIndex = i;
                break;
            }
        }

        owningPlayer = playerSynchronizer.GetPlayerById(sourceId);

        projectileBehaviour = weapons[weaponIndex].pool.GetFromPool(position, Quaternion.identity, null, projectileID);
        projectileBehaviour.flipFlop = flipFlop;

        ProjectileInitData data = WeaponToProjectileData(ref weapons[weaponIndex], projectileID, position, direction, burstData, fluctuation, owningPlayer);

        projectileBehaviour.ownerId = owningPlayer.id;
        projectileBehaviour.InitializeBullet(ref data);

        multiplier1 = weapons[weaponIndex].recoil * Mods.at[13];
        multiplier2 = MyExtentions.EaseOutQuad(math.clamp(1 - (playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 28), 0, 1));

        forceToAdd = -direction.normalized * multiplier1 * multiplier2;
        owningPlayer.rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        owningPlayer.AnimatePlayer();

    }

    ProjectileInitData WeaponToProjectileData(ref Weapon weapon, uint projectileID, Vector2 position, Vector2 direction, float[] burstData, float[] fluctuation, PlayerBehaviour owningPlayer)
    {

        return new()
        {
            original = weapon.pool.GetOriginal,
            projectileType = weapon.type,
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
            lingeringDamage = weapon.lingeringDamage,
            lingeringFrequency = weapon.lingeringFrequency,
            alignDirection = weapon.alignDirection,

        };

    }

    #region otherSyncs

    public uint GenerateRandomUInt()
    {
        byte[] buffer = new byte[4];
        new Random().NextBytes(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

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

    public void SpawnParticles(Vector3 particlePosition, Quaternion particleRotation, ProjectileType projectileType)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        byte[] rotation = MyExtentions.EncodeRotation(particleRotation.eulerAngles.z);

        particleData[3] = (byte) ignoreId;

        particleData[4] = (byte)projectileType;

        particleData[5] = rotation[0];
        particleData[6] = rotation[1];

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);


        Color particleColor = playerSynchronizer.GetPlayerById(ignoreId).PlayerColor.ParticleColor;

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

            ParticleSystem.MainModule mainModule = particle.GetComponent<ParticleSystem>().main;
            mainModule.startColor = particleColor;

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

    [ServerRpc(RequireOwnership = false)]
    public void SpawnParticlesServerRpc(Vector3 particlePosition, byte[] newParticleData)
    {

        ulong ignoreId = newParticleData[3];
        if (NetworkManager.LocalClientId == ignoreId) return;

        Color particleColor = playerSynchronizer.GetPlayerById(ignoreId).PlayerColor.ParticleColor;
        ProjectileType projectileType = (ProjectileType) newParticleData[4];
        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[5], newParticleData[6] }));


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

            ParticleSystem.MainModule mainModule = particle.GetComponent<ParticleSystem>().main;
            mainModule.startColor = particleColor;
        }

    }

    [ClientRpc]
    public void SpawnParticlesClientRpc(Vector3 particlePosition, byte[] newParticleData)
    {

        ulong ignoreId = newParticleData[3];
        if (IsHost) return;

        Color particleColor = playerSynchronizer.GetPlayerById(ignoreId).PlayerColor.ParticleColor;
        ProjectileType projectileType = (ProjectileType)newParticleData[4];
        Quaternion particleRotation = Quaternion.Euler(0, 0, MyExtentions.DecodeRotation(new byte[] { newParticleData[5], newParticleData[6] }));


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

            ParticleSystem.MainModule mainModule = particle.GetComponent<ParticleSystem>().main;
            mainModule.startColor = particleColor;
        }

    }

    public void DespawnProjectile(uint projectileID, bool hit)
    {
        if (IsHost) DespawnProjectileClientRpc(projectileID, hit);
        if (!IsHost) DespawnProjectileServerRpc(projectileID, hit);
        DespawnProjectileLocal(projectileID, hit);
    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void DespawnProjectileServerRpc(uint projectileID, bool hit)
    {
        DespawnProjectileClientRpc(projectileID, hit);
        DespawnProjectileLocal(projectileID, hit);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void DespawnProjectileClientRpc(uint projectileID, bool hit)
    {

        if (IsHost) return;

        DespawnProjectileLocal(projectileID, hit);

    }

    void DespawnProjectileLocal(uint projectileID, bool hit)
    {
        ProjectileBehaviour deletedProjectile = null;

        foreach (ProjectileBehaviour instance in projectiles)
        {

            if (instance.projectileID == projectileID)
            {

                if (instance != null)
                {
                    instance.OnDespawn(hit);
                    instance.Release();
                }

                deletedProjectile = instance;

                break;

            }

        }

        if (deletedProjectile != null) projectiles.Remove(deletedProjectile);
    }
    public void ReleaseProjectile(uint projectileID, ProjectileType projectileType)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == projectileType)
            {
                Debug.Log(projectileID);
                Debug.Log(projectileType);
                Debug.Log(weapons[i].pool.GetActiveCount);
                Debug.Log(weapons[i].pool.GetInactiveCount);
                weapons[i].pool.ReturnToPool(projectileID);
                Debug.Log(weapons[i].pool.GetInactiveCount);
                return;
            }
        }
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
        public float lingeringDamage;
        public float lingeringFrequency;
        public bool alignDirection;
        public MyObjectPool<ProjectileBehaviour> pool;
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
        Scortcher,
        Bounzooka

    }

}

public sealed class MyObjectPool<T> where T : Component, IRevert<T>
{
    private T prefab;
    public T GetOriginal => prefab;
    private Dictionary<uint, T> activeObjects;
    private List<T> inactiveObjects;

    public int GetActiveCount => activeObjects.Count;
    public int GetInactiveCount => inactiveObjects.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(T prefab, int prewarmCount = 0)
    {
        this.prefab = prefab;
        activeObjects = new Dictionary<uint, T>();
        inactiveObjects = new List<T>(prewarmCount);

        // Optionally pre-instantiate objects
        for (int i = 0; i < prewarmCount; i++)
        {
            T obj = UnityEngine.Object.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            inactiveObjects.Add(obj);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetFromPool(Vector3 objectPosition, Quaternion objectRotation, Transform objectParent, uint objectID)
    {
        T obj;

        if (inactiveObjects.Count > 0)
        {
            // Take the last inactive object to avoid shifting
            int lastIndex = inactiveObjects.Count - 1;
            obj = inactiveObjects[lastIndex];
            obj.transform.SetParent(objectParent);
            obj.transform.position = objectPosition;
            obj.transform.rotation = objectRotation;
            obj.transform.localScale = prefab.transform.localScale;
            obj.Revert(prefab);
            obj.gameObject.SetActive(true);
            inactiveObjects.RemoveAt(lastIndex);
        }
        else
        {
            // No inactive objects, instantiate a new one
            obj = UnityEngine.Object.Instantiate(prefab, objectPosition, objectRotation, objectParent);
        }

        // Track in active objects
        activeObjects[objectID] = obj;

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnToPool(uint objectID)
    {
        if (!activeObjects.TryGetValue(objectID, out T obj)) return;

        // Remove from active
        activeObjects.Remove(objectID);

        // Deactivate and return to pool
        obj.gameObject.SetActive(false);
        inactiveObjects.Add(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetActive(uint objectID, out T obj) => activeObjects.TryGetValue(objectID, out obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // Destroy all objects if needed
        foreach (var obj in activeObjects.Values) UnityEngine.Object.Destroy(obj.gameObject);
        foreach (var obj in inactiveObjects) UnityEngine.Object.Destroy(obj.gameObject);

        activeObjects.Clear();
        inactiveObjects.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveFromSystem(T obj, uint objID)
    {
        activeObjects.Remove(objID);
        inactiveObjects.Remove(obj);
    }
}

public interface IRevert<T>
{
    [MethodImpl(512)]
    public void Revert(T original);

}