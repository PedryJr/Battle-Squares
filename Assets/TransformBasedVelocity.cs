using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TransformBasedVelocity : MonoBehaviour
{
    Rigidbody2D rb;

    Vector2 prevPos;
    float prevRot;

    float deltaTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        prevPos = rb.position;
        prevRot = rb.rotation;
    }

    private void FixedUpdate()
    {/*
        EstimateVelocity();*/
    }

    private void EstimateVelocity()
    {
        deltaTime = Time.fixedDeltaTime;

        Vector2 currentPosition = rb.position;
        Vector2 velocity = (currentPosition - prevPos) / deltaTime;

        float currentRotation = rb.rotation;
        float angularVelocity = (currentRotation - prevRot) / deltaTime;

        rb.linearVelocity = velocity;

        rb.angularVelocity = angularVelocity;

        prevPos = currentPosition;
        prevRot = currentRotation;
    }
}
