using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static ProximityPixelationSystem;
using static ProximityPixelSenssor;

[CreateAssetMenu(fileName = "ProximityPixelSensorConfig", menuName = "Scriptable Objects/ProximityPixelSensorConfig")]
public class ProximityPixelSensorConfig : ScriptableObject
{
    [SerializeField]
    public GridSpaceColorGradient gridSpaceColor;
    [SerializeField]
    public GridSpaceForceField gridSpaceSensor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void FetchDataUpdate(ref GridSpaceForceField gridSpaceSensor, ref GridSpaceColorGradient gridSpaceColor, Vector3 position, float zRot)
    {
        gridSpaceColor = this.gridSpaceColor;
        gridSpaceSensor.warpSpeed = this.gridSpaceSensor.warpSpeed;
        gridSpaceSensor.chroma = this.gridSpaceSensor.chroma;
        gridSpaceSensor.enableBlockerInfluence = this.gridSpaceSensor.enableBlockerInfluence;
        gridSpaceSensor.blockerFalloff = this.gridSpaceSensor.blockerFalloff;
        gridSpaceSensor.enablePositionWarp = this.gridSpaceSensor.enablePositionWarp;
        gridSpaceSensor.positionWarpStrength = this.gridSpaceSensor.positionWarpStrength;
        gridSpaceSensor.positionWarpFallof = this.gridSpaceSensor.positionWarpFallof;
        gridSpaceSensor.positionWarpRadius = this.gridSpaceSensor.positionWarpRadius;
        gridSpaceSensor.enableScaleWarp = this.gridSpaceSensor.enableScaleWarp;
        gridSpaceSensor.scaleWarpStrength = this.gridSpaceSensor.scaleWarpStrength;
        gridSpaceSensor.scaleWarpFalloff = this.gridSpaceSensor.scaleWarpFalloff;
        gridSpaceSensor.scaleWarpRadius = this.gridSpaceSensor.scaleWarpRadius;
        gridSpaceSensor.enableRotationWarp = this.gridSpaceSensor.enableRotationWarp;
        gridSpaceSensor.rotationWarpStrength = this.gridSpaceSensor.rotationWarpStrength;
        gridSpaceSensor.rotationWarpFalloff = this.gridSpaceSensor.rotationWarpFalloff;
        gridSpaceSensor.rotationWarpRadius = this.gridSpaceSensor.rotationWarpRadius;
        gridSpaceSensor.enableSwirl = this.gridSpaceSensor.enableSwirl;
        gridSpaceSensor.swirlStrength = this.gridSpaceSensor.swirlStrength;
        gridSpaceSensor.swirlFalloff = this.gridSpaceSensor.swirlFalloff;
        gridSpaceSensor.swirlRadius = this.gridSpaceSensor.swirlRadius;
        gridSpaceSensor.enablePulsation = this.gridSpaceSensor.enablePulsation;
        gridSpaceSensor.pulsationFrequency = this.gridSpaceSensor.pulsationFrequency;
        gridSpaceSensor.pulsationAmplitude = this.gridSpaceSensor.pulsationAmplitude;
        gridSpaceSensor.pulsationFallof = this.gridSpaceSensor.pulsationFallof;
        gridSpaceSensor.pulsationRadius = this.gridSpaceSensor.pulsationRadius;

        gridSpaceSensor.origin = new float2(position.x, position.y);
        gridSpaceSensor.rotation = zRot;
        gridSpaceSensor.colorValue = new float3(gridSpaceColor.color.r, gridSpaceColor.color.g, gridSpaceColor.color.b);
        gridSpaceSensor.colorRadius = gridSpaceColor.radius;
    }

    internal void RefetchPerInstanceData(ref GridSpaceForceField gridSpaceSensor, ref GridSpaceColorGradient gridSpaceColor, Vector3 position, float zRot)
    {
        gridSpaceSensor.origin = new float2(position.x, position.y);
        gridSpaceSensor.rotation = zRot;
        gridSpaceSensor.colorValue = new float3(gridSpaceColor.color.r, gridSpaceColor.color.g, gridSpaceColor.color.b);
        gridSpaceSensor.colorRadius = gridSpaceColor.radius;
    }

    public event Action refreshAllActiveSensors = new(() => { });

    private void OnValidate()
    {
        if(refreshAllActiveSensors != null) refreshAllActiveSensors();
    }

}