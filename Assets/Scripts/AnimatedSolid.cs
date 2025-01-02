using UnityEngine;

public sealed class AnimatedSolid : MonoBehaviour
{
    [SerializeField]
    private bool simulated;

    [SerializeField]
    private float degreesPerSecond;

    private Rigidbody2D rb;

    private float timer;
    private float rotation;

    [SerializeField]
    private float rotationsPerSecond;

    private MapSynchronizer mapSynchronizer;

    private void Awake()
    {

        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        if (simulated) InitSimulated();
    }

    private void InitSimulated()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!simulated)
        {
            rotation = mapSynchronizer.repeat30S * 360f;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
        }
    }

    private void FixedUpdate()
    {
        if (simulated) UpdateSimulated();
    }

    private void UpdateSimulated()
    {
        rotation = mapSynchronizer.repeat30S * 360f;
        rb.rotation = rotation;
        rb.angularVelocity = degreesPerSecond;
    }
}
