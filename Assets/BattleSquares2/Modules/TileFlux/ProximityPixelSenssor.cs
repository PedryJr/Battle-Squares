using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static ProximityPixelationSystem;
using static ProximityPixelSenssor;

public sealed class ProximityPixelSenssor : MonoBehaviour
{
    IProximityPixelAnimation anim;
    [SerializeField]
    public ProximityPixelSensorConfig proximityPixelSensor;
    [HideInInspector]
    public GridSpaceColorGradient gridSpaceColor;
    [HideInInspector]
    public GridSpaceForceField gridSpaceSensor;

    Transform cachedTransform;
    private void Awake()
    {
        anim = GetComponent<ProjectileForceAnimationBehaviour>();
        cachedTransform = transform;
        if (proximityPixelSensor)
        {
            proximityPixelSensor.FetchDataUpdate(ref gridSpaceSensor, ref gridSpaceColor, cachedTransform.position, cachedTransform.rotation.eulerAngles.z);
            proximityPixelSensor.refreshAllActiveSensors += ProximityPixelSensor_refreshAllActiveSensors;
        }
    }

    private void ProximityPixelSensor_refreshAllActiveSensors()
    {
        proximityPixelSensor.FetchDataUpdate(ref gridSpaceSensor, ref gridSpaceColor, cachedTransform.position, cachedTransform.rotation.eulerAngles.z);
        if (anim != null) anim.RegenerateGroundTruth();
    }

    private void Start()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad") SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void OnDestroy()
    {
        if (proximityPixelSensor) proximityPixelSensor.refreshAllActiveSensors -= ProximityPixelSensor_refreshAllActiveSensors;
        if (gameObject.scene.name == "DontDestroyOnLoad") SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
    }

    private void OnEnable()
    {
        if (Singleton) Singleton.sensorObjects.Add(this);
    }

    private void OnDisable()
    {
        if (Singleton) Singleton.sensorObjects.RemoveSwapBack(this);
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (Singleton) Singleton.sensorObjects.Add(this);
    }

    private void Update()
    {
        if (!proximityPixelSensor) return;
        if (BackdropBehaviour.Singleton) BackdropBehaviour.Singleton.AddProximityColor(gridSpaceColor.color, gridSpaceSensor.origin, gridSpaceColor.radius, gridSpaceColor.saturationBoost);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CustomUpdate()
    {
        if (!Singleton) return;
        if (!proximityPixelSensor) return;
        proximityPixelSensor.RefetchPerInstanceData(ref gridSpaceSensor, ref gridSpaceColor, cachedTransform.position, cachedTransform.rotation.eulerAngles.z);
        Singleton.AddProximitySensor(ref gridSpaceSensor);
    }


    [Serializable]
    public struct GridSpaceColorGradient
    {
        public Color color;
        public float radius;
        public float saturationBoost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DemolishField() => Destroy(gameObject);

}