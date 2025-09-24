using UnityEngine;

public sealed class ExternalTrailBehaviour : MonoBehaviour
{

    [SerializeField]
    ParticleSystem[] particles;

    [SerializeField]
    ParticleSystemRenderer[] renderers;
    bool activated;

    public void Play(Color color, ulong id, PlayerBehaviour owningPlayer)
    {

        for (int i = 0; i < renderers.Length; i++)
        {

            owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(renderers[i], particles[i]);
            particles[i].Play();

        }

        activated = true;

    }

    private void Update()
    {

        bool shouldDestroy = true;

        if (activated)
        {

            foreach (var particle in particles)
            {

                if(particle.particleCount > 0) shouldDestroy = false;
                if(particle.isPlaying) shouldDestroy = false;

            }

        }
        else
        {
            shouldDestroy = false;
        }

        if (shouldDestroy)
        {

            for (int i = 0; i < renderers.Length; i++)
            {
                for (int j = 0; j < renderers[i].materials.Length; j++) Destroy(renderers[i].materials[j]);
            }
            Destroy(gameObject);
        }

    }

}
