using System;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{
    public interface IProjectileHandle
    {
        public IPlayerHandle Owner { get; }
        
        public uint NetworkID { get; }
        public ushort TypeID { get; }
        public bool IsLocal { get; }

        public void SetPosition(Vector2 position);
        public Vector2 GetPosition();

        public void SetVelocity(Vector2 position);
        public Vector2 GetVelocity();

        public void SetRotation(float rotation);
        public float GetRotation();

        public void SetAngularVelocity(float rotation);
        public float GetAngularVelocity();

        public event Action<IProjectileHandle> OnDestroyed;
    }

    public struct ProjectileSpawnData
    {
        public ProjectileInitializationData creationData;
        public IProjectileHandle handle;
    }

    public struct ProjectileInitializationData
    {

        public Vector2 spawnPosition;
        public Vector2 spawnDirection;

        public float Speed;
        public float MinSpeed;
        public float MaxSpeed;
        public float Acceleration;
        public float LifeTime;

        public float BaseDamage;
        public float DamageScaleOverTime;

        public float AreaDamage;
        public float AreaRadius;

        public float Knockback;

        public bool IsMelee;
        public float MeleeRange;
        public float SwingAngle;
        public float MeleeRotation;

        public bool Homing;
        public float HomingStrength;
        public float HomingDistance;

        public byte MaxBounces;
        public float BounceSpeedLoss;
        public float BounceAngleTilt;

        public bool Hover;
        public float HoverDistance;
        public float HoverStrength;
        public float HoverRadius;
        public float HoverAttenuation;
        public float TimeToFullHover;

        public bool NoGravity;
        public bool DieOnImpact;
        public bool DamageOnImpact;
        public bool Sticky;
        public bool OneHitOnly;
        public bool Sync;
        public bool AlignDirection;
        public bool RotateOnImpact;
        public bool DieFromProjectiles;
        public bool IgnoreProjectileBlocking;
        public bool BounceOffPlayers;
    }
}