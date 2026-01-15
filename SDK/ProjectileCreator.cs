using System;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{

    public struct ProjectileCreator
    {
        internal ushort typeID;
        public ushort TypeID => typeID;

        public ProjectileMetaData metaData;
        public ProjectileObjConfig projectileObjConfig;
        public ProjectileParamConfig projectileParamConfig;

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
        public ParticleObjConfig[] particles;
    }

    public struct ParticleObjConfig
    {

        public string pathToModuleJson;
        public Vector2 localPosition;
        public float localZRotation;

    }

    public struct ProjectileParamConfig
    {
        public string morphAnimationCurveJsonPath;
        public string meleePosAnimationJsonPath;
        public string meleeRotAnimationJsonPath;

        public float reloadTime;
        public float shootingInterval;
        public float lifeTime;
        public float baseDamage;
        public float aoeDamage;
        public float knockback;
        public float aoe;
        public float projectileSpeed;
        public float projectileAcceleration;
        public float speedLimit;
        public float minSpeed;
        public float syncSpeed;

        public int projectileAmmo;
        public int burst;

        public byte bounces;
        public bool holdable;
        public bool noGravity;
        public bool dieOnImpact;
        public bool damageOnImpact;
        public bool sticky;
        public bool homing;
        public bool melee;
        public bool sync;
        public bool hover;
    }


    public enum TrailMode { Internal, External }
}