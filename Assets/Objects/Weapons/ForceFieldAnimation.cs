using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using static ForceFieldAnimation;
using static PlayerColoringBehaviour;
using static ProximityPixelationSystem;
using static ProximityPixelSenssor;

[CreateAssetMenu(fileName = "ForceFieldAnimation", menuName = "Scriptable Objects/ForceFieldAnimation")]
public unsafe class ForceFieldAnimation : ScriptableObject
{

    [Header("Animations")]
    [SerializeField] ForceAnimation animations;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnValidate() => animations.ReAllocateSamples();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnEnable() => animations.ReAllocateSamples();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDisable() => animations.FreeSamples();

    [Serializable]
    public struct ForceAnimation
    {
        [Header("Settings")]
        [SerializeField]
        int samplingResolution;

        [Header("Colors")]
        [SerializeField]
        AnimationCurve radius;
        [SerializeField]
        AnimationCurve saturation;
        [SerializeField]
        AnimationCurve value;

        [Header("Blockers")]
        [SerializeField]
        AnimationCurve blockerEnable;
        [SerializeField]
        AnimationCurve blockerFalloff;

        [Header("Position Warp")]
        [SerializeField]
        AnimationCurve positionWarpEnable;
        [SerializeField]
        AnimationCurve positionWarpStrength;
        [SerializeField]
        AnimationCurve positionWarpFalloff;
        [SerializeField]
        AnimationCurve positionWarpRadius;

        [Header("Scale Warp")]
        [SerializeField]
        AnimationCurve scaleWarpEnable;
        [SerializeField]
        AnimationCurve scaleWarpStrength;
        [SerializeField]
        AnimationCurve scaleWarpFalloff;
        [SerializeField]
        AnimationCurve scaleWarpRadius;

        [Header("Rotation Warp")]
        [SerializeField]
        AnimationCurve rotationWarpEnable;
        [SerializeField]
        AnimationCurve rotationWarpStrength;
        [SerializeField]
        AnimationCurve rotationWarpFalloff;
        [SerializeField]
        AnimationCurve rotationWarpRadius;

        [Header("Swirl/Twist Warp")]
        [SerializeField]
        AnimationCurve swirlEnable;
        [SerializeField]
        AnimationCurve swirlStrength;
        [SerializeField]
        AnimationCurve swirlFalloff;
        [SerializeField]
        AnimationCurve swirlRadius;

        [Header("Radial Pulsation")]
        [SerializeField]
        AnimationCurve pulsationEnable;
        [SerializeField]
        AnimationCurve pulsationFrequency;
        [SerializeField]
        AnimationCurve pulsationAmplitude;
        [SerializeField]
        AnimationCurve pulsationFalloff;
        [SerializeField]
        AnimationCurve pulsationRadius;

        ForceAnimationSample* forceAnimationSample;
        public bool isAllocated { get; private set; }

        public void ReAllocateSamples()
        {
            if (isAllocated && forceAnimationSample != null)
                UnsafeUtility.Free(forceAnimationSample, Allocator.Persistent);

            forceAnimationSample = (ForceAnimationSample*)UnsafeUtility.Malloc(
                samplingResolution * sizeof(ForceAnimationSample),
                UnsafeUtility.AlignOf<ForceAnimationSample>(),
                Allocator.Persistent
            );

            isAllocated = true;

            for (int i = 0; i < samplingResolution; i++)
            {
                float t = (float)i / (samplingResolution - 1);

                forceAnimationSample[i] = new ForceAnimationSample
                {
                    enableBlockerInfluence = blockerEnable.Evaluate(t) > 0.5f,
                    enablePositionWarp = positionWarpEnable.Evaluate(t) > 0.5f, 
                    enableScaleWarp = scaleWarpEnable.Evaluate(t) > 0.5f, 
                    enableRotationWarp = rotationWarpEnable.Evaluate(t) > 0.5f, 
                    enableSwirl = swirlEnable.Evaluate(t) > 0.5f, 
                    enablePulsation = pulsationEnable.Evaluate(t) > 0.5f, 
                    radius = radius.Evaluate(t),
                    saturation = saturation.Evaluate(t),
                    value = value.Evaluate(t),
                    blockerFalloff = blockerFalloff.Evaluate(t),
                    positionWarpStrength = positionWarpStrength.Evaluate(t),
                    positionWarpFalloff = positionWarpFalloff.Evaluate(t),
                    positionWarpRadius = positionWarpRadius.Evaluate(t),
                    scaleWarpStrength = scaleWarpStrength.Evaluate(t),
                    scaleWarpFalloff = scaleWarpFalloff.Evaluate(t),
                    scaleWarpRadius = scaleWarpRadius.Evaluate(t),
                    rotationWarpStrength = rotationWarpStrength.Evaluate(t),
                    rotationWarpFalloff = rotationWarpFalloff.Evaluate(t),
                    rotationWarpRadius = rotationWarpRadius.Evaluate(t),
                    swirlStrength = swirlStrength.Evaluate(t),
                    swirlFalloff = swirlFalloff.Evaluate(t),
                    swirlRadius = swirlRadius.Evaluate(t),
                    pulsationFrequency = pulsationFrequency.Evaluate(t),
                    pulsationAmplitude = pulsationAmplitude.Evaluate(t),
                    pulsationFalloff = pulsationFalloff.Evaluate(t),
                    pulsationRadius = pulsationRadius.Evaluate(t)
                };
            }
        }

        public void FreeSamples()
        {
            if (isAllocated && forceAnimationSample != null)
            {
                UnsafeUtility.Free(forceAnimationSample, Allocator.Persistent);
                forceAnimationSample = null;
                isAllocated = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ForceAnimationSample* SampleFast(float t) => &forceAnimationSample[math.clamp((int)(t * samplingResolution), 0, samplingResolution - 1)];

        
    }

    public struct ForceAnimationSample
    {
        public bool enableBlockerInfluence; 
        public bool enablePositionWarp; 
        public bool enableScaleWarp; 
        public bool enableRotationWarp; 
        public bool enableSwirl; 
        public bool enablePulsation; 

        public float radius;
        public float saturation;
        public float value;
        public float blockerFalloff;
        public float positionWarpStrength;
        public float positionWarpFalloff;
        public float positionWarpRadius;
        public float scaleWarpStrength;
        public float scaleWarpFalloff;
        public float scaleWarpRadius;
        public float rotationWarpStrength;
        public float rotationWarpFalloff;
        public float rotationWarpRadius;
        public float swirlStrength;
        public float swirlFalloff;
        public float swirlRadius;
        public float pulsationFrequency;
        public float pulsationAmplitude;
        public float pulsationFalloff;
        public float pulsationRadius;
    }

    public struct ForceFieldData
    {
        public GridSpaceColorGradient gradient;
        public GridSpaceForceField parameters;
        public ColorComponent colorComp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public void SampleFast(ref ForceFieldData dataReference, ProximityPixelSenssor applicant, float t)
    {
        ForceAnimationSample* sample = animations.SampleFast(t);

        ColorComponent modified = dataReference.colorComp;
        modified.saturation *= sample->saturation;
        modified.value *= sample->value;
        modified.SetHue(dataReference.colorComp.ReadHue);
        applicant.gridSpaceColor.color = modified.ActiveColor;

        applicant.gridSpaceColor.saturationBoost = dataReference.gradient.saturationBoost * sample->saturation;
        applicant.gridSpaceColor.radius = dataReference.gradient.radius * sample->radius;
        applicant.gridSpaceSensor.warpSpeed = dataReference.parameters.warpSpeed;
        applicant.gridSpaceSensor.chroma = dataReference.parameters.chroma;
        applicant.gridSpaceSensor.enableBlockerInfluence = sample->enableBlockerInfluence;
        applicant.gridSpaceSensor.blockerFalloff = dataReference.parameters.blockerFalloff * sample->blockerFalloff;
        applicant.gridSpaceSensor.enablePositionWarp = sample->enablePositionWarp; 
        applicant.gridSpaceSensor.positionWarpStrength = dataReference.parameters.positionWarpStrength * sample->positionWarpStrength;
        applicant.gridSpaceSensor.positionWarpFallof = dataReference.parameters.positionWarpFallof * sample->positionWarpFalloff;
        applicant.gridSpaceSensor.positionWarpRadius = dataReference.parameters.positionWarpRadius * sample->positionWarpRadius;
        applicant.gridSpaceSensor.enableScaleWarp = sample->enableScaleWarp; 
        applicant.gridSpaceSensor.scaleWarpStrength = dataReference.parameters.scaleWarpStrength * sample->scaleWarpStrength;
        applicant.gridSpaceSensor.scaleWarpFalloff = dataReference.parameters.scaleWarpFalloff * sample->scaleWarpFalloff;
        applicant.gridSpaceSensor.scaleWarpRadius = dataReference.parameters.scaleWarpRadius * sample->scaleWarpRadius;
        applicant.gridSpaceSensor.enableRotationWarp = sample->enableRotationWarp; 
        applicant.gridSpaceSensor.rotationWarpStrength = dataReference.parameters.rotationWarpStrength * sample->rotationWarpStrength;
        applicant.gridSpaceSensor.rotationWarpFalloff = dataReference.parameters.rotationWarpFalloff * sample->rotationWarpFalloff;
        applicant.gridSpaceSensor.rotationWarpRadius = dataReference.parameters.rotationWarpRadius * sample->rotationWarpRadius;
        applicant.gridSpaceSensor.enableSwirl = sample->enableSwirl; 
        applicant.gridSpaceSensor.swirlStrength = dataReference.parameters.swirlStrength * sample->swirlStrength;
        applicant.gridSpaceSensor.swirlFalloff = dataReference.parameters.swirlFalloff * sample->swirlFalloff;
        applicant.gridSpaceSensor.swirlRadius = dataReference.parameters.swirlRadius * sample->swirlRadius;
        applicant.gridSpaceSensor.enablePulsation = sample->enablePulsation; 
        applicant.gridSpaceSensor.pulsationFrequency = dataReference.parameters.pulsationFrequency * sample->pulsationFrequency;
        applicant.gridSpaceSensor.pulsationAmplitude = dataReference.parameters.pulsationAmplitude * sample->pulsationAmplitude;
        applicant.gridSpaceSensor.pulsationFallof = dataReference.parameters.pulsationFallof * sample->pulsationFalloff;
        applicant.gridSpaceSensor.pulsationRadius = dataReference.parameters.pulsationRadius * sample->pulsationRadius;
    }
}

public unsafe struct BurstableForceFieldAnimation
{

    ForceAnimationSample* forceAnimationSample;

}