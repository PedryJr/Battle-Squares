using BattleSquaresSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Scripting;
using static UnityVecToSystemVec;
using SQuat = System.Numerics.Quaternion;
using SVec2 = System.Numerics.Vector2;
using SVec3 = System.Numerics.Vector3;
using SVec4 = System.Numerics.Vector4;
using UQuat = UnityEngine.Quaternion;
using UVec2 = UnityEngine.Vector2;
using UVec3 = UnityEngine.Vector3;
using UVec4 = UnityEngine.Vector4;


public class UnityVecToSystemVec
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UVec2 cVec2(SVec2 v) => new UVec2(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SVec2 cVec2(UVec2 v) => new SVec2(v.x, v.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UVec3 cVec3(SVec3 v) => new UVec3(v.X, v.Y, v.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SVec3 cVec3(UVec3 v) => new SVec3(v.x, v.y, v.z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UVec4 cVec4(SVec4 v) => new UVec4(v.X, v.Y, v.Z, v.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SVec4 cVec4(UVec4 v) => new SVec4(v.x, v.y, v.z, v.w);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UQuat cQuat(SQuat q) => new UQuat(q.X, q.Y, q.Z, q.W);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SQuat cQuat(UQuat q) => new SQuat(q.x, q.y, q.z, q.w);
}

[Preserve]
public class ModLoader : MonoBehaviour
{

    private void Awake()
    {
        PhysicsBridge.Init();
        GameSideBridge.InitializeBridge();
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

    public static void Init() => PhysBridge.RaycastInternal = RaycastImpl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HitInfo RaycastImpl(
        SVec2 origin,
        SVec2 direction,
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

    public struct ModHook
    {
        public ModContext context;
        public ModBase mod;
        public string dllPath;
        public string directoryPath;
    }

    public static ModHook[] hooks;

    public static void LoadMods(string folder)
    {
        Directory.CreateDirectory(folder);

#if UNITY_EDITOR
        // In editor: Navigate from Assets folder up to project root, then into build directory
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string testingModPath = Path.Combine(projectRoot, "build", "Build_Raw_Mono", "Battle Squares_Data", "Mod");
#else
    string testingModPath = Path.Combine(Application.dataPath, "Mod");
#endif
        Directory.CreateDirectory(testingModPath);

        string internalModPath = testingModPath;

        List<string> allModPaths = new List<string>();

        if (Directory.Exists(internalModPath))
        {
            allModPaths.AddRange(Directory.GetFiles(internalModPath, "*.dll", SearchOption.AllDirectories));
        }

        allModPaths.AddRange(Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories));

        List<ModHook> loadedMods = new List<ModHook>();
        HashSet<string> loadedFileHashes = new HashSet<string>();

        for (int i = 0; i < allModPaths.Count; i++)
        {
            string path = allModPaths[i];

            string fileHash = ComputeFileHash(path);

            if (loadedFileHashes.Contains(fileHash)) continue;

            byte[] bytes = File.ReadAllBytes(path);
            Assembly asm = Assembly.Load(bytes);
            string modDirectory = Path.GetDirectoryName(path)!;
            bool modLoadedFromAssembly = false;

            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (!typeof(ModBase).IsAssignableFrom(type)) continue;

                try
                {
                    ModContext context = new ModContext(modDirectory);
                    var mod = (ModBase)Activator.CreateInstance(type)!;
                    mod.OnLoad(context);

                    loadedMods.Add(new ModHook()
                    {
                        mod = mod,
                        context = context,
                        dllPath = path,
                        directoryPath = modDirectory,
                    });

                    modLoadedFromAssembly = true;
                    if(context.Logger.enable) VLog.Log($"§mLoaded mod: {type.Name} from {path}", 5);
                }
                catch (Exception e)
                {
                    VLog.Log($"§4Failed loading mod from §6{path}\n§m{e}", 5);
                }
            }

            if (modLoadedFromAssembly) loadedFileHashes.Add(fileHash);
        }

        hooks = loadedMods.ToArray();
        Debug.Log($"Total mods loaded: {hooks.Length}");
    }

    private static string ComputeFileHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            byte[] hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnUpdate(float dt)
    {
        for (int i = 0; i < hooks.Length; i++) hooks[i].mod.OnUpdate(dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnLateUpdate(float dt)
    {
        for (int i = 0; i < hooks.Length; i++) hooks[i].mod.OnLateUpdate(dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnFixedUpdate(float dt)
    {
        for (int i = 0; i < hooks.Length; i++) hooks[i].mod.OnFixedUpdate(dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnloadMods()
    {
        for (int i = 0; i < hooks.Length; i++) hooks[i].mod.OnUnload();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RaiseOnProjectileSpawnEvent(ProjectileBehaviour projectile, ref ProjectileInitData data)
    {
        for (int i = 0; i < hooks.Length; i++) hooks[i].context.RaiseOnProjectileSpawnEvent(projectile, ref data);
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
    ModLogger logger;

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public ModContext(string pathToMod)
    {
        logger = new ModLogger();
        projectileSpawnEvents = new List<IModContext.ProjectileSpawnEvent>();
        ModRoot = pathToMod;
    }

    [Preserve]
    public BattleSquaresSDK.ILogger Logger => logger;

    readonly string ModRoot;
    public string GetPathToRelative(string relativePath) => Path.Combine(ModRoot, relativePath);
    public string GetPathToRoot() => ModRoot;

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

        foreach (var handler in projectileSpawnEvents) 
        {
            try
            {
                handler(ref projectileSpawnData);
            }
            catch (Exception error)
            {
                Debug.Log(error.Message);
            }
        }

        ModProjectileConverter.Apply(ref data, projectileSpawnData.creationData);
    }

    public void PlayAudio(string path) => ProgrammerAudio.Instance.PlayDialogue(path);

    public void OnCreateProjectileAssets(IModContext.ProjectileCreationEvent handler)
    {
        ProjectileCreator newProjectileCreator = new ProjectileCreator();
        newProjectileCreator.typeID = projectileManager.GetNextAvailibleID();
        SetupMinimalWorkingDefaults(ref newProjectileCreator);
        handler(ref newProjectileCreator);
        projectileManager.CreateWeaponFromMod(ref newProjectileCreator);
    }

    private void SetupMinimalWorkingDefaults(ref ProjectileCreator pc)
    {
        ref ProjectileParamConfig cfg = ref pc.projectileParamConfig;

        cfg.projectileSpeed = 20f;
        cfg.projectileAcceleration = 0f;
        cfg.speedLimit = 50f;
        cfg.lifeTime = 5f;

        cfg.baseDamage = 1f;

        cfg.projectileAmmo = 1;
        cfg.reloadTime = 0.5f;
        cfg.shootingInterval = 0.1f;
        cfg.sync = true;
        cfg.syncSpeed = 10f;
    }
}

[Preserve]
public class ModLogger : BattleSquaresSDK.ILogger
{
    public bool enable { get; set; }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void Log(string message)
    {
        if(enable) VLog.Log(message);
    }
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void Log(string message, float duration)
    {
        if(enable) VLog.Log(message, duration);
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

            spawnPosition = new SVec2(src.position.x, src.position.y),
            spawnDirection = new SVec2(src.direction.x, src.direction.y),

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

        dst.position = new UVec2(src.spawnPosition.X, src.spawnPosition.Y);
        dst.direction = new UVec2(src.spawnDirection.X, src.spawnDirection.Y);

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
    [Serializable]
    public class AnimationCurveFile
    {
        public CurveData curve;
    }

    [Serializable]
    public class CurveData
    {
        public string serializedVersion;
        public List<KeyframeData> m_Curve;
    }

    [Serializable]
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
}
