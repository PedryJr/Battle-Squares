using UnityEngine;

public class ExternalTrailBehaviour : MonoBehaviour
{

    [SerializeField]
    ParticleSystem[] particles;

    [SerializeField]
    ParticleSystemRenderer[] renderers;
    bool activated;

    private void Awake()
    {
/*
        Play(Color.red);*/

    }

    public void Play(Color color)
    {

        Material material = Instantiate(renderers[0].material);
        material.color = color;

        for (int i = 0; i < renderers.Length; i++)
        {

            renderers[i].material = material;
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
            Destroy(gameObject);
        }

    }

}
