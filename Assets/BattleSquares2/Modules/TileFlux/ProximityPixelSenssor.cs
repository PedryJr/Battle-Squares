using Unity.Mathematics;
using UnityEngine;
using static ProximityPixelationSystem;
using System;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;


public sealed class ProximityPixelSenssor : MonoBehaviour
{
    Transform cachedTransform;
    private void Awake() => cachedTransform = transform;

    private void Start()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad") SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void OnDestroy()
    {
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

    [SerializeField]
    public GridSpaceColorGradient gridSpaceColor;


    [SerializeField]
    public GridSpaceForceField sensorData;

    private void Update()
    {
        if(BackdropBehaviour.Singleton) BackdropBehaviour.Singleton.AddProximityColor(gridSpaceColor.color, sensorData.origin, gridSpaceColor.radius, gridSpaceColor.saturationBoost);
    }

    public void CustomUpdate()
    {
        if (!Singleton) return;
        Vector3 position = cachedTransform.position;

/*        sensorData.forceCoordinate = new float2(position.x, position.y);
        sensorData.forceZRotation = cachedTransform.rotation.z;*/

        sensorData.origin = new float2(position.x, position.y);
        sensorData.rotation = cachedTransform.rotation.eulerAngles.z;
        sensorData.colorValue = (Vector3)(Vector4)gridSpaceColor.color;
        sensorData.colorRadius = gridSpaceColor.radius;

        Singleton.AddProximitySensor(ref sensorData);
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