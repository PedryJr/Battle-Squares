using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WorldColors : MonoBehaviour
{

    List<Light2D> lights;

    [SerializeField] Material pixelEffectMaterial;
    [SerializeField] Material arenaMaterial;

    [SerializeField] Color pixeleffectColor;
    [SerializeField] Color arenaColor;
    [SerializeField] Color lightColor;

    [SerializeField] [Range(0f, 1f)] float lightDensity;
    [SerializeField] [Range(0f, 1f)] float shadowDesnity;

    float maxLightStrength = 1f;
    public void RegisterLight(Light2D light, float maxStrength)
    {
        lights.Add(light);
        maxLightStrength = maxStrength;
    }

    private void Awake()
    {
        lights = new List<Light2D>();
    }

    private void Update()
    {
        for (int i = lights.Count - 1; i >= 0; i--)
        {
            if (!lights[i]) lights.RemoveAt(i);
            else
            {
                lights[i].color = lightColor;
                lights[i].intensity = maxLightStrength * lightDensity;
                lights[i].shadowIntensity = shadowDesnity;
                /*                lights[i].volumeIntensity = lightDensity;
                                lights[i].shadowVolumeIntensity = shadowDesnity;*/
            }
        }
        pixelEffectMaterial.SetColor("_Color", pixeleffectColor);
        arenaMaterial.SetColor("_ColorOverride", arenaColor);
    }

    /*    static WorldColors instance;
        public static WorldColors Instance 
        { 
            get 
            {
                if (!instance) instance = FindAnyObjectByType<WorldColors>();
                return instance;
            }
        }

        public static Color GetPixelEffectColor() => Instance.pixeleffectColor;
        public static Color GetGroundColor() => Instance.groundColor;
        public static Color GetLightColor() => Instance.lightColor;*/

}
