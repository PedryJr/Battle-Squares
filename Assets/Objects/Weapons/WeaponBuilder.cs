using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static PlayerSynchronizer;
using static ProjectileManager;
using Random = System.Random;

[CreateAssetMenu(fileName = "WeaponBuilder", menuName = "WeaponBuilder")]
public class WeaponBuilder : ScriptableObject
{


    [SerializeField]
    Weapon specs;

    public int GetProjectileAmmo => specs.projectileAmmo;
    public float GetShootingInterval => specs.shootingInterval;
    public float GetReloadTime => specs.reloadTime;
    public bool GetHoldable => specs.holdable;
    public ushort typeID => specs.typeID;
    public Weapon weapon => GetWeapon();
    public ParticleBehaviour GetLaunchParticle => specs.launchParticle;
    public Sprite GetSprite => specs.icon;
    public Sprite GetIcon => specs.icon;

    public bool MatchID(ushort otherID) => otherID == specs.typeID;
    public Weapon GetWeapon() => specs;
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
    }

}
