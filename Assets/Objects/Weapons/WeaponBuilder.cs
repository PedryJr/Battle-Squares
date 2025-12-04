using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBuilder", menuName = "WeaponBuilder")]
public class WeaponBuilder : ScriptableObject
{


    private void OnValidate()
    {
        if (Application.isPlaying) return;
        specs.typeID = (ushort) UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue);
    }

    [SerializeField]
    Weapon specs;

    public string WeaponName => specs.weaponName;
    public int GetProjectileAmmo => specs.projectileAmmo;
    public float GetShootingInterval => specs.shootingInterval;
    public float GetReloadTime => specs.reloadTime;
    public bool GetHoldable => specs.holdable;
    public ushort typeID => specs.typeID;
    public Weapon weapon => GetWeapon();
    public ParticleBehaviour GetLaunchParticle => specs.launchParticle;
    public Sprite GetSprite => specs.icon;
    public Sprite GetIcon => specs.icon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MatchID(ushort otherID) => otherID == specs.typeID;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Weapon GetWeapon() => specs;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ASSIGN_ID(ushort newID) => specs.typeID = newID;

    [Serializable]
    public struct Weapon
    {
        public Sprite icon;
        public ProjectileBehaviour projectile;
        public ParticleBehaviour launchParticle;
        public ParticleBehaviour bounceParticle;
        public ParticleBehaviour impactParticle;
        public int projectileAmmo;
        public float reloadTime;
        public float shootingInterval;
        public float projectileSpeed;
        public float projectileAcceleration;
        public float lifeTime;
        public bool holdable;
        public int burst;
        public float burstSpread;
        public byte bounces;
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
        public ushort typeID;
        public bool clampMorph;
        public float bounceSpeedLoss;
        public float bounceAngleTilt;
        public float spawnOffsetPadding;
        public bool hover;
        public float hoverDistance;
        public float hoverStrength;
        public float hoverFloorRadius;
        public float hoverDistanceAttenuation;
        public float timeForFullHoverEffect;
        public ProjectileSpawnEvent[] projectileSpawnEvents;
        public string weaponName;
        public bool delistWeapon;
        public void SetTypeId(ref Weapon self, ushort typeId) => self.typeID = typeId;
    }

}
