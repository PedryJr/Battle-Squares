using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static ProjectileManager;

[BurstCompile]
public sealed class NozzleBehaviour : MonoBehaviour
{

    PlayerController playerController;
    PlayerBehaviour playerBehaviour;
    ProjectileManager projectileManager;
    public SpriteRenderer spriteRenderer;

    public ProjectileType primary = ProjectileType.Revolver;
    public ProjectileType secondary = ProjectileType.Revolver;

    Vector2 relativePositionToPlayer = new ();
    Vector2 globalNozzleDirection = new();

    Vector3 originalScale;
    Vector3 shotScale;

    public Color owningPlayerColor;
    public Color owningPlayerDarkerColor;

    public float intensity;

    public int primaryAmmo;
    public int secondaryAmmo;
    public int primaryShots;
    public int secondaryShots;

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

    float h, s, v;

    [BurstCompile]
    public void Awake()
    {

        if (primaryTimeSinceShot == 0) if (secondaryTimeSinceShot == 0) primaryTimeSinceShot = 0;
        
        originalScale = transform.localScale;
        shotScale = new Vector3 (transform.localScale.x/2, transform.localScale.y*1.5f, transform.localScale.z);
        spriteRenderer = GetComponent<SpriteRenderer>();



    }
    [BurstCompile]
    public NozzleBehaviour SetPlayerController(PlayerController playerController, PlayerBehaviour playerBehaviour)
    {

        projectileManager = GameObject.FindGameObjectWithTag("Sync").GetComponent<ProjectileManager>();
        this.playerBehaviour = playerBehaviour;
        this.playerController = playerController;

        UpdateWeaponTypes(ProjectileType.Sniper);
        UpdateWeaponTypes(ProjectileType.Revolver);

        return this;

    }
    [BurstCompile]
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
                playerBehaviour.AnimateNozzle(Vector3.zero, Vector3.zero);
                projectileManager.SpawnParticles(
                    globalNozzleDirection - (relativePositionToPlayer / 3.5f),
                    Quaternion.Euler(0, 0, 
                    math.degrees(math.atan2(relativePositionToPlayer.y, relativePositionToPlayer.x))),
                    primary);
            }
        }
        if (playerController.shootSecondary && secondaryReady)
        {
            if (ShootWeapon(secondary))
            {
                playerBehaviour.AnimateNozzle(Vector3.zero, Vector3.zero);
                projectileManager.SpawnParticles(
                    globalNozzleDirection - (relativePositionToPlayer / 3.5f),
                    Quaternion.Euler(0, 0, math.degrees(math.atan2(relativePositionToPlayer.y, relativePositionToPlayer.x))),
                    secondary);
            }
        }
    }
    [BurstCompile]
    bool ShootWeapon(ProjectileType type)
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
                globalNozzleDirection,
                relativePositionToPlayer.normalized,
                playerBehaviour);
            playerBehaviour.ApplyRecoil();
        }
        return fire;

    }
    [BurstCompile]
    public void UpdateWeaponTypes(ProjectileType newWeapon)
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

        foreach (Weapon weapon in projectileManager.weapons)
        {

            if(weapon.type == primary)
            {

                primaryAmmo = weapon.projectileAmmo;
                primaryFireTime = weapon.shootingInterval;
                primaryReloadTime = weapon.reloadTime;
                primaryHoldable = weapon.holdable;

            }

            if(weapon.type == secondary)
            {

                secondaryAmmo = weapon.projectileAmmo;
                secondaryFireTime = weapon.shootingInterval;
                secondaryReloadTime = weapon.reloadTime;
                secondaryHoldable = weapon.holdable;

            }

        }

    }

}
