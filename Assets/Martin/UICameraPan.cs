using UnityEngine;
using UnityEngine.InputSystem;

public class UICameraPan : MonoBehaviour
{
    #region Serialized Fields
    // Camera panning settings
    [Header("Camera Settings")]
    [Tooltip("Use camera panning")]
    [SerializeField] bool useCameraPanning = true;
    [Tooltip("The affected camera.")]
    [SerializeField] Camera targetCamera;
    [Space]
    [Tooltip("Overall multiplier for how much the camera pans.")]
    [SerializeField] float panStrength = 0.001f;

    // UI panning settings
    [Header("UI Panning Settings")]
    [Tooltip("Use UI panning")]
    [SerializeField] bool useUIPanning = true;
    [Tooltip("The parent object that will be panned along with the camera. This should be the parent of all UI elements you want to pan, and needs a Rectangle Transform component.")]
    [SerializeField] GameObject UIPanObject; // All UI that you want to pan should be parented to this object, and it will move along with the pan target.
    [Tooltip("UI Panning Strength. Higher values = more movement. This is separate from camera pan strength to allow for different parallax effects.")]
    [Space]
    [SerializeField] float UIPanStrength = 0.1f;

    // UI Rotation settings
    [Header("UI Rotation Settings")]
    [Tooltip("Rotate the UI based on pan target.")]
    [SerializeField] bool useUIRotating = false;
    [Tooltip("UI Rotation Strength. Higher values = more rotation.")]
    [SerializeField] float UIRotationStrength = 0.1f;

    // Pan Target settings
    [Header("Pan Target Settings")]
    [Tooltip("Controls pan speed based on distance between cursor and current pan target position.")]
    [SerializeField] AnimationCurve speedCurve;
    [Tooltip("Minimum distance to consider the pan target reached. \n\n The 'Cyan Crosshair' in debug shows this radius.")]
    [SerializeField] float panPrecision = 1f;
    [Tooltip("Movement speed multiplier for the internal pan target. \n\n Make sure not to go beyond about 10000 because you will start seeing floating point inaccuracy bs.")]
    [SerializeField] float panTargetSpeedMultiplier = 2500f;
    [Space]
    [Tooltip("There is a distance beyond which the move speed of the Pan Target is maxed out. This variable adjust what percentage of the diagonal of the screen is used as that distance. \n\n Ergo, beyond this distance the pan target will move at max speed and be clamped to the far right of the Speed Curve.")]
    [Range(0.01f, 200f)]
    [SerializeField] float distanceToMaxSpeed = 100f;
    [Tooltip("Minimum movement speed percentage (1 = 1%) to prevent the target from getting stuck. \n\n Example: At 1% the pan target moves at 1% max speed at it's slowest.")]
    [Range(0.01f, 100f)]
    [SerializeField] float baseSpeedPercentage = 1f;

    // Debug settings
    [Header("Debug Visualization")]
    [Tooltip("Enable basic console logging for distance and speed.")]
    [SerializeField] bool showDebugLogs = false;
    [Tooltip("Draw visual lines in the Scene View.")]
    [SerializeField] bool showDebugLines = false;
    [Tooltip("Scale factor for the yellow velocity ray. Higher = longer visual line.")]
    [SerializeField] float velocityRayScale = 1.0f;
    [Tooltip("Color of the debug line when at minimum speed.")]
    [SerializeField] Color slowColor = Color.green;
    [Tooltip("Color of the debug line when at maximum speed.")]
    [SerializeField] Color fastColor = Color.red;
    #endregion

    private Vector3 defaultCameraPosition = new Vector3(0, 0, -10); // The default position of the camera to pan around
    private Vector2 trueTargetPosition; // The target position for the Pan Target, chosen between the cursor or highlighted UI element
    private Vector2 panTargetPosition; // Position of the Pan Target
    private float panTargetToTrueTargetDistance;
    private float panTargetSpeed;
    private float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);

    private Vector2 GetTargetPosition()
    {
        // Implement controller / button highlighting bullshit later
        return Mouse.current.position.value;
    }
    private float GetPanToTargetDistance()
    {
        return Vector2.Distance(panTargetPosition, GetTargetPosition());
    }
    private float GetPanToTargetDistanceNormalized()
    {
        float maxSpeedDistance = screenDiagonal * (distanceToMaxSpeed / 100f);
        return Mathf.Clamp01(GetPanToTargetDistance() / maxSpeedDistance);
    }
    private Vector2 GetPanDirection()
    {
        return (GetTargetPosition() - panTargetPosition).normalized;
    }
    private void ApplyPanMovement()
    {
        
        panTargetSpeed = (speedCurve.Evaluate(GetPanToTargetDistanceNormalized()) + baseSpeedPercentage / 100) * panTargetSpeedMultiplier * Time.deltaTime;
        panTargetPosition += GetPanDirection() * panTargetSpeed;
    }
    private float CentreToPanDistance()
    {
        return Vector2.Distance(new Vector2(Screen.width / 2, Screen.height / 2), panTargetPosition);
    }
    private Vector2 CentreToPanVector()
    {
        return panTargetPosition - (new Vector2(Screen.width / 2, Screen.height / 2));
    }
    private Vector2 CentreToPanVectorNormalized()
    {
        return CentreToPanVector().normalized;
    }
    private void SetCameraPosition()
    {
        targetCamera.transform.position = defaultCameraPosition + new Vector3(CentreToPanDistance() * CentreToPanVectorNormalized().x * panStrength, CentreToPanDistance() * CentreToPanVectorNormalized().y * panStrength, 0);
    }
    private void ApplyUIPanning()
    {
        if (UIPanObject != null)
        {
            // Move the UI Pan Object in the opposite direction of the pan target to create a parallax effect
            UIPanObject.transform.localPosition = -new Vector3(CentreToPanVector().x * UIPanStrength, CentreToPanVector().y * UIPanStrength, 0);
        }
    }
    private void ApplyUIRotating()
    {
        if (UIPanObject != null)
        {
            // Rotate the UI Pan Object based on the pan target's position relative to the center of the screen
            float rotationX = CentreToPanVector().y * UIRotationStrength; // Rotate around X based on vertical distance
            float rotationY = -CentreToPanVector().x * UIRotationStrength; // Rotate around Y based on horizontal distance
            UIPanObject.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
    }
    private void ShowDebugObjects()
    {
        if (showDebugLines)
        {
            Vector2 cursorReached = GetTargetPosition();
            Vector2 direction = GetPanDirection();

            // Normalized speed based on the multiplier for color lerping
            float normalizedSpeed = Mathf.Clamp01(panTargetSpeed / (panTargetSpeedMultiplier * Time.deltaTime + 0.01f));
            Color speedColor = Color.Lerp(slowColor, fastColor, normalizedSpeed);

            // 1. Connection Line (Variable Color)
            Debug.DrawLine(panTargetPosition, cursorReached, speedColor);

            // 2. Scaled Velocity Ray (Yellow)
            // Scaling the ray by the new velocityRayScale for better visibility at high speeds
            float rayLength = panTargetSpeed * velocityRayScale;
            Debug.DrawRay(panTargetPosition, direction * rayLength, Color.yellow);

            // 3. Precision Radius (Cyan)
            Debug.DrawRay(cursorReached, Vector2.up * panPrecision, Color.cyan);
            Debug.DrawRay(cursorReached, Vector2.right * panPrecision, Color.cyan);
        }

        if (showDebugLogs)
        {
            Debug.Log($"Speed: {panTargetSpeed:F3} | Distance: {panTargetToTrueTargetDistance:F2}");
        }
    }

    private void Start()
    {
        defaultCameraPosition = targetCamera.transform.position; // is valid check on camera if not disable camera panning and log warning or something
        if (useUIPanning && UIPanObject != null)
        {
            if (UIPanObject.GetComponent<RectTransform>() == null)
            {
               Debug.LogWarning("UIPanObject does not have a RectTransform component. Disabling UI panning.");
               useUIPanning = false;
            }
        }
    }
    private void Update()
    {
        panTargetToTrueTargetDistance = GetPanToTargetDistance();
        if (panTargetToTrueTargetDistance > panPrecision)
        {
            ApplyPanMovement();
            if (useCameraPanning) SetCameraPosition();
            if (useUIPanning) ApplyUIPanning();
            if (useUIRotating) ApplyUIRotating();
        }
        ShowDebugObjects();
    }
}
