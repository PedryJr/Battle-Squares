using UnityEngine;

public class AnimatedSolid : MonoBehaviour
{

    [SerializeField]
    bool simulated;

    [SerializeField]
    float degreesPerSecond;

    Rigidbody2D rb;

    float timer;
    float rotation;

    [SerializeField]
    float rotationsPerSecond;

    MapSynchronizer mapSynchronizer;

    private void Awake()
    {
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        if(simulated) InitSimulated();
    }

    void InitSimulated()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if(!simulated)
        {
            rotation = mapSynchronizer.repeat30S * 360f;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
        }
    }

    private void FixedUpdate()
    {
        if(simulated) UpdateSimulated();
    }

    void UpdateSimulated()
    {
        rotation = mapSynchronizer.repeat30S * 360f;
        rb.rotation = rotation;
        rb.angularVelocity = degreesPerSecond;
    }

}
