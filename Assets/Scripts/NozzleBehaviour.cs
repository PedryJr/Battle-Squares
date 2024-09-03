using Unity.VisualScripting;
using UnityEngine;
using static ProjectileManager;
using static UnityEngine.ParticleSystem;

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

    float h, s, v;

    public void Awake()
    {

        if (primaryTimeSinceShot == 0) if (secondaryTimeSinceShot == 0) primaryTimeSinceShot = 0;
        
        originalScale = transform.localScale;
        shotScale = new Vector3 (transform.localScale.x/2, transform.localScale.y*1.5f, transform.localScale.z);
        spriteRenderer = GetComponent<SpriteRenderer>();



    }

    public NozzleBehaviour SetPlayerController(PlayerController playerController, PlayerBehaviour playerBehaviour)
    {

        projectileManager = GameObject.FindGameObjectWithTag("Sync").GetComponent<ProjectileManager>();
        this.playerBehaviour = playerBehaviour;
        this.playerController = playerController;

        UpdateWeaponTypes(ProjectileType.Sniper);
        UpdateWeaponTypes(ProjectileType.Revolver);

        return this;

    }

    private void Update()
    {

        intensity = Mathf.Clamp01(intensity - Time.deltaTime / 5f);

        Color.RGBToHSV(owningPlayerColor, out h, out s, out v);
        Color nozzleColor = Color.HSVToRGB(h, s * 0.9130434f, v * 0.6274509f);
        Color particleColor = nozzleColor;

        spriteRenderer.color = nozzleColor;

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

        /*relativePositionToPlayer = transform.position - playerBehaviour.transform.position;
        globalNozzleDirection = playerBehaviour.position + relativePositionToPlayer.normalized * 1.5f;*/
        relativePositionToPlayer = playerController.projectileDirection;
        globalNozzleDirection = playerBehaviour.transform.position + (Vector3)playerController.projectileDirection;

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
                    playerBehaviour.nozzlePosition - (relativePositionToPlayer / 4),
                    transform.rotation,
                    particleColor);
            }
        }
        if (playerController.shootSecondary && secondaryReady)
        {
            if (ShootWeapon(secondary))
            {
                playerBehaviour.AnimateNozzle(Vector3.zero, Vector3.zero);
                projectileManager.SpawnParticles(
                    playerBehaviour.nozzlePosition - (relativePositionToPlayer / 4),
                    transform.rotation,
                    particleColor);
            }
        }
    }

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

        if (fire) projectileManager.SpawnProjectile(
                        type,
            globalNozzleDirection,
            relativePositionToPlayer,
            playerBehaviour,
            owningPlayerColor);

        return fire;

    }

    public void UpdateWeaponTypes(ProjectileType newWeapon)
    {

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
