using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class TestSpinner : MonoBehaviour
{
    [Tooltip("Number of full rotations per second")]
    public float spinsPerSecond = 3f;

    [Tooltip("Axis of rotation")]
    public Vector3 rotationAxis = Vector3.forward;
    [MethodImpl(512)]
    void Update()
    {
        float degreesPerSecond = spinsPerSecond * 360f;
        transform.Rotate(rotationAxis.normalized, degreesPerSecond * Time.deltaTime);
    }
}
