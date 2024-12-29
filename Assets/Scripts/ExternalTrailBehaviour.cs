using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public sealed class ExternalTrailBehaviour : MonoBehaviour
{

    [SerializeField]
    ParticleSystem[] particles;

    [SerializeField]
    ParticleSystemRenderer[] renderers;
    bool activated;

    static Dictionary<ulong, Material> externTrailMats = new Dictionary<ulong, Material>();

    private void Awake()
    {
/*
        Play(Color.red);*/

    }

    public void Play(Color color, ulong id)
    {

        for (int i = 0; i < renderers.Length; i++)
        {

            Material mat;

            if(externTrailMats.ContainsKey(id)) mat = externTrailMats[id];
            else
            {
                mat = Instantiate(renderers[0].material);
                externTrailMats.Add(id, mat);
            }

            mat.color = color;
            for (int j = 0; j < renderers[i].materials.Length; j++) Destroy(renderers[i].materials[j]);

            renderers[i].material = mat;
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
