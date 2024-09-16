using UnityEngine;

public sealed class ParticleBehaviour : MonoBehaviour
{

    float timer = 0;

    private void Update()
    {

        timer += Time.deltaTime;

        if (timer > 1.4f)
        {

            Destroy(gameObject);

        }

    }

}
