using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.ParticleSystem;

public sealed class ProjectileTrailBehaviour : MonoBehaviour
{

    [SerializeField]
    Transform target;

    [SerializeField]
    ParticleSystem attatchedParticles;

    TrailModule trails;

    float lifeTime;
    bool stateCheck;
    bool deadProjectile;

    private void Start()
    {
        trails = attatchedParticles.trails;
        if(trails.enabled) trails.colorOverTrail = GetComponentInParent<ProjectileBehaviour>().owningPlayer.playerDarkerColor;
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

        transform.position = target.position;

    }

}
