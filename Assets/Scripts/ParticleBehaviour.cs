using UnityEngine;

public sealed class ParticleBehaviour : MonoBehaviour
{

    float timer = 0;

    private void Update()
    {

        timer += Time.deltaTime;

        if (timer > 1.4f)
        {

            ParticleSystemRenderer[] particleSystems = GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (ParticleSystemRenderer particleSystem in particleSystems)
            {
                Material[] materials = particleSystem.materials;
                for (int i = 0; i < materials.Length; i++) Destroy(materials[i]);
            }

            

            Destroy(gameObject);

        }

    }

}
