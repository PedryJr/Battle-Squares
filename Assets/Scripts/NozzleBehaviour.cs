using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static WeaponBuilder;

[BurstCompile]
public sealed class NozzleBehaviour : MonoBehaviour
{


    PlayerController playerController;
    PlayerBehaviour playerBehaviour;
    ProjectileManager projectileManager;
    public SpriteRenderer spriteRenderer;

    public ushort primary = 0;
    public ushort secondary = 1;

    Vector2 relativePositionToPlayer = new();
    Vector2 globalNozzleDirection = new();

    public Color owningPlayerColor;
    public Color owningPlayerDarkerColor;

    public float intensity;

    int a = 82375459;
    int b = 89346787;
    int c = 89457937;

    int _primaryAmmo;
    int _secondaryAmmo;
    int _primaryShots;
    int _secondaryShots;

    public int primaryAmmo
    {
        get => _primaryAmmo ^ b ^ a ^ c;
        set => _primaryAmmo = value ^ a ^ c ^ b;
    }
    public int secondaryAmmo
    {
        get => _secondaryAmmo ^ b ^ c ^ a;
        set => _secondaryAmmo = value ^ b ^ c ^ a;
    }
    public int primaryShots
    {
        get => _primaryShots ^ b ^ c ^ a;
        set => _primaryShots = value ^ a ^ c ^ b;
    }
    public int secondaryShots
    {
        get => _secondaryShots ^ a ^ b ^ c;
        set => _secondaryShots = value ^ a ^ c ^ b;
    }

    public float primaryTimeSinceShot;
    public float primaryFireTime;
    public float secondaryTimeSinceShot;
    public float secondaryFireTime;

    public float primaryTimeSinceEmpty;
    public float primaryReloadTime;
    public float secondaryTimeSinceEmpty;
    public float secondaryReloadTime;

    public bool primaryHoldable = false;
    public bool secondaryHoldable = false;
    public bool flipFlop;

    public void Awake()
    {
        if (primaryTimeSinceShot == 0) if (secondaryTimeSinceShot == 0) primaryTimeSinceShot = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public NozzleBehaviour SetPlayerController(PlayerController playerController, PlayerBehaviour playerBehaviour)
    {

        projectileManager = GameObject.FindGameObjectWithTag("Sync").GetComponent<ProjectileManager>();

        primary = projectileManager.GetFirstWeaponTypeId();
        secondary = projectileManager.GetSecondWeaponTypeId();

        this.playerBehaviour = playerBehaviour;
        this.playerController = playerController;

        intensity = 0;
        primaryAmmo = 0;
        secondaryAmmo = 0;
        primaryShots = 0;
        secondaryShots = 0;
        primaryTimeSinceShot = 0;
        primaryFireTime = 0;
        secondaryTimeSinceShot = 0;
        secondaryFireTime = 0;
        primaryTimeSinceEmpty = 0;
        primaryReloadTime = 0;
        secondaryTimeSinceEmpty = 0;
        secondaryReloadTime = 0;
        primaryHoldable = false;
        secondaryHoldable = false;

        Weapon primaryWeapon = projectileManager.GetRawWeaponByTypeID(primary);
        Weapon secondaryWeapon = projectileManager.GetRawWeaponByTypeID(secondary);

        primaryAmmo = primaryWeapon.projectileAmmo;
        primaryFireTime = primaryWeapon.shootingInterval;
        primaryReloadTime = primaryWeapon.reloadTime;
        primaryHoldable = primaryWeapon.holdable;

        secondaryAmmo = secondaryWeapon.projectileAmmo;
        secondaryFireTime = secondaryWeapon.shootingInterval;
        secondaryReloadTime = secondaryWeapon.reloadTime;
        secondaryHoldable = secondaryWeapon.holdable;

        return this;

    }

    private void FixedUpdate()
    {

        intensity = math.clamp(intensity - Time.deltaTime / 5f, 0, 1);

        if (playerBehaviour == null) return;
        if (playerController == null) return;
        if (projectileManager == null) return;

        primaryTimeSinceShot += Time.deltaTime;
        primaryTimeSinceEmpty += Time.deltaTime;

        secondaryTimeSinceShot += Time.deltaTime;
        secondaryTimeSinceEmpty += Time.deltaTime;

        if (primaryTimeSinceShot >= primaryReloadTime)
        {
            primaryShots = 0;
            primaryTimeSinceEmpty = primaryReloadTime;
        }

        if (secondaryTimeSinceShot >= secondaryReloadTime)
        {
            secondaryShots = 0;
            secondaryTimeSinceEmpty = secondaryReloadTime;
        }

        if (!playerController.shootPrimary && !playerController.shootSecondary) return;
        
        relativePositionToPlayer = playerBehaviour.toPos;
        globalNozzleDirection = playerBehaviour.rb.position + playerBehaviour.toPos;

        bool primaryReady, secondaryReady;

        if (primaryTimeSinceShot >= primaryFireTime && primaryTimeSinceEmpty >= primaryReloadTime) primaryReady = true;
        else primaryReady = false;

        if (secondaryTimeSinceShot >= secondaryFireTime && secondaryTimeSinceEmpty >= secondaryReloadTime) secondaryReady = true;
        else secondaryReady = false;

        if (playerController.shootPrimary && primaryReady)
        {
            if (ShootWeapon(primary))
            {
                projectileManager.SpawnNozzleParticles(
                    GetParticlePoint(),
                    GetFireRot(),
                    primary,
                    playerBehaviour.GetGameID());
            }
        }
        if (playerController.shootSecondary && secondaryReady)
        {
            if (ShootWeapon(secondary))
            {
                projectileManager.SpawnNozzleParticles(
                    GetParticlePoint(),
                    GetFireRot(),
                    secondary,
                    playerBehaviour.GetGameID());
            }
        }
    }

    bool ShootWeapon(ushort type)
    {

        intensity += 0.2f;

        bool fire = false;

        if (type == primary) 
        {

            if (primaryShots == primaryAmmo)
            {

                primaryTimeSinceEmpty = 0;

            }
            else
            {

                primaryTimeSinceShot = 0;
                primaryShots++;
                playerController.shootPrimary = primaryHoldable;
                fire = true;

            }

        }
        if (type == secondary)
        {

            if (secondaryShots == secondaryAmmo)
            {

                secondaryTimeSinceEmpty = 0;

            }
            else
            {

                secondaryTimeSinceShot = 0;
                secondaryShots++;
                playerController.shootSecondary = secondaryHoldable;
                fire = true;

            }
        }
        if (fire)
        {

            projectileManager.SpawnProjectile(
                type,
                GetFirePoint(),
                playerBehaviour.aimDirection,
                playerBehaviour);
            playerBehaviour.PlayNozzleRecoilAnimation();
        }
        return fire;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetParticlePoint() => (Vector2)playerBehaviour.transform.position + (playerBehaviour.aimDirection / 1.8f);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetFirePoint() => (Vector2)transform.position - (playerBehaviour.aimDirection / 2f);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion GetFireRot()
    {
        Vector2 aimDirection = playerBehaviour.aimDirection;
        return Quaternion.Euler(0f, 0f, math.degrees(math.atan2(aimDirection.y, aimDirection.x)));
    }

    public void UpdateWeaponTypes(ushort newWeapon)
    {
        intensity = 0;
        primaryAmmo = 0;
        secondaryAmmo = 0;
        primaryShots = 0;
        secondaryShots = 0;
        primaryTimeSinceShot = 0;
        primaryFireTime = 0;
        secondaryTimeSinceShot = 0;
        secondaryFireTime = 0;
        primaryTimeSinceEmpty = 0;
        primaryReloadTime = 0;
        secondaryTimeSinceEmpty = 0;
        secondaryReloadTime = 0;
        primaryHoldable = false;
        secondaryHoldable = false;

        if (newWeapon == primary) return;

        secondary = primary;
        primary = newWeapon;

        Weapon primaryWeapon = projectileManager.GetRawWeaponByTypeID(primary);
        Weapon secondaryWeapon = projectileManager.GetRawWeaponByTypeID(secondary);

        primaryAmmo = primaryWeapon.projectileAmmo;
        primaryFireTime = primaryWeapon.shootingInterval;
        primaryReloadTime = primaryWeapon.reloadTime;
        primaryHoldable = primaryWeapon.holdable;

        secondaryAmmo = secondaryWeapon.projectileAmmo;
        secondaryFireTime = secondaryWeapon.shootingInterval;
        secondaryReloadTime = secondaryWeapon.reloadTime;
        secondaryHoldable = secondaryWeapon.holdable;
    }

}
