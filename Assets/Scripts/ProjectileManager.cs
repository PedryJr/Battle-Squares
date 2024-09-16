using Steamworks;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {

        projectiles = new List<ProjectileBehaviour>();
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {

        projectiles.Clear();

    }

    private void Update()
    {

        timer += Time.deltaTime;

        if(timer > 3)
        {
            timer = 0;
        }

    }

    public void SpawnProjectile(ProjectileType type, Vector2 position, Vector2 direction, PlayerBehaviour shootingPlayer, Vector3 color, Vector3 darkColor)
    {

        ProjectileBehaviour projectileBehaviour = null;

        ProjectileInitData data = new ProjectileInitData();
        uint newId = (uint) new System.Random().Next(0, 2147483640) + (uint) new System.Random().Next(0, 2147483640);

        foreach (Weapon weapon in weapons)
        {

            if(weapon.type == type)
            {

                Span<float> burstData = stackalloc float[weapon.burst * 2];
                for (int i = 0; i < burstData.Length; i += 2) 
                { 
                    burstData[i] = UnityEngine.Random.Range(2.7f, 3.45f); 
                    burstData[i + 1] = UnityEngine.Random.Range(2.7f, 3.45f); 
                }

                Span<float> fluctuation = stackalloc float[2];
                for (int i = 0; i < fluctuation.Length; i++)
                {
                    fluctuation[i] = UnityEngine.Random.Range(-weapon.fluctuation, weapon.fluctuation);
                }

                projectileBehaviour = Instantiate(weapon.projectile).GetComponent<ProjectileBehaviour>();

                data.projectileManager = this;
                data.IsLocalProjectile = shootingPlayer.isLocalPlayer;
                data.id = newId;
                data.direction = direction;
                data.acceleration = weapon.projectileAcceleration;
                data.speed = weapon.projectileSpeed;
                data.position = position;
                data.projectileColor = new Color(color.x, color.y, color.z);
                data.projectileDarkerColor = new Color(darkColor.x, darkColor.y, darkColor.z);
                data.burst = weapon.burst;
                data.lifeTime = weapon.lifeTime;
                data.burstData = burstData.ToArray();
                data.fluctuation = fluctuation.ToArray();
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

                projectileBehaviour.ownerId = NetworkManager.LocalClientId;

                break;

            }

        }

        ulong ignoreId = NetworkManager.LocalClientId;

        if (IsHost)
        {

            SpawnProjectileClientRpc(type, position, direction, (byte) ignoreId, newId, color, darkColor, data.burstData, data.fluctuation);

        }

        if (!IsHost)
        {

            SpawnProjectileServerRpc(type, position, direction, (byte) ignoreId, newId, color, darkColor, data.burstData, data.fluctuation);

        }

        projectileBehaviour.InitializeBullet(data);

        float multiplier1, multiplier2;
        multiplier1 = projectileBehaviour.recoil;
        multiplier2 = MyExtentions.EaseOutQuad(Mathf.Clamp01(1 - (playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 28)));
        
        Vector2 forceToAdd = -direction.normalized * multiplier1 * multiplier2;

        shootingPlayer.rb.AddForce(forceToAdd, ForceMode2D.Impulse);

    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void SpawnProjectileServerRpc(ProjectileType type, Vector2 position, Vector2 direction, byte ignoreId, uint projectileID, Vector3 color, Vector3 darkColor, float[] burstData, float[] fluctuation)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        ProjectileInitData data = new ProjectileInitData();

        ProjectileBehaviour projectileBehaviour = null;

        foreach (Weapon weapon in weapons)
        {

            if (weapon.type == type)
            {

                projectileBehaviour = Instantiate(weapon.projectile).GetComponent<ProjectileBehaviour>();

                data.projectileManager = this;
                data.IsLocalProjectile = false;
                data.id = projectileID;
                data.direction = direction.normalized;
                data.acceleration = weapon.projectileAcceleration;
                data.speed = weapon.projectileSpeed;
                data.position = position;
                data.projectileColor = new Color(color.x, color.y, color.z);
                data.projectileDarkerColor = new Color(darkColor.x, darkColor.y, darkColor.z);
                data.burst = weapon.burst;
                data.lifeTime = weapon.lifeTime;
                data.burstData = burstData;
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

                projectileBehaviour.ownerId = ignoreId;

                break;

            }

        }

        SpawnProjectileClientRpc(type, position, data.direction, (byte) ignoreId, data.id, color, darkColor, burstData, fluctuation);

        projectileBehaviour.transform.position = position;
        projectileBehaviour.InitializeBullet(data);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void SpawnProjectileClientRpc(ProjectileType type, Vector2 position, Vector2 direction, byte ignoreId, uint projectileID, Vector3 color, Vector3 darkColor, float[] burstData, float[] fluctuation)
    {

        if ((byte) NetworkManager.LocalClientId == ignoreId) return;

        ProjectileInitData data = new ProjectileInitData();

        if (IsHost) return;

        ProjectileBehaviour projectileBehaviour = null;

        foreach (Weapon weapon in weapons)
        {

            if (weapon.type == type)
            {

                projectileBehaviour = Instantiate(weapon.projectile).GetComponent<ProjectileBehaviour>();

                data.projectileManager = this;
                data.IsLocalProjectile = false;
                data.id = projectileID;
                data.direction = direction.normalized;
                data.acceleration = weapon.projectileAcceleration;
                data.speed = weapon.projectileSpeed;
                data.position = position;
                data.projectileColor = new Color(color.x, color.y, color.z);
                data.projectileDarkerColor = new Color(darkColor.x, darkColor.y, darkColor.z);
                data.burst = weapon.burst;
                data.lifeTime = weapon.lifeTime;
                data.burstData = burstData;
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

                projectileBehaviour.ownerId = ignoreId;

                break;

            }

        }

        projectileBehaviour.transform.position = position;
        projectileBehaviour.InitializeBullet(data);
    }

    GameObject GetNozzleParticle(ProjectileType projectileType)
    {

        foreach(Weapon weapon in weapons)
        {

            if(weapon.type == projectileType) return weapon.launchParticle;

        }
        return null;
    }

    public void SpawnParticles(Vector3 particlePosition, Quaternion particleRotation, UnityEngine.Color particleColor, ProjectileType projectileType)
    {

        ulong ignoreId = NetworkManager.LocalClientId;

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach(ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial = Instantiate(particle.material);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

        if (IsHost)
        {

            SpawnParticlesClientRpc(particlePosition, particleRotation, particleColor, ignoreId, projectileType);

        }
        if (!IsHost)
        {

            SpawnParticlesServerRpc(particlePosition, particleRotation, particleColor, ignoreId, projectileType);

        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnParticlesServerRpc(Vector3 particlePosition, Quaternion particleRotation, Vector4 particleColor, ulong ignoreId, ProjectileType projectileType)
    {

        if (NetworkManager.LocalClientId == ignoreId) return;

        SpawnParticlesClientRpc(particlePosition, particleRotation, particleColor, ignoreId, projectileType);

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial = Instantiate(particle.material);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
        }

    }

    [ClientRpc]
    public void SpawnParticlesClientRpc(Vector3 particlePosition, Quaternion particleRotation, Vector4 particleColor, ulong ignoreId, ProjectileType projectileType)
    {

        if (IsHost) return;

        if (NetworkManager.LocalClientId == ignoreId) return;

        GameObject newParticle = Instantiate(GetNozzleParticle(projectileType), particlePosition, particleRotation, null);

        foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            Material particleMaterial = Instantiate(particle.material);
            particle.material = particleMaterial;
            particle.material.color = particleColor;
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

                if (!instance.IsDestroyed()) instance.OnDespawn(hit);
                
                deletedProjectile = instance;

                break;

            }

        }

        if(deletedProjectile != null) projectiles.Remove(deletedProjectile);

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

                if (!instance.IsDestroyed()) instance.OnDespawn(hit);

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

                if (!instance.IsDestroyed()) instance.OnDespawn(hit);

                deletedProjectile = instance;

                break;

            }

        }

        if (deletedProjectile != null) projectiles.Remove(deletedProjectile);

    }

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
        public bool enableMorph;
        public Vector3 targetMorph;
        public float timeToMorph;

    }

    public enum ProjectileType
    {

        Revolver,
        Sniper,
        Minigun,
        Shotgun,
        Rocket,
        Granade,
        Raygun

    }

}
