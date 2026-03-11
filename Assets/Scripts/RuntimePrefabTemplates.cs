using BattleSquaresSDK;
using System;
using UnityEngine;
using static AssetWrappers;
using static UnityEngine.Rendering.STP;
using static WeaponBuilder;

public class RuntimePrefabTemplates : MonoBehaviour
{
    [SerializeField] GameObject defaultParticleEmitter;
    [SerializeField] WeaponBuilder fullFeatureWeapon;

    public WeaponBuilder CreateNewWeaponPrefab(ref ProjectileCreator creator)
    {
        WeaponBuilder newWeaponBuilder = Instantiate(fullFeatureWeapon);
        newWeaponBuilder.specs = FromProjectileParamConfigToWeaponData(creator.projectileParamConfig);
        newWeaponBuilder.specs.weaponName = creator.metaData.Name;
        newWeaponBuilder.specs.projectileSpawnEvents = new ProjectileSpawnEvent[0];
        newWeaponBuilder.specs.typeID = creator.TypeID;
        if (!string.IsNullOrEmpty(creator.metaData.pathToLogoPNG))
        {
            ITexture2D itex = AssetCreator.CreateTexture(creator.metaData.pathToLogoPNG, BattleSquaresSDK.TextureWrapMode.Clamp, BattleSquaresSDK.FilterMode.Point);
            ISprite isprite = AssetCreator.CreateSprite(itex, 100);

            Sprite sprite = (isprite as SpriteWrapper).sprite;
            newWeaponBuilder.specs.icon = sprite;
        }

        ProjectileBehaviour newProjectile = Instantiate(fullFeatureWeapon.weapon.projectile);

        ParticleBehaviour launchParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.launchParticle);
        ParticleBehaviour bounceParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.bounceParticle);
        ParticleBehaviour hitParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.impactParticle);

        DontDestroyOnLoad(newProjectile.gameObject);
        DontDestroyOnLoad(launchParticlesOBJ.gameObject);
        DontDestroyOnLoad(bounceParticlesOBJ.gameObject);
        DontDestroyOnLoad(hitParticlesOBJ.gameObject);

        AssignProjectileConfig(newProjectile, creator.projectileObjConfig);

        if (creator.projectileObjConfig.trailMode == TrailMode.Internal)
        {
            ProjectileTrailBehaviour internalTrail = newProjectile.GetComponentInChildren<ProjectileTrailBehaviour>(true);
            ConfigureInternalTrail(internalTrail, creator.internalTrailParticles);
        }
        else
        {
            ParticleBehaviour externalTrailParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.projectile.externalTrailRef);
            DontDestroyOnLoad(externalTrailParticlesOBJ.gameObject);
            PopulateParticleGroup(externalTrailParticlesOBJ, creator.externalTrailParticles.particles);
            newWeaponBuilder.specs.projectile.externalTrailRef = externalTrailParticlesOBJ;
            externalTrailParticlesOBJ.Refresh();
        }

        PopulateParticleGroup(launchParticlesOBJ, creator.fireParticles.particles);
        PopulateParticleGroup(bounceParticlesOBJ, creator.bounceParticles.particles);
        PopulateParticleGroup(hitParticlesOBJ, creator.hitParticles.particles);

        launchParticlesOBJ.Refresh();
        bounceParticlesOBJ.Refresh();
        hitParticlesOBJ.Refresh();

        newWeaponBuilder.specs.projectile = newProjectile;
        newWeaponBuilder.specs.bounceParticle = bounceParticlesOBJ;
        newWeaponBuilder.specs.launchParticle = launchParticlesOBJ;
        newWeaponBuilder.specs.impactParticle = hitParticlesOBJ;

        return newWeaponBuilder;
    }

    void AssignProjectileConfig(ProjectileBehaviour projectile, in ProjectileObjConfig config)
    {
        projectile.transform.localScale = new Vector3(config.projectileStartSize.X, config.projectileStartSize.Y, 1f);
        Rigidbody2D projectileRB = projectile.GetComponent<Rigidbody2D>();
        Collider2D projectileCol = projectile.GetComponent<Collider2D>();
        projectileRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projectileRB.bodyType = RigidbodyType2D.Dynamic;
        if (config.dynamicBuiltinPhysics) projectileCol.isTrigger = false;
        else projectileCol.isTrigger = true;
    }

    void PopulateParticleGroup(ParticleBehaviour parent, ParticleObjConfig[] configs)
    {
        if (configs == null || configs.Length == 0) AddZeroEmitterAsParticleGroupChild(parent);
        else foreach (var config in configs) AddChildEmitterToParticleBehaviour(parent, config);
    }

    void AddZeroEmitterAsParticleGroupChild(ParticleBehaviour parent)
    {
        GameObject particleEmitter = Instantiate(defaultParticleEmitter);
        particleEmitter.name = "Particle Emitter";
        particleEmitter.transform.SetParent(parent.transform);
        particleEmitter.transform.position = Vector3.zero;
        particleEmitter.transform.rotation = Quaternion.identity;
    }

    void AddChildEmitterToParticleBehaviour(ParticleBehaviour parent, in ParticleObjConfig config)
    {
        GameObject particleEmitter = Instantiate(defaultParticleEmitter);
        particleEmitter.name = "Particle Emitter";
        particleEmitter.transform.SetParent(parent.transform);
        particleEmitter.transform.localPosition = new Vector3(config.localPosition.X, config.localPosition.Y, particleEmitter.transform.localPosition.z);
        particleEmitter.transform.localRotation = Quaternion.Euler(0f, 0f, config.localZRotation);
        ParticleSystem particleSystem = particleEmitter.GetComponent<ParticleSystem>();
        ParticleSystemSerializer.LoadFromFile(particleSystem, config.pathToModuleJson);
    }

    void ConfigureInternalTrail(ProjectileTrailBehaviour internalTrail, in ParticleObjConfig config)
    {
        ParticleSystemSerializer.LoadFromFile(internalTrail.GetComponent<ParticleSystem>(), config.pathToModuleJson);
        internalTrail.transform.localPosition = new Vector3(config.localPosition.X, config.localPosition.Y, internalTrail.transform.localPosition.z);
        internalTrail.transform.localRotation = Quaternion.Euler(0f, 0f, config.localZRotation);
    }

    public static Weapon FromProjectileParamConfigToWeaponData(in ProjectileParamConfig mod)
    {
        Weapon weapon = default;

        weapon.meleePosAnimation = AnimationCurveCreatorTest.ImportJson(mod.meleePosAnimationJsonPath);
        weapon.meleeRotAnimation = AnimationCurveCreatorTest.ImportJson(mod.meleeRotAnimationJsonPath);
        weapon.morphAnimation = AnimationCurveCreatorTest.ImportJson(mod.morphAnimationCurveJsonPath);

        weapon.reloadTime = mod.reloadTime;
        weapon.shootingInterval = mod.shootingInterval;
        weapon.lifeTime = mod.lifeTime;

        weapon.baseDamage = mod.baseDamage;
        weapon.aoeDamage = mod.aoeDamage;
        weapon.knockback = mod.knockback;
        weapon.aoe = mod.aoe;

        weapon.projectileSpeed = mod.projectileSpeed;
        weapon.projectileAcceleration = mod.projectileAcceleration;
        weapon.speedLimit = mod.speedLimit;
        weapon.minSpeed = mod.minSpeed;
        weapon.projectileAmmo = mod.projectileAmmo;
        weapon.burst = mod.burst;
        weapon.bounces = mod.bounces;

        weapon.holdable = mod.holdable;
        weapon.noGravity = mod.noGravity;
        weapon.dieOnImpact = mod.dieOnImpact;
        weapon.damageOnImpact = mod.damageOnImpact;
        weapon.sticky = mod.sticky;
        weapon.homing = mod.homing;
        weapon.melee = mod.melee;
        weapon.sync = mod.sync;
        weapon.hover = mod.hover;
        weapon.syncSpeed = mod.syncSpeed;

        //weapon.hitMarkSize = mod.hitmarkSize; Add later
        weapon.hitMarkSize = 1f;

        return weapon;
    }

}