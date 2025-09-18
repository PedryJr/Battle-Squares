using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class PermaLightBehvaiour : MonoBehaviour
{

    Transform cachedTransform;
    Light2D myLight;

    Vector3 onScale;
    Vector3 offScale = new Vector3(0, 0, 0);

    bool lastActive = false;
    bool isActive = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetActive(bool activity) => isActive = activity;

    private void Awake()
    {
        cachedTransform = transform;
        myLight = GetComponent<Light2D>();
        onScale = transform.localScale;
    }


    private void Update()
    {

        if (isActive) ActiveUpdate();
        else InactiveUpdate();
        lastActive = isActive;

    }

    void ActiveUpdate()
    {
        Vector3 currentScale = cachedTransform.localScale;
        currentScale = Vector3.Lerp(currentScale, onScale, Time.deltaTime * AnimationAnchor.animationSpeed);
        cachedTransform.localScale = currentScale;
        myLight.intensity = Mathf.Lerp(myLight.intensity, 1f, AnimationAnchor.animationSpeed);
        if (myLight.intensity > 0.001f && !myLight.enabled) myLight.enabled = true;
    }

    void InactiveUpdate()
    {
        Vector3 currentScale = cachedTransform.localScale;
        currentScale = Vector3.Lerp(currentScale, offScale, Time.deltaTime * AnimationAnchor.animationSpeed);
        cachedTransform.localScale = currentScale;
        myLight.intensity = Mathf.Lerp(myLight.intensity, 0f, AnimationAnchor.animationSpeed);
        if(myLight.intensity < 0.001f && myLight.enabled) myLight.enabled = false;
    }


}
