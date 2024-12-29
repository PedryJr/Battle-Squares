using UnityEngine;

public sealed class SolidRotatorBehaviour : MonoBehaviour
{

    Rigidbody2D rb;

    MapSynchronizer mapSynchronizer;

    [SerializeField]
    float rotationsPerSecond;

    float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
    }

    private void LateUpdate()
    {
        
        timer = Mathf.Repeat(mapSynchronizer.localTime * rotationsPerSecond, 1);

        rb.rotation = timer * 360;
        rb.angularVelocity = rotationsPerSecond * 360;

    }

}
