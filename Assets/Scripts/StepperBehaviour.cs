using System;
using UnityEngine;

public sealed class StepperBehaviour : MonoBehaviour
{

    public float rayDistance = 0.05f; // Distance of the raycast

    [SerializeField]
    public LayerMask hitMask; // LayerMask to filter which layers can be hit

    float timer;

    [SerializeField]
    GameObject particle;

    [SerializeField]
    Rigidbody2D rb;

    private void Update()
    {

        timer += Time.deltaTime * rb.linearVelocity.magnitude;

        if(timer > 3)
        {

            foreach(Vector3? point in ShootRays())
            {

                if(point != null)
                {

                    Instantiate(particle, point.Value, Quaternion.identity, null);
                    timer = 1;

                }

            }

        }

    }

    Vector3?[] ShootRays()
    {
        // Array to store hit positions
        Vector3?[] hitPositions = new Vector3?[4];

        // Define the four directions
        Vector3[] directions = 
            { 
            transform.up, 
            -transform.up,
            transform.right,
            -transform.right
        };

        // Cast rays in each direction
        for (int i = 0; i < directions.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position + (directions[i] * 0.508f), directions[i], rayDistance, hitMask);

            // Check if the ray hit something
            if (hit.collider != null)
            {
                hitPositions[i] = hit.point;
            }
            else
            {
                hitPositions[i] = null; // Use null for no hit
            }
        }

        return hitPositions;
    }

}
