using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class WorldLight : MonoBehaviour
{

    bool markedForDestruction = false;

    Transform cachedTransform;
    PermaLightBehvaiour permaLight;
    Light2D myLight;

    public static int WorldLightCount = 0;
    public static Dictionary<int, WorldLight> WorldLights = new Dictionary<int, WorldLight>();
    public static int worldLightIDCounter = 0;
    public int worldLightID = 0;

    public float targetLightIntensity = 1.0f;
    public float usedLightIntensity = 0.0f;

    Vector2 targetPos;
    Vector2 originalScale;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTargetPos(Vector2 targetPos) => this.targetPos = targetPos;

    private void Awake()
    {
        cachedTransform = transform;
        originalScale = transform.localScale;
        cachedTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        worldLightID = worldLightIDCounter;
        WorldLights.Add(worldLightID, this);
        worldLightIDCounter++;
        myLight = GetComponentInChildren<Light2D>();
        permaLight = FindAnyObjectByType<PermaLightBehvaiour>();
        UpdateSharedLightParameters();
    }

    public void UpdateSharedLightParameters()
    {
        WorldLightCount = WorldLights.Count;
        bool shouldPermaLight = WorldLightCount == 0;
        permaLight.SetActive(shouldPermaLight);
    }

    private void Update()
    {

        UpdateLightPosition();
        if (markedForDestruction) TurnInvisible();
        else TurnVisible();
        UpdateVisible();

    }

    private void UpdateVisible()
    {
        usedLightIntensity = Mathf.Lerp(usedLightIntensity, targetLightIntensity, AnimationAnchor.animationSpeed);
        myLight.intensity = usedLightIntensity;
    }


    public void DeleteLight()
    {
        markedForDestruction = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateLightPosition() => cachedTransform.position = Vector3.Lerp(cachedTransform.position, new Vector3(targetPos.x, targetPos.y, cachedTransform.position.z), Time.deltaTime * AnimationAnchor.animationSpeed);

    void TurnInvisible()
    {
        if (WorldLights.ContainsKey(worldLightID))
        {
            WorldLights.Remove(worldLightID);
            UpdateSharedLightParameters();
        }
        cachedTransform.localScale = Vector3.Lerp(cachedTransform.localScale, new Vector3(0, 0, 0), Time.deltaTime * AnimationAnchor.animationSpeed);
        targetLightIntensity = 0;
        if (cachedTransform.localScale.x < 0.009f)
        {
            Destroy(gameObject);
        }
    }
    private void TurnVisible()
    {
        cachedTransform.localScale = Vector3.Lerp(cachedTransform.localScale, originalScale, Time.deltaTime * AnimationAnchor.animationSpeed);
        targetLightIntensity = 1f / WorldLightCount;
    }

    internal void OverrideID(int lightId)
    {
        this.worldLightID = lightId;
        WorldLights.Add(lightId, this);
        UpdateSharedLightParameters();
    }
}
