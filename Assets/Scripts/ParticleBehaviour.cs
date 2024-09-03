using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public sealed class ParticleBehaviour : MonoBehaviour
{

    [SerializeField]
    new ParticleSystem particleSystem;

    private void Update()
    {

        if (!particleSystem.isPlaying)
        {

            Destroy(gameObject);

        }

    }

}
