using Unity.VisualScripting;
using UnityEngine;

public class ProjectileTrailBehaviour : MonoBehaviour
{

    [SerializeField]
    Transform target;

    [SerializeField]
    ParticleSystem attatchedParticles;

    float lifeTime;
    bool stateCheck;
    bool deadProjectile;

    private void Start()
    {
        transform.SetParent(null, true);
        transform.position = target.position;
        attatchedParticles.Play();
    }

    private void LateUpdate()
    {

        deadProjectile = (target.IsDestroyed() || target == null);
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
