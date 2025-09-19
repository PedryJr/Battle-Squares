using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.ParticleSystem;

public sealed class ProjectileTrailBehaviour : MonoBehaviour
{

    public Quaternion localRotation;
    public Vector3 localPosition;
    public Vector3 localScale;

    [SerializeField]
    bool enableCheckOnParent = false;

    [SerializeField]
    public bool allowDisableEnableFromRemote = false;

    [SerializeField]
    public Transform target;

    [SerializeField]
    ParticleSystem attatchedParticles;
    ProjectileBehaviour attatchedProjectile;

    TrailModule trails;

    float lifeTime;
    bool stateCheck;
    bool deadProjectile;

    private void Awake()
    {
        localRotation = transform.localRotation;
        localPosition = transform.localPosition;
        localScale = transform.localScale;
    }

    private void Start()
    {
        trails = attatchedParticles.trails;
        attatchedProjectile = GetComponentInParent<ProjectileBehaviour>();

        if (trails.enabled) trails.colorOverTrail = attatchedProjectile.owningPlayer.PlayerColor.ParticleColor;
        if (allowDisableEnableFromRemote) attatchedProjectile.loopreferencedTail = this;

        transform.SetParent(null, true);
        transform.position = target.position;
        attatchedParticles.Play();

    }

    private void LateUpdate()
    {

        deadProjectile = target == null;
        if (stateCheck != deadProjectile)
        {
            stateCheck = deadProjectile;
            attatchedParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        if (deadProjectile) DeathUpdate();
        else NormalUpdate();

    }

    void DeathUpdate()
    {

        if (attatchedParticles.particleCount <= 0) Destroy(gameObject);

    }

    void NormalUpdate()
    {
        if (trails.enabled) trails.colorOverTrail = attatchedProjectile.owningPlayer.PlayerColor.ParticleColor;
        bool targetActive = false;
        if (enableCheckOnParent) targetActive = target.parent.gameObject.activeSelf;
        else targetActive = target.gameObject.activeSelf;

        if (targetActive)
        {
            if (!attatchedParticles.isPlaying) attatchedParticles.Play();
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
        else
        {
            if (attatchedParticles.isPlaying)
            {
                attatchedParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else if (allowDisableEnableFromRemote && attatchedParticles.particleCount <= 0)
            {
                gameObject.SetActive(false);
            }
        }


    }

    public bool CanBeReused() => !attatchedParticles.isPlaying;
    public void ForceRelease() => target = null;

}
