using UnityEngine;

public class ImpactForceBehaviour : MonoBehaviour
{

    [SerializeField]
    float forceStrength;

    [SerializeField]
    float effectTime;

    [SerializeField]
    float sensorStrengthMultiplier = 1.0f;
    [SerializeField]
    float sensorRadiusMultiplier = 1.0f;

    float timer;
    float timerMul = 0f;

    [SerializeField]
    ProximityPixelSenssor sensorUp;
    [SerializeField]
    ProximityPixelSenssor sensorDown;

    private void Awake()
    {
        timerMul = 1f / effectTime;
    }

    private void Update()
    {
        timer = Mathf.Clamp01(timer + (Time.deltaTime * timerMul));
        float strengthResult = forceStrength * timer;

        float sensorStrength = strengthResult * sensorStrengthMultiplier;
        float sensorRadius = sensorStrength * sensorRadiusMultiplier;

        transform.localScale = Vector3.one * strengthResult;

        sensorUp.sensorData.positionWarpStrength = sensorStrength;
        sensorUp.sensorData.positionWarpRadius = sensorRadius;
        sensorDown.sensorData.positionWarpStrength = sensorStrength;
        sensorDown.sensorData.positionWarpRadius = sensorRadius;

        if (timer >= 1) Destroy(gameObject);
    }

}
