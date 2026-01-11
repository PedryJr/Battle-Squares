using BattleSquaresSDK;
using FMOD.Studio;
using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using static UnityVecToSystemVec;


public class UnityVecToSystemVec
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 cVec2(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 cVec2(Vector2 v) => new System.Numerics.Vector2(v.x, v.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 cVec3(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector3 cVec3(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 cVec4(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector4 cVec4(Vector4 v) => new System.Numerics.Vector4(v.x, v.y, v.z, v.w);

}

[Preserve]
public class ModLoader : MonoBehaviour
{

    private void Awake()
    {
        PhysicsBridge.Init();
        ModContext.projectileManager = GetComponent<ProjectileManager>();
        ModContext.playerSynchronizer = GetComponent<PlayerSynchronizer>();
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    private void Start() => UserMods.LoadMods(SaveManager.modsPath);
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private void Update() => UserMods.OnUpdate(Time.deltaTime);
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private void LateUpdate() => UserMods.OnLateUpdate(Time.deltaTime);
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private void FixedUpdate() => UserMods.OnFixedUpdate(Time.deltaTime);
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private void OnApplicationQuit() => UserMods.UnloadMods();
}

public class PhysicsBridge
{

    public static void Init()
    {
        PhysBridge.RaycastInternal = RaycastImpl;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HitInfo RaycastImpl(
        System.Numerics.Vector2 origin,
        System.Numerics.Vector2 direction,
        float distance,
        int layerMask)
    {
        RaycastHit2D hit = Physics2D.Raycast(cVec2(origin), cVec2(direction), distance, layerMask);
        return new HitInfo(hit.transform, cVec2(hit.point), cVec2(hit.normal), hit.distance);
    }

}

[Preserve]
public static class UserMods
{
    [Preserve]
    public static ModContext mc = new ModContext();
    [Preserve]
    private static readonly List<ModBase> _mods = new();
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void LoadMods(string folder)
    {

        

        Directory.CreateDirectory(folder);

        foreach (var path in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
        {

            byte[] bytes = File.ReadAllBytes(path);
            Assembly asm = Assembly.Load(bytes);

            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (!typeof(ModBase).IsAssignableFrom(type)) continue;

                try
                {

                    object obj = Activator.CreateInstance(type);
                    var mod = (ModBase)obj;
                    mod.OnLoad(mc);
                    _mods.Add(mod);

                }catch(Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        }
    }

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void OnUpdate(float dt)
    {
        foreach (var mod in _mods) mod.OnUpdate(dt);
    }

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void OnLateUpdate(float dt)
    {
        foreach (var mod in _mods) mod.OnLateUpdate(dt);
    }

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void OnFixedUpdate(float dt)
    {
        foreach (var mod in _mods) mod.OnFixedUpdate(dt);
    }

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void UnloadMods()
    {
        foreach (var mod in _mods) mod.OnUnload();
        _mods.Clear();
    }
}


[Preserve]
public class ModContext : IModContext
{

    internal static ProjectileManager projectileManager;
    internal static PlayerSynchronizer playerSynchronizer;

    [Preserve]
    private List<IModContext.ProjectileSpawnEvent> projectileSpawnEvents;
    [Preserve]
    private List<IModContext.ProjectileCreationEvent> projectileCreationEvents;

    [Preserve]
    ModLogger logger = new ModLogger();
    [Preserve]
    internal string pathToMods;
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public ModContext()
    {
        logger = new ModLogger();
        projectileSpawnEvents = new List<IModContext.ProjectileSpawnEvent>();
        pathToMods = SaveManager.modsPath;
    }

    [Preserve]
    public BattleSquaresSDK.ILogger Logger => logger;
    [Preserve]
    public string PathToMods => pathToMods; 

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SubscribeToProjectileSpawnEvent(IModContext.ProjectileSpawnEvent handler) => projectileSpawnEvents.Add(handler);

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void RaiseOnProjectileSpawnEvent(ProjectileBehaviour projectile, ref ProjectileInitData data)
    {
        ProjectileInitializationData creationData = ModProjectileConverter.Extract(data);
        ProjectileSpawnData projectileSpawnData = new ProjectileSpawnData()
        {
            creationData = creationData,
            handle = projectile
        };

        foreach (var handler in projectileSpawnEvents) handler(ref projectileSpawnData);
        ModProjectileConverter.Apply(ref data, projectileSpawnData.creationData);
    }

    public ISoundHandle PlayAudio(string path)
    {
        return ProgrammerAudio.Instance.PlayDialogue(path);
    }

    public void OnCreateProjectileAssets(IModContext.ProjectileCreationEvent handler)
    {
        ProjectileCreator newProjectileCreator = new ProjectileCreator();
        newProjectileCreator.typeID = projectileManager.GetNextAvailibleID();
        handler(ref newProjectileCreator);
        projectileManager.CreateWeaponFromMod(ref newProjectileCreator);
    }
}

[Preserve]
public class ModLogger : BattleSquaresSDK.ILogger
{
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void Log(string message)
    {
        Debug.Log(message);
    }
}

[Preserve]
public static class ModProjectileConverter
{
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static ProjectileInitializationData Extract(in ProjectileInitData src)
    {
        return new ProjectileInitializationData
        {

            spawnPosition = new System.Numerics.Vector2(src.position.x, src.position.y),
            spawnDirection = new System.Numerics.Vector2(src.direction.x, src.direction.y),

            Speed = src.speed,
            MinSpeed = src.minSpeed,
            MaxSpeed = src.speedLimit,
            Acceleration = src.acceleration,
            LifeTime = src.lifeTime,

            BaseDamage = src.baseDamage,
            DamageScaleOverTime = src.damageTimeScale,

            AreaDamage = src.aoeDamage,
            AreaRadius = src.aoe,

            Knockback = src.knockback,

            IsMelee = src.melee,
            MeleeRange = src.meleeRange,
            SwingAngle = src.swingDegrees,
            MeleeRotation = src.meleeRotation,

            Homing = src.homing,
            HomingStrength = src.homingStrength,
            HomingDistance = src.homingDistance,

            MaxBounces = src.bounces,
            BounceSpeedLoss = src.bounceSpeedLoss,
            BounceAngleTilt = src.bounceAngleTilt,

            Hover = src.hover,
            HoverDistance = src.hoverDistance,
            HoverStrength = src.hoverStrength,
            HoverRadius = src.hoverFloorRadius,
            HoverAttenuation = src.hoverDistanceAttenuation,
            TimeToFullHover = src.timeForFullHoverEffect,

            NoGravity = src.noGravity,
            DieOnImpact = src.dieOnImpact,
            DamageOnImpact = src.damageOnImpact,
            Sticky = src.sticky,
            OneHitOnly = src.oneTimeHit,
            Sync = src.sync,
            AlignDirection = src.alignDirection,
            RotateOnImpact = src.rotationFlipOnImpact,
            DieFromProjectiles = src.dieFromProjectiles,
            IgnoreProjectileBlocking = src.dontBlockProjectiles,
            BounceOffPlayers = src.bounceOfPlayers
        };
    }

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void Apply(ref ProjectileInitData dst, in ProjectileInitializationData src)
    {

        dst.position = new Vector2(src.spawnPosition.X, src.spawnPosition.Y);
        dst.direction = new Vector2(src.spawnDirection.X, src.spawnDirection.Y);

        dst.speed = src.Speed;
        dst.minSpeed = src.MinSpeed;
        dst.speedLimit = src.MaxSpeed;
        dst.acceleration = src.Acceleration;
        dst.lifeTime = src.LifeTime;

        dst.baseDamage = src.BaseDamage;
        dst.damageTimeScale = src.DamageScaleOverTime;

        dst.aoeDamage = src.AreaDamage;
        dst.aoe = src.AreaRadius;

        dst.knockback = src.Knockback;

        dst.melee = src.IsMelee;
        dst.meleeRange = src.MeleeRange;
        dst.swingDegrees = src.SwingAngle;
        dst.meleeRotation = src.MeleeRotation;

        dst.homing = src.Homing;
        dst.homingStrength = src.HomingStrength;
        dst.homingDistance = src.HomingDistance;

        dst.bounces = src.MaxBounces;
        dst.bounceSpeedLoss = src.BounceSpeedLoss;
        dst.bounceAngleTilt = src.BounceAngleTilt;

        dst.hover = src.Hover;
        dst.hoverDistance = src.HoverDistance;
        dst.hoverStrength = src.HoverStrength;
        dst.hoverFloorRadius = src.HoverRadius;
        dst.hoverDistanceAttenuation = src.HoverAttenuation;
        dst.timeForFullHoverEffect = src.TimeToFullHover;

        dst.noGravity = src.NoGravity;
        dst.dieOnImpact = src.DieOnImpact;
        dst.damageOnImpact = src.DamageOnImpact;
        dst.sticky = src.Sticky;
        dst.oneTimeHit = src.OneHitOnly;
        dst.sync = src.Sync;
        dst.alignDirection = src.AlignDirection;
        dst.rotationFlipOnImpact = src.RotateOnImpact;
        dst.dieFromProjectiles = src.DieFromProjectiles;
        dst.dontBlockProjectiles = src.IgnoreProjectileBlocking;
        dst.bounceOfPlayers = src.BounceOffPlayers;
    }
}
public static class AnimationCurveJsonUtility
{
    [System.Serializable]
    public class AnimationCurveFile
    {
        public CurveData curve;
    }

    [System.Serializable]
    public class CurveData
    {
        public string serializedVersion;
        public List<KeyframeData> m_Curve;
    }

    [System.Serializable]
    public class KeyframeData
    {
        public string serializedVersion;
        public float time;
        public float value;
        public float inSlope;
        public float outSlope;
        public int tangentMode;
        public int weightedMode;
        public float inWeight;
        public float outWeight;
    }

    public static AnimationCurve LoadCurveFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return LoadCurveFromJson(json);
        }
        catch(Exception e) { Debug.Log(e.Message); }
        
        AnimationCurve defaultCurve = new AnimationCurve();
        Keyframe[] keys = new Keyframe[2]
        {
            new Keyframe(0, 1),
            new Keyframe(1, 1),
        };
        defaultCurve.keys = keys;
        return defaultCurve;
    }

    public static AnimationCurve LoadCurveFromJson(string json)
    {
        AnimationCurveFile file = JsonConvert.DeserializeObject<AnimationCurveFile>(json);
        if (file?.curve?.m_Curve == null) return new AnimationCurve();

        AnimationCurve curve = new AnimationCurve();

        foreach (var k in file.curve.m_Curve)
        {
            Keyframe key = new Keyframe(
                k.time,
                k.value,
                k.inSlope,
                k.outSlope,
                k.inWeight,
                k.outWeight
            )
            {
                weightedMode = (WeightedMode)k.weightedMode
            };

            curve.AddKey(key);
        }

        return curve;
    }
}

public class SoundHandle : ISoundHandle
{
    private EventInstance _instance;
    private GCHandle _handle;
    private bool _stopped;

    internal SoundHandle(EventInstance instance, GCHandle handle)
    {
        _instance = instance;
        _handle = handle;
        _stopped = false;
    }

    public void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _instance.release();
        _handle.Free();
    }

    public void Pause()
    {
        if (_stopped) return;
        _instance.setPaused(true);
    }

    public void Resume()
    {
        if (_stopped) return;
        _instance.setPaused(false);
    }

    public void SetVolume(float volume)
    {
        if (_stopped) return;
        _instance.setVolume(Mathf.Clamp01(volume));
    }

    public bool IsPlaying()
    {
        if (_stopped) return false;
        _instance.getPlaybackState(out PLAYBACK_STATE state);
        return state == PLAYBACK_STATE.PLAYING;
    }
}
