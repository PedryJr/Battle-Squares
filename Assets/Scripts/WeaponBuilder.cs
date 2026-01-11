using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBuilder", menuName = "WeaponBuilder")]
public class WeaponBuilder : ScriptableObject
{


/*    private void OnValidate()
    {
        if (Application.isPlaying) return;
        specs.typeID = (ushort) UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue);
    }*/

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
    public ParticleBehaviour GetBounceParticle => specs.bounceParticle;
    public Sprite GetSprite => specs.icon;
    public Sprite GetIcon => specs.icon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MatchID(ushort otherID) => otherID == specs.typeID;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Weapon GetWeapon() => specs;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ASSIGN_ID(ushort newID) => specs.typeID = newID;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Weapon
    {
        public Sprite icon;
        public ProjectileBehaviour projectile;

        public ParticleBehaviour launchParticle;
        public ParticleBehaviour bounceParticle;
        public ParticleBehaviour impactParticle;

        public AnimationCurve morphAnimation;
        public AnimationCurve meleePosAnimation;
        public AnimationCurve meleeRotAnimation;

        public ProjectileSpawnEvent[] projectileSpawnEvents;
        public string weaponName;

        public Vector3 targetMorph;

        public float reloadTime;
        public float shootingInterval;

        public float projectileSpeed;
        public float projectileAcceleration;
        public float lifeTime;

        public float burstSpread;
        public float fluctuation;

        public float aoe;
        public float knockback;

        public float speedLimit;
        public float minSpeed;

        public float aoeDamage;
        public float baseDamage;
        public float damageTimeScale;

        public float recoil;

        public float timeToMorph;

        public float syncSpeed;

        public float meleeRange;
        public float swingDegrees;
        public float meleeRotation;

        public float homingStrength;
        public float homingDistance;
        public float spinSpeed;

        public float slowDownAmount;
        public float senderSpeedOnDeath;

        public float lingeringDamage;
        public float lingeringFrequency;

        public float bounceSpeedLoss;
        public float bounceAngleTilt;

        public float spawnOffsetPadding;

        public float hoverDistance;
        public float hoverStrength;
        public float hoverFloorRadius;
        public float hoverDistanceAttenuation;
        public float timeForFullHoverEffect;

        public float morphTimeOnBounce;

        // ?????????????????????????????
        // Integers
        // ?????????????????????????????
        public int projectileAmmo;
        public int burst;

        public ushort typeID;
        public byte bounces;

        // ?????????????????????????????
        // Booleans (packed)
        // ?????????????????????????????
        public bool holdable;

        public bool noGravity;
        public bool dieOnImpact;
        public bool damageOnImpact;
        public bool sticky;
        public bool skipAoeOnTargetHit;

        public bool enableMorph;
        public bool setMorphOnBounce;

        public bool sync;
        public bool stickToSender;

        public bool melee;
        public bool oneTimeHit;

        public bool flipFlop;

        public bool homing;

        public bool rotationFlipOnImpact;
        public bool dieFromProjectiles;
        public bool dontBlockProjectiles;
        public bool bounceOfPlayers;

        public bool alignDirection;
        public bool clampMorph;

        public bool hover;

        public bool delistWeapon;

        // ?????????????????????????????
        // Methods
        // ?????????????????????????????
        public void SetTypeId(ref Weapon self, ushort typeId) => self.typeID = typeId;
    }


}
