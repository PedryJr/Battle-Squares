using BattleSquaresSDK;
using System;
using UnityEngine;

public class RuntimePrefabTemplates : MonoBehaviour
{

    //Referenced templates are disabled by default
    //They will implement checks to enable instantiated variants at runtime.
    [SerializeField] GameObject defaultParticleEmitter;
    [SerializeField] WeaponBuilder fullFeatureWeapon;

    public WeaponBuilder CreateNewWeaponPrefab(ref ProjectileCreator creator)
    {
        WeaponBuilder newWeaponBuilder = new WeaponBuilder();
        newWeaponBuilder.ASSIGN_ID(creator.typeID);

        ProjectileBehaviour newProjectile = Instantiate(fullFeatureWeapon.weapon.projectile);
        ParticleBehaviour externalTrailParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.projectile.externalTrailRef);
        ParticleBehaviour launchParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.launchParticle);
        ParticleBehaviour bounceParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.bounceParticle);
        ParticleBehaviour hitParticlesOBJ = Instantiate(fullFeatureWeapon.weapon.impactParticle);
        ProjectileTrailBehaviour internalTrail = newProjectile.GetComponentInChildren<ProjectileTrailBehaviour>(true);
        HitMarkBehaviour hitMarker = newProjectile.GetComponentInChildren<HitMarkBehaviour>(true);

        DontDestroyOnLoad(newProjectile.gameObject);
        DontDestroyOnLoad(externalTrailParticlesOBJ.gameObject);
        DontDestroyOnLoad(launchParticlesOBJ.gameObject);
        DontDestroyOnLoad(bounceParticlesOBJ.gameObject);
        DontDestroyOnLoad(hitParticlesOBJ.gameObject);

        ParticleObjConfig[] externalTrailParticles = creator.externalTrailParticles.particles;
        for (int i = 0; i < externalTrailParticles.Length; i++) AddChildEmitterToParticleBehaviour(externalTrailParticlesOBJ, externalTrailParticles[i]);
        ParticleObjConfig[] externalTrailParticles = creator.externalTrailParticles.particles;
        for (int i = 0; i < externalTrailParticles.Length; i++) AddChildEmitterToParticleBehaviour(externalTrailParticlesOBJ, externalTrailParticles[i]);

        newWeaponBuilder.specs.projectile = newProjectile;
        newWeaponBuilder.specs.projectile.externalTrailRef = externalTrailParticlesOBJ;
        newWeaponBuilder.specs.bounceParticle = bounceParticlesOBJ;

        return newWeaponBuilder;
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
        parent.RefreshComp();
    }

    void ConfigureInternalTrail(ProjectileTrailBehaviour internalTrail, in ParticleObjConfig config)
    {
        ParticleSystemSerializer.LoadFromFile(internalTrail.GetComponent<ParticleSystem>(), config.pathToModuleJson);
        internalTrail.transform.localPosition = new Vector3(config.localPosition.X, config.localPosition.Y, internalTrail.transform.localPosition.z);
        internalTrail.transform.localRotation = Quaternion.Euler(0f, 0f, config.localZRotation);
    }

}

/*public struct ProjectileCreator
{
    internal ushort typeID;
    public ushort TypeID => typeID;

    public ProjectileMetaData metaData;
    public ProjectileObjConfig projectileObjConfig;

    public ParticleObjConfig internalTrailParticles;
    public ParticleGroup externalTrailParticles;
    public ParticleGroup fireParticles;
    public ParticleGroup bounceParticles;
    public ParticleGroup hitParticles;
}

public struct ProjectileMetaData
{
    public string pathToLogoPNG;
    public string Name;
}

public struct ProjectileObjConfig
{
    public TrailMode trailMode;
    public bool dynamicBuiltinPhysics;

    public Vector2 projectileStartSize;
    public Vector2 hitmarkSize;

}

public struct ParticleGroup
{
    ParticleObjConfig[] particles;
}

public struct ParticleObjConfig
{

    string pathToModuleJson;
    Vector2 localPosition;
    float localZRotation;

}

public enum TrailMode { Internal, External }*/