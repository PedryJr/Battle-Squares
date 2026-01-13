using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ParticleSystemSerializer
{
    public static string SerializeToJson(ParticleSystem ps)
    {
        var data = new JObject();


        data["renderer"] = SerializeRenderer(ps.GetComponent<ParticleSystemRenderer>());

        data["main"] = SerializeMainModule(ps.main);
        data["emission"] = SerializeEmissionModule(ps.emission);
        data["shape"] = SerializeShapeModule(ps.shape);
        data["velocityOverLifetime"] = SerializeVelocityOverLifetimeModule(ps.velocityOverLifetime);
        data["limitVelocityOverLifetime"] = SerializeLimitVelocityOverLifetimeModule(ps.limitVelocityOverLifetime);
        data["lifetimeByEmitterSpeed"] = SerializeLifetimeByEmitterSpeedModule(ps.lifetimeByEmitterSpeed);
        data["inheritVelocity"] = SerializeInheritVelocityModule(ps.inheritVelocity);
        data["forceOverLifetime"] = SerializeForceOverLifetimeModule(ps.forceOverLifetime);
        data["colorOverLifetime"] = SerializeColorOverLifetimeModule(ps.colorOverLifetime);
        data["colorBySpeed"] = SerializeColorBySpeedModule(ps.colorBySpeed);
        data["sizeOverLifetime"] = SerializeSizeOverLifetimeModule(ps.sizeOverLifetime);
        data["sizeBySpeed"] = SerializeSizeBySpeedModule(ps.sizeBySpeed);
        data["rotationOverLifetime"] = SerializeRotationOverLifetimeModule(ps.rotationOverLifetime);
        data["rotationBySpeed"] = SerializeRotationBySpeedModule(ps.rotationBySpeed);
        data["externalForces"] = SerializeExternalForcesModule(ps.externalForces);
        data["noise"] = SerializeNoiseModule(ps.noise);
        data["collision"] = SerializeCollisionModule(ps.collision);
        data["trigger"] = SerializeTriggerModule(ps.trigger);
        data["subEmitters"] = SerializeSubEmittersModule(ps.subEmitters);
        data["textureSheetAnimation"] = SerializeTextureSheetAnimationModule(ps.textureSheetAnimation);
        data["trails"] = SerializeTrailsModule(ps.trails);
        data["customData"] = SerializeCustomDataModule(ps.customData);

        return data.ToString(Formatting.Indented);
    }

    public static void SaveToFile(ParticleSystem ps, string path)
    {
        string json = SerializeToJson(ps);
        File.WriteAllText(path, json);
    }

    public static void DeserializeFromJson(ParticleSystem ps, string json)
    {
        var data = JObject.Parse(json);

        if (data["renderer"] != null) DeserializeRenderer(ps.GetComponent<ParticleSystemRenderer>(), data["renderer"]);

        if (data["main"] != null) DeserializeMainModule(ps.main, data["main"]);
        if (data["emission"] != null) DeserializeEmissionModule(ps.emission, data["emission"]);
        if (data["shape"] != null) DeserializeShapeModule(ps.shape, data["shape"]);
        if (data["velocityOverLifetime"] != null) DeserializeVelocityOverLifetimeModule(ps.velocityOverLifetime, data["velocityOverLifetime"]);
        if (data["limitVelocityOverLifetime"] != null) DeserializeLimitVelocityOverLifetimeModule(ps.limitVelocityOverLifetime, data["limitVelocityOverLifetime"]);
        if (data["lifetimeByEmitterSpeed"] != null) DeserializeLifetimeByEmitterSpeedModule(ps.lifetimeByEmitterSpeed, data["lifetimeByEmitterSpeed"]);
        if (data["inheritVelocity"] != null) DeserializeInheritVelocityModule(ps.inheritVelocity, data["inheritVelocity"]);
        if (data["forceOverLifetime"] != null) DeserializeForceOverLifetimeModule(ps.forceOverLifetime, data["forceOverLifetime"]);
        if (data["colorOverLifetime"] != null) DeserializeColorOverLifetimeModule(ps.colorOverLifetime, data["colorOverLifetime"]);
        if (data["colorBySpeed"] != null) DeserializeColorBySpeedModule(ps.colorBySpeed, data["colorBySpeed"]);
        if (data["sizeOverLifetime"] != null) DeserializeSizeOverLifetimeModule(ps.sizeOverLifetime, data["sizeOverLifetime"]);
        if (data["sizeBySpeed"] != null) DeserializeSizeBySpeedModule(ps.sizeBySpeed, data["sizeBySpeed"]);
        if (data["rotationOverLifetime"] != null) DeserializeRotationOverLifetimeModule(ps.rotationOverLifetime, data["rotationOverLifetime"]);
        if (data["rotationBySpeed"] != null) DeserializeRotationBySpeedModule(ps.rotationBySpeed, data["rotationBySpeed"]);
        if (data["externalForces"] != null) DeserializeExternalForcesModule(ps.externalForces, data["externalForces"]);
        if (data["noise"] != null) DeserializeNoiseModule(ps.noise, data["noise"]);
        if (data["collision"] != null) DeserializeCollisionModule(ps.collision, data["collision"]);
        if (data["trigger"] != null) DeserializeTriggerModule(ps.trigger, data["trigger"]);
        if (data["subEmitters"] != null) DeserializeSubEmittersModule(ps.subEmitters, data["subEmitters"]);
        if (data["textureSheetAnimation"] != null) DeserializeTextureSheetAnimationModule(ps.textureSheetAnimation, data["textureSheetAnimation"]);
        if (data["lights"] != null) DeserializeLightsModule(ps.lights, data["lights"]);
        if (data["trails"] != null) DeserializeTrailsModule(ps.trails, data["trails"]);
        if (data["customData"] != null) DeserializeCustomDataModule(ps.customData, data["customData"]);
    }

    public static void LoadFromFile(ParticleSystem ps, string path)
    {
        bool wasPlaying = ps.isPlaying;
        if (wasPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        string json;
        if (File.Exists(path)) json = File.ReadAllText(path);
        else json = defaultParticleJson;
        DeserializeFromJson(ps, json);
        if(wasPlaying) ps.Play();
    }

    // Serialization methods for each module
    private static JObject SerializeMainModule(ParticleSystem.MainModule m)
    {
        return new JObject
        {
            ["duration"] = m.duration,
            ["loop"] = m.loop,
            ["prewarm"] = m.prewarm,
            ["startDelay"] = SerializeMinMaxCurve(m.startDelay),
            ["startDelayMultiplier"] = m.startDelayMultiplier,
            ["startLifetime"] = SerializeMinMaxCurve(m.startLifetime),
            ["startLifetimeMultiplier"] = m.startLifetimeMultiplier,
            ["startSpeed"] = SerializeMinMaxCurve(m.startSpeed),
            ["startSpeedMultiplier"] = m.startSpeedMultiplier,
            ["startSize3D"] = m.startSize3D,
            ["startSize"] = SerializeMinMaxCurve(m.startSize),
            ["startSizeMultiplier"] = m.startSizeMultiplier,
            ["startSizeX"] = SerializeMinMaxCurve(m.startSizeX),
            ["startSizeXMultiplier"] = m.startSizeXMultiplier,
            ["startSizeY"] = SerializeMinMaxCurve(m.startSizeY),
            ["startSizeYMultiplier"] = m.startSizeYMultiplier,
            ["startSizeZ"] = SerializeMinMaxCurve(m.startSizeZ),
            ["startSizeZMultiplier"] = m.startSizeZMultiplier,
            ["startRotation3D"] = m.startRotation3D,
            ["startRotation"] = SerializeMinMaxCurve(m.startRotation),
            ["startRotationMultiplier"] = m.startRotationMultiplier,
            ["startRotationX"] = SerializeMinMaxCurve(m.startRotationX),
            ["startRotationXMultiplier"] = m.startRotationXMultiplier,
            ["startRotationY"] = SerializeMinMaxCurve(m.startRotationY),
            ["startRotationYMultiplier"] = m.startRotationYMultiplier,
            ["startRotationZ"] = SerializeMinMaxCurve(m.startRotationZ),
            ["startRotationZMultiplier"] = m.startRotationZMultiplier,
            ["flipRotation"] = m.flipRotation,
            ["startColor"] = SerializeMinMaxGradient(m.startColor),
            ["gravityModifier"] = SerializeMinMaxCurve(m.gravityModifier),
            ["gravityModifierMultiplier"] = m.gravityModifierMultiplier,
            ["simulationSpace"] = m.simulationSpace.ToString(),
            ["customSimulationSpace"] = m.customSimulationSpace != null ? m.customSimulationSpace.name : null,
            ["simulationSpeed"] = m.simulationSpeed,
            ["useUnscaledTime"] = m.useUnscaledTime,
            ["scalingMode"] = m.scalingMode.ToString(),
            ["playOnAwake"] = m.playOnAwake,
            ["emitterVelocityMode"] = m.emitterVelocityMode.ToString(),
            ["maxParticles"] = m.maxParticles,
            ["stopAction"] = m.stopAction.ToString(),
            ["cullingMode"] = m.cullingMode.ToString(),
            ["ringBufferMode"] = m.ringBufferMode.ToString(),
            ["ringBufferLoopRange"] = SerializeVector2(m.ringBufferLoopRange)
        };
    }

    private static JObject SerializeEmissionModule(ParticleSystem.EmissionModule m)
    {
        var obj = new JObject
        {
            ["enabled"] = m.enabled,
            ["rateOverTime"] = SerializeMinMaxCurve(m.rateOverTime),
            ["rateOverTimeMultiplier"] = m.rateOverTimeMultiplier,
            ["rateOverDistance"] = SerializeMinMaxCurve(m.rateOverDistance),
            ["rateOverDistanceMultiplier"] = m.rateOverDistanceMultiplier,
            ["burstCount"] = m.burstCount
        };

        var bursts = new JArray();
        for (int i = 0; i < m.burstCount; i++)
        {
            var burst = m.GetBurst(i);
            bursts.Add(new JObject
            {
                ["time"] = burst.time,
                ["count"] = SerializeMinMaxCurve(burst.count),
                ["cycleCount"] = burst.cycleCount,
                ["repeatInterval"] = burst.repeatInterval,
                ["probability"] = burst.probability
            });
        }
        obj["bursts"] = bursts;

        return obj;
    }

    private static JObject SerializeShapeModule(ParticleSystem.ShapeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["shapeType"] = m.shapeType.ToString(),
            ["angle"] = m.angle,
            ["radius"] = m.radius,
            ["radiusMode"] = m.radiusMode.ToString(),
            ["radiusSpread"] = m.radiusSpread,
            ["radiusSpeed"] = SerializeMinMaxCurve(m.radiusSpeed),
            ["radiusSpeedMultiplier"] = m.radiusSpeedMultiplier,
            ["radiusThickness"] = m.radiusThickness,
            ["arc"] = m.arc,
            ["arcMode"] = m.arcMode.ToString(),
            ["arcSpread"] = m.arcSpread,
            ["arcSpeed"] = SerializeMinMaxCurve(m.arcSpeed),
            ["arcSpeedMultiplier"] = m.arcSpeedMultiplier,
            ["donutRadius"] = m.donutRadius,
            ["length"] = m.length,
            ["boxThickness"] = SerializeVector3(m.boxThickness),
            ["meshShapeType"] = m.meshShapeType.ToString(),
            ["meshSpawnMode"] = m.meshSpawnMode.ToString(),
            ["meshSpawnSpread"] = m.meshSpawnSpread,
            ["meshSpawnSpeed"] = SerializeMinMaxCurve(m.meshSpawnSpeed),
            ["meshSpawnSpeedMultiplier"] = m.meshSpawnSpeedMultiplier,
            ["useMeshMaterialIndex"] = m.useMeshMaterialIndex,
            ["meshMaterialIndex"] = m.meshMaterialIndex,
            ["useMeshColors"] = m.useMeshColors,
            ["normalOffset"] = m.normalOffset,
            ["position"] = SerializeVector3(m.position),
            ["rotation"] = SerializeVector3(m.rotation),
            ["scale"] = SerializeVector3(m.scale),
            ["textureClipChannel"] = m.textureClipChannel.ToString(),
            ["textureClipThreshold"] = m.textureClipThreshold,
            ["textureColorAffectsParticles"] = m.textureColorAffectsParticles,
            ["textureAlphaAffectsParticles"] = m.textureAlphaAffectsParticles,
            ["textureBilinearFiltering"] = m.textureBilinearFiltering,
            ["textureUVChannel"] = m.textureUVChannel,
            ["alignToDirection"] = m.alignToDirection,
            ["randomDirectionAmount"] = m.randomDirectionAmount,
            ["sphericalDirectionAmount"] = m.sphericalDirectionAmount,
            ["randomPositionAmount"] = m.randomPositionAmount,
        };
    }

    private static JObject SerializeVelocityOverLifetimeModule(ParticleSystem.VelocityOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["y"] = SerializeMinMaxCurve(m.y),
            ["z"] = SerializeMinMaxCurve(m.z),
            ["xMultiplier"] = m.xMultiplier,
            ["yMultiplier"] = m.yMultiplier,
            ["zMultiplier"] = m.zMultiplier,
            ["orbitalX"] = SerializeMinMaxCurve(m.orbitalX),
            ["orbitalY"] = SerializeMinMaxCurve(m.orbitalY),
            ["orbitalZ"] = SerializeMinMaxCurve(m.orbitalZ),
            ["orbitalXMultiplier"] = m.orbitalXMultiplier,
            ["orbitalYMultiplier"] = m.orbitalYMultiplier,
            ["orbitalZMultiplier"] = m.orbitalZMultiplier,
            ["orbitalOffsetX"] = SerializeMinMaxCurve(m.orbitalOffsetX),
            ["orbitalOffsetY"] = SerializeMinMaxCurve(m.orbitalOffsetY),
            ["orbitalOffsetZ"] = SerializeMinMaxCurve(m.orbitalOffsetZ),
            ["orbitalOffsetXMultiplier"] = m.orbitalOffsetXMultiplier,
            ["orbitalOffsetYMultiplier"] = m.orbitalOffsetYMultiplier,
            ["orbitalOffsetZMultiplier"] = m.orbitalOffsetZMultiplier,
            ["radial"] = SerializeMinMaxCurve(m.radial),
            ["radialMultiplier"] = m.radialMultiplier,
            ["speedModifier"] = SerializeMinMaxCurve(m.speedModifier),
            ["speedModifierMultiplier"] = m.speedModifierMultiplier,
            ["space"] = m.space.ToString()
        };
    }



    private static JObject SerializeLimitVelocityOverLifetimeModule(ParticleSystem.LimitVelocityOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["limitX"] = SerializeMinMaxCurve(m.limitX),
            ["limitXMultiplier"] = m.limitXMultiplier,
            ["limitY"] = SerializeMinMaxCurve(m.limitY),
            ["limitYMultiplier"] = m.limitYMultiplier,
            ["limitZ"] = SerializeMinMaxCurve(m.limitZ),
            ["limitZMultiplier"] = m.limitZMultiplier,
            ["limit"] = SerializeMinMaxCurve(m.limit),
            ["limitMultiplier"] = m.limitMultiplier,
            ["dampen"] = m.dampen,
            ["separateAxes"] = m.separateAxes,
            ["space"] = m.space.ToString(),
            ["drag"] = SerializeMinMaxCurve(m.drag),
            ["dragMultiplier"] = m.dragMultiplier,
            ["multiplyDragByParticleSize"] = m.multiplyDragByParticleSize,
            ["multiplyDragByParticleVelocity"] = m.multiplyDragByParticleVelocity
        };
    }

    private static JObject SerializeLifetimeByEmitterSpeedModule(ParticleSystem.LifetimeByEmitterSpeedModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["curve"] = SerializeMinMaxCurve(m.curve),
            ["curveMultiplier"] = m.curveMultiplier,
            ["range"] = SerializeVector2(m.range)
        };
    }

    private static JObject SerializeInheritVelocityModule(ParticleSystem.InheritVelocityModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["mode"] = m.mode.ToString(),
            ["curve"] = SerializeMinMaxCurve(m.curve),
            ["curveMultiplier"] = m.curveMultiplier
        };
    }

    private static JObject SerializeForceOverLifetimeModule(ParticleSystem.ForceOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["y"] = SerializeMinMaxCurve(m.y),
            ["z"] = SerializeMinMaxCurve(m.z),
            ["xMultiplier"] = m.xMultiplier,
            ["yMultiplier"] = m.yMultiplier,
            ["zMultiplier"] = m.zMultiplier,
            ["space"] = m.space.ToString(),
            ["randomized"] = m.randomized
        };
    }

    private static JObject SerializeColorOverLifetimeModule(ParticleSystem.ColorOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["color"] = SerializeMinMaxGradient(m.color)
        };
    }

    private static JObject SerializeColorBySpeedModule(ParticleSystem.ColorBySpeedModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["color"] = SerializeMinMaxGradient(m.color),
            ["range"] = SerializeVector2(m.range)
        };
    }

    private static JObject SerializeSizeOverLifetimeModule(ParticleSystem.SizeOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["size"] = SerializeMinMaxCurve(m.size),
            ["sizeMultiplier"] = m.sizeMultiplier,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["xMultiplier"] = m.xMultiplier,
            ["y"] = SerializeMinMaxCurve(m.y),
            ["yMultiplier"] = m.yMultiplier,
            ["z"] = SerializeMinMaxCurve(m.z),
            ["zMultiplier"] = m.zMultiplier,
            ["separateAxes"] = m.separateAxes
        };
    }

    private static JObject SerializeSizeBySpeedModule(ParticleSystem.SizeBySpeedModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["size"] = SerializeMinMaxCurve(m.size),
            ["sizeMultiplier"] = m.sizeMultiplier,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["xMultiplier"] = m.xMultiplier,
            ["y"] = SerializeMinMaxCurve(m.y),
            ["yMultiplier"] = m.yMultiplier,
            ["z"] = SerializeMinMaxCurve(m.z),
            ["zMultiplier"] = m.zMultiplier,
            ["separateAxes"] = m.separateAxes,
            ["range"] = SerializeVector2(m.range)
        };
    }

    private static JObject SerializeRotationOverLifetimeModule(ParticleSystem.RotationOverLifetimeModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["xMultiplier"] = m.xMultiplier,
            ["y"] = SerializeMinMaxCurve(m.y),
            ["yMultiplier"] = m.yMultiplier,
            ["z"] = SerializeMinMaxCurve(m.z),
            ["zMultiplier"] = m.zMultiplier,
            ["separateAxes"] = m.separateAxes
        };
    }

    private static JObject SerializeRotationBySpeedModule(ParticleSystem.RotationBySpeedModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["x"] = SerializeMinMaxCurve(m.x),
            ["xMultiplier"] = m.xMultiplier,
            ["y"] = SerializeMinMaxCurve(m.y),
            ["yMultiplier"] = m.yMultiplier,
            ["z"] = SerializeMinMaxCurve(m.z),
            ["zMultiplier"] = m.zMultiplier,
            ["separateAxes"] = m.separateAxes,
            ["range"] = SerializeVector2(m.range)
        };
    }

    private static JObject SerializeExternalForcesModule(ParticleSystem.ExternalForcesModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["multiplier"] = m.multiplier
        };
    }

    private static JObject SerializeNoiseModule(ParticleSystem.NoiseModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["strength"] = SerializeMinMaxCurve(m.strengthX),
            ["strengthMultiplier"] = m.strengthXMultiplier,
            ["strengthX"] = SerializeMinMaxCurve(m.strengthX),
            ["strengthXMultiplier"] = m.strengthXMultiplier,
            ["strengthY"] = SerializeMinMaxCurve(m.strengthY),
            ["strengthYMultiplier"] = m.strengthYMultiplier,
            ["strengthZ"] = SerializeMinMaxCurve(m.strengthZ),
            ["strengthZMultiplier"] = m.strengthZMultiplier,
            ["separateAxes"] = m.separateAxes,
            ["frequency"] = m.frequency,
            ["damping"] = m.damping,
            ["octaveCount"] = m.octaveCount,
            ["octaveMultiplier"] = m.octaveMultiplier,
            ["octaveScale"] = m.octaveScale,
            ["quality"] = m.quality.ToString(),
            ["scrollSpeed"] = SerializeMinMaxCurve(m.scrollSpeed),
            ["scrollSpeedMultiplier"] = m.scrollSpeedMultiplier,
            ["remapEnabled"] = m.remapEnabled,
            ["remap"] = SerializeMinMaxCurve(m.remapX),
            ["remapMultiplier"] = m.remapXMultiplier,
            ["remapX"] = SerializeMinMaxCurve(m.remapX),
            ["remapXMultiplier"] = m.remapXMultiplier,
            ["remapY"] = SerializeMinMaxCurve(m.remapY),
            ["remapYMultiplier"] = m.remapYMultiplier,
            ["remapZ"] = SerializeMinMaxCurve(m.remapZ),
            ["remapZMultiplier"] = m.remapZMultiplier,
            ["positionAmount"] = SerializeMinMaxCurve(m.positionAmount),
            ["rotationAmount"] = SerializeMinMaxCurve(m.rotationAmount),
            ["sizeAmount"] = SerializeMinMaxCurve(m.sizeAmount)
        };
    }

    private static JObject SerializeCollisionModule(ParticleSystem.CollisionModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["type"] = m.type.ToString(),
            ["mode"] = m.mode.ToString(),
            ["dampen"] = SerializeMinMaxCurve(m.dampen),
            ["dampenMultiplier"] = m.dampenMultiplier,
            ["bounce"] = SerializeMinMaxCurve(m.bounce),
            ["bounceMultiplier"] = m.bounceMultiplier,
            ["lifetimeLoss"] = SerializeMinMaxCurve(m.lifetimeLoss),
            ["lifetimeLossMultiplier"] = m.lifetimeLossMultiplier,
            ["minKillSpeed"] = m.minKillSpeed,
            ["maxKillSpeed"] = m.maxKillSpeed,
            ["collidesWith"] = m.collidesWith.value,
            ["enableDynamicColliders"] = m.enableDynamicColliders,
            ["maxCollisionShapes"] = m.maxCollisionShapes,
            ["quality"] = m.quality.ToString(),
            ["voxelSize"] = m.voxelSize,
            ["radiusScale"] = m.radiusScale,
            ["sendCollisionMessages"] = m.sendCollisionMessages,
            ["colliderForce"] = m.colliderForce,
            ["multiplyColliderForceByCollisionAngle"] = m.multiplyColliderForceByCollisionAngle,
            ["multiplyColliderForceByParticleSpeed"] = m.multiplyColliderForceByParticleSpeed,
            ["multiplyColliderForceByParticleSize"] = m.multiplyColliderForceByParticleSize,

        };
    }

    private static JObject SerializeTriggerModule(ParticleSystem.TriggerModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["inside"] = m.inside.ToString(),
            ["outside"] = m.outside.ToString(),
            ["enter"] = m.enter.ToString(),
            ["exit"] = m.exit.ToString(),
            ["radiusScale"] = m.radiusScale
        };
    }

    private static JObject SerializeSubEmittersModule(ParticleSystem.SubEmittersModule m)
    {
        var obj = new JObject
        {
            ["enabled"] = m.enabled,
            ["subEmittersCount"] = m.subEmittersCount
        };

        return obj;
    }

    private static JObject SerializeTextureSheetAnimationModule(ParticleSystem.TextureSheetAnimationModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["numTilesX"] = m.numTilesX,
            ["numTilesY"] = m.numTilesY,
            ["animation"] = m.animation.ToString(),
            ["rowMode"] = m.rowMode.ToString(),
            ["frameOverTime"] = SerializeMinMaxCurve(m.frameOverTime),
            ["frameOverTimeMultiplier"] = m.frameOverTimeMultiplier,
            ["startFrame"] = SerializeMinMaxCurve(m.startFrame),
            ["startFrameMultiplier"] = m.startFrameMultiplier,
            ["cycleCount"] = m.cycleCount,
            ["rowIndex"] = m.rowIndex
        };
    }

    private static JObject SerializeLightsModule(ParticleSystem.LightsModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled,
            ["ratio"] = m.ratio,
            ["useRandomDistribution"] = m.useRandomDistribution,
            ["useParticleColor"] = m.useParticleColor,
            ["sizeAffectsRange"] = m.sizeAffectsRange,
            ["alphaAffectsIntensity"] = m.alphaAffectsIntensity,
            ["range"] = SerializeMinMaxCurve(m.range),
            ["rangeMultiplier"] = m.rangeMultiplier,
            ["intensity"] = SerializeMinMaxCurve(m.intensity),
            ["intensityMultiplier"] = m.intensityMultiplier,
            ["maxLights"] = m.maxLights
        };
    }

    private static JObject SerializeTrailsModule(ParticleSystem.TrailModule m)
    {
        return new JObject
        {
            ["mode"] = m.mode.ToString(),
            ["attachRibbonsToTransform"] = m.attachRibbonsToTransform,

            ["ribbonCount"] = m.ribbonCount,
            ["splitSubEmitterRibbons"] = m.splitSubEmitterRibbons,
            ["textureScale"] = SerializeVector2(m.textureScale),

            ["enabled"] = m.enabled,
            ["ratio"] = m.ratio,
            ["lifetime"] = SerializeMinMaxCurve(m.lifetime),
            ["lifetimeMultiplier"] = m.lifetimeMultiplier,
            ["minVertexDistance"] = m.minVertexDistance,
            ["textureMode"] = m.textureMode.ToString(),
            ["worldSpace"] = m.worldSpace,
            ["dieWithParticles"] = m.dieWithParticles,
            ["sizeAffectsWidth"] = m.sizeAffectsWidth,
            ["sizeAffectsLifetime"] = m.sizeAffectsLifetime,
            ["inheritParticleColor"] = m.inheritParticleColor,
            ["colorOverLifetime"] = SerializeMinMaxGradient(m.colorOverLifetime),
            ["widthOverTrail"] = SerializeMinMaxCurve(m.widthOverTrail),
            ["widthOverTrailMultiplier"] = m.widthOverTrailMultiplier,
            ["colorOverTrail"] = SerializeMinMaxGradient(m.colorOverTrail)
        };
    }

    private static JObject SerializeCustomDataModule(ParticleSystem.CustomDataModule m)
    {
        return new JObject
        {
            ["enabled"] = m.enabled
        };
    }

    private static void DeserializeMainModule(ParticleSystem.MainModule m, JToken data)
    {
        m.duration = data["duration"].Value<float>();
        m.loop = data["loop"].Value<bool>();
        m.prewarm = data["prewarm"].Value<bool>();
        m.startDelay = DeserializeMinMaxCurve(data["startDelay"]);
        m.startDelayMultiplier = data["startDelayMultiplier"].Value<float>();
        m.startLifetime = DeserializeMinMaxCurve(data["startLifetime"]);
        m.startLifetimeMultiplier = data["startLifetimeMultiplier"].Value<float>();
        m.startSpeed = DeserializeMinMaxCurve(data["startSpeed"]);
        m.startSpeedMultiplier = data["startSpeedMultiplier"].Value<float>();
        m.startSize3D = data["startSize3D"].Value<bool>();
        m.startSize = DeserializeMinMaxCurve(data["startSize"]);
        m.startSizeMultiplier = data["startSizeMultiplier"].Value<float>();
        m.startSizeX = DeserializeMinMaxCurve(data["startSizeX"]);
        m.startSizeXMultiplier = data["startSizeXMultiplier"].Value<float>();
        m.startSizeY = DeserializeMinMaxCurve(data["startSizeY"]);
        m.startSizeYMultiplier = data["startSizeYMultiplier"].Value<float>();
        m.startSizeZ = DeserializeMinMaxCurve(data["startSizeZ"]);
        m.startSizeZMultiplier = data["startSizeZMultiplier"].Value<float>();
        m.startRotation3D = data["startRotation3D"].Value<bool>();
        m.startRotation = DeserializeMinMaxCurve(data["startRotation"]);
        m.startRotationMultiplier = data["startRotationMultiplier"].Value<float>();
        m.startRotationX = DeserializeMinMaxCurve(data["startRotationX"]);
        m.startRotationXMultiplier = data["startRotationXMultiplier"].Value<float>();
        m.startRotationY = DeserializeMinMaxCurve(data["startRotationY"]);
        m.startRotationYMultiplier = data["startRotationYMultiplier"].Value<float>();
        m.startRotationZ = DeserializeMinMaxCurve(data["startRotationZ"]);
        m.startRotationZMultiplier = data["startRotationZMultiplier"].Value<float>();
        m.flipRotation = data["flipRotation"].Value<float>();
        m.startColor = DeserializeMinMaxGradient(data["startColor"]);
        m.gravityModifier = DeserializeMinMaxCurve(data["gravityModifier"]);
        m.gravityModifierMultiplier = data["gravityModifierMultiplier"].Value<float>();
        m.simulationSpace = (ParticleSystemSimulationSpace)Enum.Parse(typeof(ParticleSystemSimulationSpace), data["simulationSpace"].Value<string>());
        m.simulationSpeed = data["simulationSpeed"].Value<float>();
        m.useUnscaledTime = data["useUnscaledTime"].Value<bool>();
        m.scalingMode = (ParticleSystemScalingMode)Enum.Parse(typeof(ParticleSystemScalingMode), data["scalingMode"].Value<string>());
        m.playOnAwake = data["playOnAwake"].Value<bool>();
        m.emitterVelocityMode = (ParticleSystemEmitterVelocityMode)Enum.Parse(typeof(ParticleSystemEmitterVelocityMode), data["emitterVelocityMode"].Value<string>());
        m.maxParticles = data["maxParticles"].Value<int>();
        m.stopAction = (ParticleSystemStopAction)Enum.Parse(typeof(ParticleSystemStopAction), data["stopAction"].Value<string>());
        m.cullingMode = (ParticleSystemCullingMode)Enum.Parse(typeof(ParticleSystemCullingMode), data["cullingMode"].Value<string>());
        m.ringBufferMode = (ParticleSystemRingBufferMode)Enum.Parse(typeof(ParticleSystemRingBufferMode), data["ringBufferMode"].Value<string>());
        m.ringBufferLoopRange = DeserializeVector2(data["ringBufferLoopRange"]);
    }

    private static void DeserializeEmissionModule(ParticleSystem.EmissionModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.rateOverTime = DeserializeMinMaxCurve(data["rateOverTime"]);
        m.rateOverTimeMultiplier = data["rateOverTimeMultiplier"].Value<float>();
        m.rateOverDistance = DeserializeMinMaxCurve(data["rateOverDistance"]);
        m.rateOverDistanceMultiplier = data["rateOverDistanceMultiplier"].Value<float>();

        var bursts = data["bursts"] as JArray;
        if (bursts != null && bursts.Count > 0)
        {
            var burstArray = new ParticleSystem.Burst[bursts.Count];

            for (int i = 0; i < bursts.Count; i++)
            {
                var burstData = bursts[i];
                burstArray[i] = new ParticleSystem.Burst
                {
                    time = burstData["time"].Value<float>(),
                    count = DeserializeMinMaxCurve(burstData["count"]),
                    cycleCount = burstData["cycleCount"].Value<int>(),
                    repeatInterval = burstData["repeatInterval"].Value<float>(),
                    probability = burstData["probability"].Value<float>()
                };
            }
            m.SetBursts(burstArray);
        }
        else
        {
            m.SetBursts(new ParticleSystem.Burst[0]);
        }
    }

    private static void DeserializeShapeModule(ParticleSystem.ShapeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.shapeType = (ParticleSystemShapeType)Enum.Parse(typeof(ParticleSystemShapeType), data["shapeType"].Value<string>());
        m.angle = data["angle"].Value<float>();
        m.radius = data["radius"].Value<float>();
        m.radiusMode = (ParticleSystemShapeMultiModeValue)Enum.Parse(typeof(ParticleSystemShapeMultiModeValue), data["radiusMode"].Value<string>());
        m.radiusSpread = data["radiusSpread"].Value<float>();
        m.radiusSpeed = DeserializeMinMaxCurve(data["radiusSpeed"]);
        m.radiusSpeedMultiplier = data["radiusSpeedMultiplier"].Value<float>();
        m.radiusThickness = data["radiusThickness"].Value<float>();
        m.arc = data["arc"].Value<float>();
        m.arcMode = (ParticleSystemShapeMultiModeValue)Enum.Parse(typeof(ParticleSystemShapeMultiModeValue), data["arcMode"].Value<string>());
        m.arcSpread = data["arcSpread"].Value<float>();
        m.arcSpeed = DeserializeMinMaxCurve(data["arcSpeed"]);
        m.arcSpeedMultiplier = data["arcSpeedMultiplier"].Value<float>();
        m.donutRadius = data["donutRadius"].Value<float>();
        m.length = data["length"].Value<float>();
        m.boxThickness = DeserializeVector3(data["boxThickness"]);
        m.meshShapeType = (ParticleSystemMeshShapeType)Enum.Parse(typeof(ParticleSystemMeshShapeType), data["meshShapeType"].Value<string>());
        m.meshSpawnMode = (ParticleSystemShapeMultiModeValue)Enum.Parse(typeof(ParticleSystemShapeMultiModeValue), data["meshSpawnMode"].Value<string>());
        m.meshSpawnSpread = data["meshSpawnSpread"].Value<float>();
        m.meshSpawnSpeed = DeserializeMinMaxCurve(data["meshSpawnSpeed"]);
        m.meshSpawnSpeedMultiplier = data["meshSpawnSpeedMultiplier"].Value<float>();
        m.useMeshMaterialIndex = data["useMeshMaterialIndex"].Value<bool>();
        m.meshMaterialIndex = data["meshMaterialIndex"].Value<int>();
        m.useMeshColors = data["useMeshColors"].Value<bool>();
        m.normalOffset = data["normalOffset"].Value<float>();
        m.position = DeserializeVector3(data["position"]);
        m.rotation = DeserializeVector3(data["rotation"]);
        m.scale = DeserializeVector3(data["scale"]);

        m.textureClipChannel = (ParticleSystemShapeTextureChannel)Enum.Parse(typeof(ParticleSystemShapeTextureChannel), data["textureClipChannel"].Value<string>());
        m.textureClipThreshold = data["textureClipThreshold"].Value<float>();
        m.textureColorAffectsParticles = data["textureColorAffectsParticles"].Value<bool>();
        m.textureAlphaAffectsParticles = data["textureAlphaAffectsParticles"].Value<bool>();
        m.textureBilinearFiltering = data["textureBilinearFiltering"].Value<bool>();
        m.textureUVChannel = data["textureUVChannel"].Value<int>();
        m.alignToDirection = data["alignToDirection"].Value<bool>();
        m.randomDirectionAmount = data["randomDirectionAmount"].Value<float>();
        m.sphericalDirectionAmount = data["sphericalDirectionAmount"].Value<float>();
        m.randomPositionAmount = data["randomPositionAmount"].Value<float>();
    }

    private static void DeserializeVelocityOverLifetimeModule(ParticleSystem.VelocityOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.orbitalX = DeserializeMinMaxCurve(data["orbitalX"]);
        m.orbitalY = DeserializeMinMaxCurve(data["orbitalY"]);
        m.orbitalZ = DeserializeMinMaxCurve(data["orbitalZ"]);
        m.orbitalXMultiplier = data["orbitalXMultiplier"].Value<float>();
        m.orbitalYMultiplier = data["orbitalYMultiplier"].Value<float>();
        m.orbitalZMultiplier = data["orbitalZMultiplier"].Value<float>();
        m.orbitalOffsetX = DeserializeMinMaxCurve(data["orbitalOffsetX"]);
        m.orbitalOffsetY = DeserializeMinMaxCurve(data["orbitalOffsetY"]);
        m.orbitalOffsetZ = DeserializeMinMaxCurve(data["orbitalOffsetZ"]);
        m.orbitalOffsetXMultiplier = data["orbitalOffsetXMultiplier"].Value<float>();
        m.orbitalOffsetYMultiplier = data["orbitalOffsetYMultiplier"].Value<float>();
        m.orbitalOffsetZMultiplier = data["orbitalOffsetZMultiplier"].Value<float>();
        m.radial = DeserializeMinMaxCurve(data["radial"]);
        m.radialMultiplier = data["radialMultiplier"].Value<float>();
        m.speedModifier = DeserializeMinMaxCurve(data["speedModifier"]);
        m.speedModifierMultiplier = data["speedModifierMultiplier"].Value<float>();
        m.space = (ParticleSystemSimulationSpace)Enum.Parse(typeof(ParticleSystemSimulationSpace), data["space"].Value<string>());
    }

    private static void DeserializeLimitVelocityOverLifetimeModule(ParticleSystem.LimitVelocityOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.limitX = DeserializeMinMaxCurve(data["limitX"]);
        m.limitXMultiplier = data["limitXMultiplier"].Value<float>();
        m.limitY = DeserializeMinMaxCurve(data["limitY"]);
        m.limitYMultiplier = data["limitYMultiplier"].Value<float>();
        m.limitZ = DeserializeMinMaxCurve(data["limitZ"]);
        m.limitZMultiplier = data["limitZMultiplier"].Value<float>();
        m.limit = DeserializeMinMaxCurve(data["limit"]);
        m.limitMultiplier = data["limitMultiplier"].Value<float>();
        m.dampen = data["dampen"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
        m.space = (ParticleSystemSimulationSpace)Enum.Parse(typeof(ParticleSystemSimulationSpace), data["space"].Value<string>());
        m.drag = DeserializeMinMaxCurve(data["drag"]);
        m.dragMultiplier = data["dragMultiplier"].Value<float>();
        m.multiplyDragByParticleSize = data["multiplyDragByParticleSize"].Value<bool>();
        m.multiplyDragByParticleVelocity = data["multiplyDragByParticleVelocity"].Value<bool>();
    }

    private static void DeserializeLifetimeByEmitterSpeedModule(ParticleSystem.LifetimeByEmitterSpeedModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.curve = DeserializeMinMaxCurve(data["curve"]);
        m.curveMultiplier = data["curveMultiplier"].Value<float>();
        m.range = DeserializeVector2(data["range"]);
    }

    private static void DeserializeInheritVelocityModule(ParticleSystem.InheritVelocityModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.mode = (ParticleSystemInheritVelocityMode)Enum.Parse(typeof(ParticleSystemInheritVelocityMode), data["mode"].Value<string>());
        m.curve = DeserializeMinMaxCurve(data["curve"]);
        m.curveMultiplier = data["curveMultiplier"].Value<float>();
    }

    private static void DeserializeForceOverLifetimeModule(ParticleSystem.ForceOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.space = (ParticleSystemSimulationSpace)Enum.Parse(typeof(ParticleSystemSimulationSpace), data["space"].Value<string>());
        m.randomized = data["randomized"].Value<bool>();
    }

    private static void DeserializeColorOverLifetimeModule(ParticleSystem.ColorOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.color = DeserializeMinMaxGradient(data["color"]);
    }

    private static void DeserializeColorBySpeedModule(ParticleSystem.ColorBySpeedModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.color = DeserializeMinMaxGradient(data["color"]);
        m.range = DeserializeVector2(data["range"]);
    }

    private static void DeserializeSizeOverLifetimeModule(ParticleSystem.SizeOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.size = DeserializeMinMaxCurve(data["size"]);
        m.sizeMultiplier = data["sizeMultiplier"].Value<float>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
    }

    private static void DeserializeSizeBySpeedModule(ParticleSystem.SizeBySpeedModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.size = DeserializeMinMaxCurve(data["size"]);
        m.sizeMultiplier = data["sizeMultiplier"].Value<float>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
        m.range = DeserializeVector2(data["range"]);
    }

    private static void DeserializeRotationOverLifetimeModule(ParticleSystem.RotationOverLifetimeModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
    }

    private static void DeserializeRotationBySpeedModule(ParticleSystem.RotationBySpeedModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.x = DeserializeMinMaxCurve(data["x"]);
        m.xMultiplier = data["xMultiplier"].Value<float>();
        m.y = DeserializeMinMaxCurve(data["y"]);
        m.yMultiplier = data["yMultiplier"].Value<float>();
        m.z = DeserializeMinMaxCurve(data["z"]);
        m.zMultiplier = data["zMultiplier"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
        m.range = DeserializeVector2(data["range"]);
    }

    private static void DeserializeExternalForcesModule(ParticleSystem.ExternalForcesModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.multiplier = data["multiplier"].Value<float>();
    }

    private static void DeserializeNoiseModule(ParticleSystem.NoiseModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.strengthX = DeserializeMinMaxCurve(data["strengthX"]);
        m.strengthXMultiplier = data["strengthXMultiplier"].Value<float>();
        m.strengthY = DeserializeMinMaxCurve(data["strengthY"]);
        m.strengthYMultiplier = data["strengthYMultiplier"].Value<float>();
        m.strengthZ = DeserializeMinMaxCurve(data["strengthZ"]);
        m.strengthZMultiplier = data["strengthZMultiplier"].Value<float>();
        m.separateAxes = data["separateAxes"].Value<bool>();
        m.frequency = data["frequency"].Value<float>();
        m.damping = data["damping"].Value<bool>();
        m.octaveCount = data["octaveCount"].Value<int>();
        m.octaveMultiplier = data["octaveMultiplier"].Value<float>();
        m.octaveScale = data["octaveScale"].Value<float>();
        m.quality = (ParticleSystemNoiseQuality)Enum.Parse(typeof(ParticleSystemNoiseQuality), data["quality"].Value<string>());
        m.scrollSpeed = DeserializeMinMaxCurve(data["scrollSpeed"]);
        m.scrollSpeedMultiplier = data["scrollSpeedMultiplier"].Value<float>();
        m.remapEnabled = data["remapEnabled"].Value<bool>();
        m.remapX = DeserializeMinMaxCurve(data["remapX"]);
        m.remapXMultiplier = data["remapXMultiplier"].Value<float>();
        m.remapY = DeserializeMinMaxCurve(data["remapY"]);
        m.remapYMultiplier = data["remapYMultiplier"].Value<float>();
        m.remapZ = DeserializeMinMaxCurve(data["remapZ"]);
        m.remapZMultiplier = data["remapZMultiplier"].Value<float>();
        m.positionAmount = DeserializeMinMaxCurve(data["positionAmount"]);
        m.rotationAmount = DeserializeMinMaxCurve(data["rotationAmount"]);
        m.sizeAmount = DeserializeMinMaxCurve(data["sizeAmount"]);
    }

    private static void DeserializeCollisionModule(ParticleSystem.CollisionModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.type = (ParticleSystemCollisionType)Enum.Parse(typeof(ParticleSystemCollisionType), data["type"].Value<string>());
        m.mode = (ParticleSystemCollisionMode)Enum.Parse(typeof(ParticleSystemCollisionMode), data["mode"].Value<string>());
        m.dampen = DeserializeMinMaxCurve(data["dampen"]);
        m.dampenMultiplier = data["dampenMultiplier"].Value<float>();
        m.bounce = DeserializeMinMaxCurve(data["bounce"]);
        m.bounceMultiplier = data["bounceMultiplier"].Value<float>();
        m.lifetimeLoss = DeserializeMinMaxCurve(data["lifetimeLoss"]);
        m.lifetimeLossMultiplier = data["lifetimeLossMultiplier"].Value<float>();
        m.minKillSpeed = data["minKillSpeed"].Value<float>();
        m.maxKillSpeed = data["maxKillSpeed"].Value<float>();
        m.collidesWith = data["collidesWith"].Value<int>();
        m.enableDynamicColliders = data["enableDynamicColliders"].Value<bool>();
        m.maxCollisionShapes = data["maxCollisionShapes"].Value<int>();
        m.quality = (ParticleSystemCollisionQuality)Enum.Parse(typeof(ParticleSystemCollisionQuality), data["quality"].Value<string>());
        m.voxelSize = data["voxelSize"].Value<float>();
        m.radiusScale = data["radiusScale"].Value<float>();
        m.sendCollisionMessages = data["sendCollisionMessages"].Value<bool>();
        m.colliderForce = data["colliderForce"].Value<float>();
        m.multiplyColliderForceByCollisionAngle = data["multiplyColliderForceByCollisionAngle"].Value<bool>();
        m.multiplyColliderForceByParticleSpeed = data["multiplyColliderForceByParticleSpeed"].Value<bool>();
        m.multiplyColliderForceByParticleSize = data["multiplyColliderForceByParticleSize"].Value<bool>();
    }

    private static void DeserializeTriggerModule(ParticleSystem.TriggerModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.inside = (ParticleSystemOverlapAction)Enum.Parse(typeof(ParticleSystemOverlapAction), data["inside"].Value<string>());
        m.outside = (ParticleSystemOverlapAction)Enum.Parse(typeof(ParticleSystemOverlapAction), data["outside"].Value<string>());
        m.enter = (ParticleSystemOverlapAction)Enum.Parse(typeof(ParticleSystemOverlapAction), data["enter"].Value<string>());
        m.exit = (ParticleSystemOverlapAction)Enum.Parse(typeof(ParticleSystemOverlapAction), data["exit"].Value<string>());
        m.radiusScale = data["radiusScale"].Value<float>();
    }

    private static void DeserializeSubEmittersModule(ParticleSystem.SubEmittersModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
    }

    private static void DeserializeTextureSheetAnimationModule(ParticleSystem.TextureSheetAnimationModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.numTilesX = data["numTilesX"].Value<int>();
        m.numTilesY = data["numTilesY"].Value<int>();
        m.animation = (ParticleSystemAnimationType)Enum.Parse(typeof(ParticleSystemAnimationType), data["animation"].Value<string>());
        m.rowMode = (ParticleSystemAnimationRowMode)Enum.Parse(typeof(ParticleSystemAnimationRowMode), data["rowMode"].Value<string>());
        m.frameOverTime = DeserializeMinMaxCurve(data["frameOverTime"]);
        m.frameOverTimeMultiplier = data["frameOverTimeMultiplier"].Value<float>();
        m.startFrame = DeserializeMinMaxCurve(data["startFrame"]);
        m.startFrameMultiplier = data["startFrameMultiplier"].Value<float>();
        m.cycleCount = data["cycleCount"].Value<int>();
        m.rowIndex = data["rowIndex"].Value<int>();
    }

    private static void DeserializeLightsModule(ParticleSystem.LightsModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
        m.ratio = data["ratio"].Value<float>();
        m.useRandomDistribution = data["useRandomDistribution"].Value<bool>();
        m.useParticleColor = data["useParticleColor"].Value<bool>();
        m.sizeAffectsRange = data["sizeAffectsRange"].Value<bool>();
        m.alphaAffectsIntensity = data["alphaAffectsIntensity"].Value<bool>();
        m.range = DeserializeMinMaxCurve(data["range"]);
        m.rangeMultiplier = data["rangeMultiplier"].Value<float>();
        m.intensity = DeserializeMinMaxCurve(data["intensity"]);
        m.intensityMultiplier = data["intensityMultiplier"].Value<float>();
        m.maxLights = data["maxLights"].Value<int>();
    }

    private static void DeserializeTrailsModule(ParticleSystem.TrailModule m, JToken data)
    {
        m.mode = (ParticleSystemTrailMode)Enum.Parse(typeof(ParticleSystemTrailMode), data["mode"].Value<string>());

        m.attachRibbonsToTransform = data["attachRibbonsToTransform"].Value<bool>();
        m.ribbonCount = data["ribbonCount"].Value<int>();
        m.splitSubEmitterRibbons = data["splitSubEmitterRibbons"].Value<bool>();
        m.textureScale = DeserializeVector2(data["textureScale"]);
        m.enabled = data["enabled"].Value<bool>();
        m.ratio = data["ratio"].Value<float>();
        m.lifetime = DeserializeMinMaxCurve(data["lifetime"]);
        m.lifetimeMultiplier = data["lifetimeMultiplier"].Value<float>();
        m.minVertexDistance = data["minVertexDistance"].Value<float>();
        m.textureMode = (ParticleSystemTrailTextureMode)Enum.Parse(typeof(ParticleSystemTrailTextureMode), data["textureMode"].Value<string>());
        m.worldSpace = data["worldSpace"].Value<bool>();
        m.dieWithParticles = data["dieWithParticles"].Value<bool>();
        m.sizeAffectsWidth = data["sizeAffectsWidth"].Value<bool>();
        m.sizeAffectsLifetime = data["sizeAffectsLifetime"].Value<bool>();
        m.inheritParticleColor = data["inheritParticleColor"].Value<bool>();
        m.colorOverLifetime = DeserializeMinMaxGradient(data["colorOverLifetime"]);
        m.widthOverTrail = DeserializeMinMaxCurve(data["widthOverTrail"]);
        m.widthOverTrailMultiplier = data["widthOverTrailMultiplier"].Value<float>();
        m.colorOverTrail = DeserializeMinMaxGradient(data["colorOverTrail"]);
    }

    private static void DeserializeCustomDataModule(ParticleSystem.CustomDataModule m, JToken data)
    {
        m.enabled = data["enabled"].Value<bool>();
    }

    private static JObject SerializeRenderer(ParticleSystemRenderer r)
    {
        var obj = new JObject
        {
            ["renderMode"] = r.renderMode.ToString(),
            ["sortMode"] = r.sortMode.ToString(),
            ["sortingFudge"] = r.sortingFudge,
            ["normalDirection"] = r.normalDirection,
            ["shadowCastingMode"] = r.shadowCastingMode.ToString(),
            ["receiveShadows"] = r.receiveShadows,
            ["shadowBias"] = r.shadowBias,
            ["motionVectorGenerationMode"] = r.motionVectorGenerationMode.ToString(),

            ["lightProbeUsage"] = r.lightProbeUsage.ToString(),
            ["reflectionProbeUsage"] = r.reflectionProbeUsage.ToString(),
            ["allowOcclusionWhenDynamic"] = r.allowOcclusionWhenDynamic,

            ["alignment"] = r.alignment.ToString(),
            ["flip"] = SerializeVector3(r.flip),
            ["pivot"] = SerializeVector3(r.pivot),

            ["meshDistribution"] = r.meshDistribution.ToString(),

            ["allowRoll"] = r.allowRoll,
            ["freeformStretching"] = r.freeformStretching,
            ["rotateWithStretchDirection"] = r.rotateWithStretchDirection,

            ["cameraVelocityScale"] = r.cameraVelocityScale,
            ["velocityScale"] = r.velocityScale,
            ["lengthScale"] = r.lengthScale,
        };

        if (r.renderMode == ParticleSystemRenderMode.Mesh)
        {
            obj["minParticleSize"] = r.minParticleSize;
            obj["maxParticleSize"] = r.maxParticleSize;
        }

        var streams = new JArray();
        var streamsList = new List<ParticleSystemVertexStream>();
        r.GetActiveVertexStreams(streamsList);
        foreach (var stream in streamsList)
        {
            streams.Add(stream.ToString());
        }
        obj["activeVertexStreams"] = streams;

        return obj;
    }

    private static void DeserializeRenderer(ParticleSystemRenderer r, JToken data)
    {
        r.renderMode =
            (ParticleSystemRenderMode)Enum.Parse(
                typeof(ParticleSystemRenderMode),
                data["renderMode"].Value<string>());

        r.sortMode =
            (ParticleSystemSortMode)Enum.Parse(
                typeof(ParticleSystemSortMode),
                data["sortMode"].Value<string>());

        r.sortingFudge = data["sortingFudge"].Value<float>();
        r.normalDirection = data["normalDirection"].Value<float>();

        r.shadowCastingMode =
            (UnityEngine.Rendering.ShadowCastingMode)Enum.Parse(
                typeof(UnityEngine.Rendering.ShadowCastingMode),
                data["shadowCastingMode"].Value<string>());

        r.receiveShadows = data["receiveShadows"].Value<bool>();
        r.shadowBias = data["shadowBias"].Value<float>();

        r.motionVectorGenerationMode =
            (MotionVectorGenerationMode)Enum.Parse(
                typeof(MotionVectorGenerationMode),
                data["motionVectorGenerationMode"].Value<string>());

        r.lightProbeUsage =
            (UnityEngine.Rendering.LightProbeUsage)Enum.Parse(
                typeof(UnityEngine.Rendering.LightProbeUsage),
                data["lightProbeUsage"].Value<string>());

        r.reflectionProbeUsage =
            (UnityEngine.Rendering.ReflectionProbeUsage)Enum.Parse(
                typeof(UnityEngine.Rendering.ReflectionProbeUsage),
                data["reflectionProbeUsage"].Value<string>());

        r.allowOcclusionWhenDynamic =
            data["allowOcclusionWhenDynamic"].Value<bool>();

        r.alignment =
            (ParticleSystemRenderSpace)Enum.Parse(
                typeof(ParticleSystemRenderSpace),
                data["alignment"].Value<string>());

        r.flip = DeserializeVector3(data["flip"]);
        r.pivot = DeserializeVector3(data["pivot"]);

        r.meshDistribution =
            (ParticleSystemMeshDistribution)Enum.Parse(
                typeof(ParticleSystemMeshDistribution),
                data["meshDistribution"].Value<string>());

        r.allowRoll = data["allowRoll"].Value<bool>();
        r.freeformStretching = data["freeformStretching"].Value<bool>();
        r.rotateWithStretchDirection =
            data["rotateWithStretchDirection"].Value<bool>();

        r.cameraVelocityScale = data["cameraVelocityScale"].Value<float>();
        r.velocityScale = data["velocityScale"].Value<float>();
        r.lengthScale = data["lengthScale"].Value<float>();

        if (data["minParticleSize"] != null)
            r.minParticleSize = data["minParticleSize"].Value<float>();

        if (data["maxParticleSize"] != null)
            r.maxParticleSize = data["maxParticleSize"].Value<float>();

        if (data["activeVertexStreams"] is JArray streamsData)
        {
            var streams = new List<ParticleSystemVertexStream>(streamsData.Count);

            foreach (var s in streamsData)
            {
                streams.Add(
                    (ParticleSystemVertexStream)Enum.Parse(
                        typeof(ParticleSystemVertexStream),
                        s.Value<string>()));
            }

            r.SetActiveVertexStreams(streams);
        }
    }


    // Helper serialization methods
    private static JObject SerializeMinMaxCurve(ParticleSystem.MinMaxCurve curve)
    {
        var obj = new JObject
        {
            ["mode"] = curve.mode.ToString(),
            ["constant"] = curve.constant,
            ["constantMin"] = curve.constantMin,
            ["constantMax"] = curve.constantMax,
            ["curveMultiplier"] = curve.curveMultiplier
        };

        if (curve.curve != null)
            obj["curve"] = SerializeAnimationCurve(curve.curve);
        if (curve.curveMin != null)
            obj["curveMin"] = SerializeAnimationCurve(curve.curveMin);
        if (curve.curveMax != null)
            obj["curveMax"] = SerializeAnimationCurve(curve.curveMax);

        return obj;
    }

    private static ParticleSystem.MinMaxCurve DeserializeMinMaxCurve(JToken data)
    {
        var mode = (ParticleSystemCurveMode)Enum.Parse(typeof(ParticleSystemCurveMode), data["mode"].Value<string>());
        var curve = new ParticleSystem.MinMaxCurve();
        curve.mode = mode;
        curve.constant = data["constant"].Value<float>();
        curve.constantMin = data["constantMin"].Value<float>();
        curve.constantMax = data["constantMax"].Value<float>();
        curve.curveMultiplier = data["curveMultiplier"].Value<float>();

        if (data["curve"] != null)
            curve.curve = DeserializeAnimationCurve(data["curve"]);
        if (data["curveMin"] != null)
            curve.curveMin = DeserializeAnimationCurve(data["curveMin"]);
        if (data["curveMax"] != null)
            curve.curveMax = DeserializeAnimationCurve(data["curveMax"]);

        return curve;
    }

    private static JObject SerializeMinMaxGradient(ParticleSystem.MinMaxGradient gradient)
    {
        var obj = new JObject
        {
            ["mode"] = gradient.mode.ToString(),
            ["color"] = SerializeColor(gradient.color),
            ["colorMin"] = SerializeColor(gradient.colorMin),
            ["colorMax"] = SerializeColor(gradient.colorMax)
        };

        if (gradient.gradient != null)
            obj["gradient"] = SerializeGradient(gradient.gradient);
        if (gradient.gradientMin != null)
            obj["gradientMin"] = SerializeGradient(gradient.gradientMin);
        if (gradient.gradientMax != null)
            obj["gradientMax"] = SerializeGradient(gradient.gradientMax);

        return obj;
    }

    private static ParticleSystem.MinMaxGradient DeserializeMinMaxGradient(JToken data)
    {
        var mode = (ParticleSystemGradientMode)Enum.Parse(typeof(ParticleSystemGradientMode), data["mode"].Value<string>());
        var gradient = new ParticleSystem.MinMaxGradient();
        gradient.mode = mode;
        gradient.color = DeserializeColor(data["color"]);
        gradient.colorMin = DeserializeColor(data["colorMin"]);
        gradient.colorMax = DeserializeColor(data["colorMax"]);

        if (data["gradient"] != null)
            gradient.gradient = DeserializeGradient(data["gradient"]);
        if (data["gradientMin"] != null)
            gradient.gradientMin = DeserializeGradient(data["gradientMin"]);
        if (data["gradientMax"] != null)
            gradient.gradientMax = DeserializeGradient(data["gradientMax"]);

        return gradient;
    }

    private static JObject SerializeAnimationCurve(AnimationCurve curve)
    {
        var keys = new JArray();
        foreach (var key in curve.keys)
        {
            keys.Add(new JObject
            {
                ["time"] = key.time,
                ["value"] = key.value,
                ["inTangent"] = key.inTangent,
                ["outTangent"] = key.outTangent,
                ["inWeight"] = key.inWeight,
                ["outWeight"] = key.outWeight,
                ["weightedMode"] = key.weightedMode.ToString()
            });
        }

        return new JObject
        {
            ["keys"] = keys,
            ["preWrapMode"] = curve.preWrapMode.ToString(),
            ["postWrapMode"] = curve.postWrapMode.ToString()
        };
    }

    private static AnimationCurve DeserializeAnimationCurve(JToken data)
    {
        var keys = data["keys"] as JArray;
        var keyframes = new Keyframe[keys.Count];

        for (int i = 0; i < keys.Count; i++)
        {
            var keyData = keys[i];
            keyframes[i] = new Keyframe
            {
                time = keyData["time"].Value<float>(),
                value = keyData["value"].Value<float>(),
                inTangent = keyData["inTangent"].Value<float>(),
                outTangent = keyData["outTangent"].Value<float>(),
                inWeight = keyData["inWeight"].Value<float>(),
                outWeight = keyData["outWeight"].Value<float>(),
                weightedMode = (WeightedMode)Enum.Parse(typeof(WeightedMode), keyData["weightedMode"].Value<string>())
            };
        }

        var curve = new AnimationCurve(keyframes);
        curve.preWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), data["preWrapMode"].Value<string>());
        curve.postWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), data["postWrapMode"].Value<string>());

        return curve;
    }

    private static JObject SerializeGradient(Gradient gradient)
    {
        var colorKeys = new JArray();
        foreach (var key in gradient.colorKeys)
        {
            colorKeys.Add(new JObject
            {
                ["color"] = SerializeColor(key.color),
                ["time"] = key.time
            });
        }

        var alphaKeys = new JArray();
        foreach (var key in gradient.alphaKeys)
        {
            alphaKeys.Add(new JObject
            {
                ["alpha"] = key.alpha,
                ["time"] = key.time
            });
        }

        return new JObject
        {
            ["colorKeys"] = colorKeys,
            ["alphaKeys"] = alphaKeys,
            ["mode"] = gradient.mode.ToString()
        };
    }

    private static Gradient DeserializeGradient(JToken data)
    {
        var gradient = new Gradient();

        var colorKeysData = data["colorKeys"] as JArray;
        var colorKeys = new GradientColorKey[colorKeysData.Count];
        for (int i = 0; i < colorKeysData.Count; i++)
        {
            colorKeys[i] = new GradientColorKey
            {
                color = DeserializeColor(colorKeysData[i]["color"]),
                time = colorKeysData[i]["time"].Value<float>()
            };
        }

        var alphaKeysData = data["alphaKeys"] as JArray;
        var alphaKeys = new GradientAlphaKey[alphaKeysData.Count];
        for (int i = 0; i < alphaKeysData.Count; i++)
        {
            alphaKeys[i] = new GradientAlphaKey
            {
                alpha = alphaKeysData[i]["alpha"].Value<float>(),
                time = alphaKeysData[i]["time"].Value<float>()
            };
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        gradient.mode = (GradientMode)Enum.Parse(typeof(GradientMode), data["mode"].Value<string>());

        return gradient;
    }

    private static JObject SerializeColor(Color color)
    {
        return new JObject
        {
            ["r"] = color.r,
            ["g"] = color.g,
            ["b"] = color.b,
            ["a"] = color.a
        };
    }

    private static Color DeserializeColor(JToken data)
    {
        return new Color(
            data["r"].Value<float>(),
            data["g"].Value<float>(),
            data["b"].Value<float>(),
            data["a"].Value<float>()
        );
    }

    private static JObject SerializeVector3(Vector3 v)
    {
        return new JObject
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z
        };
    }

    private static Vector3 DeserializeVector3(JToken data)
    {
        return new Vector3(
            data["x"].Value<float>(),
            data["y"].Value<float>(),
            data["z"].Value<float>()
        );
    }

    private static JObject SerializeVector2(Vector2 v)
    {
        return new JObject
        {
            ["x"] = v.x,
            ["y"] = v.y
        };
    }

    private static Vector2 DeserializeVector2(JToken data)
    {
        return new Vector2(
            data["x"].Value<float>(),
            data["y"].Value<float>()
        );
    }

    private static string defaultParticleJson =
        "{\r\n  \"renderer\": {\r\n    \"renderMode\": \"Billboard\",\r\n    \"sortMode\": \"Distance\",\r\n    \"sortingFudge\": 0.0,\r\n    \"normalDirection\": 1.0,\r\n    \"shadowCastingMode\": \"Off\",\r\n    \"receiveShadows\": false,\r\n    \"shadowBias\": 0.0,\r\n    \"motionVectorGenerationMode\": \"Object\",\r\n    \"lightProbeUsage\": \"Off\",\r\n    \"reflectionProbeUsage\": \"Off\",\r\n    \"allowOcclusionWhenDynamic\": true,\r\n    \"alignment\": \"View\",\r\n    \"flip\": {\r\n      \"x\": 0.0,\r\n      \"y\": 0.0,\r\n      \"z\": 0.0\r\n    },\r\n    \"pivot\": {\r\n      \"x\": 0.0,\r\n      \"y\": 0.0,\r\n      \"z\": 0.0\r\n    },\r\n    \"meshDistribution\": \"UniformRandom\",\r\n    \"allowRoll\": true,\r\n    \"freeformStretching\": false,\r\n    \"rotateWithStretchDirection\": true,\r\n    \"cameraVelocityScale\": 0.0,\r\n    \"velocityScale\": 0.0,\r\n    \"lengthScale\": 2.0,\r\n    \"activeVertexStreams\": [\r\n      \"Position\",\r\n      \"Normal\",\r\n      \"Color\",\r\n      \"UV\"\r\n    ]\r\n  },\r\n  \"main\": {\r\n    \"duration\": 0.05,\r\n    \"loop\": false,\r\n    \"prewarm\": false,\r\n    \"startDelay\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startDelayMultiplier\": 0.0,\r\n    \"startLifetime\": {\r\n      \"mode\": \"TwoConstants\",\r\n      \"constant\": 0.35,\r\n      \"constantMin\": 0.01,\r\n      \"constantMax\": 0.35,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startLifetimeMultiplier\": 0.35,\r\n    \"startSpeed\": {\r\n      \"mode\": \"TwoConstants\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startSpeedMultiplier\": 0.0,\r\n    \"startSize3D\": false,\r\n    \"startSize\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startSizeMultiplier\": 1.0,\r\n    \"startSizeX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startSizeXMultiplier\": 1.0,\r\n    \"startSizeY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startSizeYMultiplier\": 1.0,\r\n    \"startSizeZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startSizeZMultiplier\": 1.0,\r\n    \"startRotation3D\": false,\r\n    \"startRotation\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startRotationMultiplier\": 0.0,\r\n    \"startRotationX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startRotationXMultiplier\": 0.0,\r\n    \"startRotationY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startRotationYMultiplier\": 0.0,\r\n    \"startRotationZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startRotationZMultiplier\": 0.0,\r\n    \"flipRotation\": 0.0,\r\n    \"startColor\": {\r\n      \"mode\": \"Color\",\r\n      \"color\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      },\r\n      \"colorMin\": {\r\n        \"r\": 1.23890687E+33,\r\n        \"g\": 3.89561E-43,\r\n        \"b\": -4.055289E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMax\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      }\r\n    },\r\n    \"gravityModifier\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"gravityModifierMultiplier\": 0.0,\r\n    \"simulationSpace\": \"World\",\r\n    \"customSimulationSpace\": null,\r\n    \"simulationSpeed\": 1.0,\r\n    \"useUnscaledTime\": false,\r\n    \"scalingMode\": \"Local\",\r\n    \"playOnAwake\": true,\r\n    \"emitterVelocityMode\": \"Rigidbody\",\r\n    \"maxParticles\": 1000,\r\n    \"stopAction\": \"None\",\r\n    \"cullingMode\": \"Automatic\",\r\n    \"ringBufferMode\": \"Disabled\",\r\n    \"ringBufferLoopRange\": {\r\n      \"x\": 0.0,\r\n      \"y\": 1.0\r\n    }\r\n  },\r\n  \"emission\": {\r\n    \"enabled\": true,\r\n    \"rateOverTime\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"rateOverTimeMultiplier\": 0.0,\r\n    \"rateOverDistance\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"rateOverDistanceMultiplier\": 0.0,\r\n    \"burstCount\": 1,\r\n    \"bursts\": [\r\n      {\r\n        \"time\": 0.0,\r\n        \"count\": {\r\n          \"mode\": \"Constant\",\r\n          \"constant\": 6.0,\r\n          \"constantMin\": 0.0,\r\n          \"constantMax\": 6.0,\r\n          \"curveMultiplier\": 1.0\r\n        },\r\n        \"cycleCount\": 1,\r\n        \"repeatInterval\": 0.01,\r\n        \"probability\": 1.0\r\n      }\r\n    ]\r\n  },\r\n  \"shape\": {\r\n    \"enabled\": true,\r\n    \"shapeType\": \"SingleSidedEdge\",\r\n    \"angle\": 25.0,\r\n    \"radius\": 0.001,\r\n    \"radiusMode\": \"Random\",\r\n    \"radiusSpread\": 0.0,\r\n    \"radiusSpeed\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"radiusSpeedMultiplier\": 1.0,\r\n    \"radiusThickness\": 1.0,\r\n    \"arc\": 360.0,\r\n    \"arcMode\": \"Random\",\r\n    \"arcSpread\": 0.0,\r\n    \"arcSpeed\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"arcSpeedMultiplier\": 1.0,\r\n    \"donutRadius\": 0.2,\r\n    \"length\": 5.0,\r\n    \"boxThickness\": {\r\n      \"x\": 0.0,\r\n      \"y\": 0.0,\r\n      \"z\": 0.0\r\n    },\r\n    \"meshShapeType\": \"Vertex\",\r\n    \"meshSpawnMode\": \"Random\",\r\n    \"meshSpawnSpread\": 0.0,\r\n    \"meshSpawnSpeed\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"meshSpawnSpeedMultiplier\": 1.0,\r\n    \"useMeshMaterialIndex\": false,\r\n    \"meshMaterialIndex\": 0,\r\n    \"useMeshColors\": true,\r\n    \"normalOffset\": 0.0,\r\n    \"position\": {\r\n      \"x\": 0.0,\r\n      \"y\": 0.0,\r\n      \"z\": 0.0\r\n    },\r\n    \"rotation\": {\r\n      \"x\": 0.0,\r\n      \"y\": 0.0,\r\n      \"z\": 0.0\r\n    },\r\n    \"scale\": {\r\n      \"x\": 1.0,\r\n      \"y\": 1.0,\r\n      \"z\": 1.0\r\n    },\r\n    \"textureClipChannel\": \"Alpha\",\r\n    \"textureClipThreshold\": 0.0,\r\n    \"textureColorAffectsParticles\": true,\r\n    \"textureAlphaAffectsParticles\": true,\r\n    \"textureBilinearFiltering\": false,\r\n    \"textureUVChannel\": 0,\r\n    \"alignToDirection\": false,\r\n    \"randomDirectionAmount\": 0.0,\r\n    \"sphericalDirectionAmount\": 0.0,\r\n    \"randomPositionAmount\": 0.0\r\n  },\r\n  \"velocityOverLifetime\": {\r\n    \"enabled\": true,\r\n    \"x\": {\r\n      \"mode\": \"TwoCurves\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -0.198830053,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMin\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -0.450286359,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.5,\r\n            \"value\": -0.2,\r\n            \"inTangent\": 1.4732306,\r\n            \"outTangent\": 1.4732306,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -0.198830053,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"y\": {\r\n      \"mode\": \"TwoCurves\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.5,\r\n            \"value\": -0.2,\r\n            \"inTangent\": 1.49568772,\r\n            \"outTangent\": 1.49568772,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMin\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.5,\r\n            \"value\": 0.2,\r\n            \"inTangent\": -1.49821007,\r\n            \"outTangent\": -1.49821007,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.5,\r\n            \"value\": -0.2,\r\n            \"inTangent\": 1.49568772,\r\n            \"outTangent\": 1.49568772,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"z\": {\r\n      \"mode\": \"TwoCurves\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMin\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"xMultiplier\": 1.0,\r\n    \"yMultiplier\": 1.0,\r\n    \"zMultiplier\": 1.0,\r\n    \"orbitalX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalXMultiplier\": 0.0,\r\n    \"orbitalYMultiplier\": 0.0,\r\n    \"orbitalZMultiplier\": 0.0,\r\n    \"orbitalOffsetX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalOffsetY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalOffsetZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"orbitalOffsetXMultiplier\": 0.0,\r\n    \"orbitalOffsetYMultiplier\": 0.0,\r\n    \"orbitalOffsetZMultiplier\": 0.0,\r\n    \"radial\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"radialMultiplier\": 0.0,\r\n    \"speedModifier\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 50.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 50.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"speedModifierMultiplier\": 50.0,\r\n    \"space\": \"Local\"\r\n  },\r\n  \"limitVelocityOverLifetime\": {\r\n    \"enabled\": false,\r\n    \"limitX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"limitXMultiplier\": 1.0,\r\n    \"limitY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"limitYMultiplier\": 1.0,\r\n    \"limitZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"limitZMultiplier\": 1.0,\r\n    \"limit\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"limitMultiplier\": 1.0,\r\n    \"dampen\": 0.0,\r\n    \"separateAxes\": false,\r\n    \"space\": \"Local\",\r\n    \"drag\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"dragMultiplier\": 0.0,\r\n    \"multiplyDragByParticleSize\": true,\r\n    \"multiplyDragByParticleVelocity\": true\r\n  },\r\n  \"lifetimeByEmitterSpeed\": {\r\n    \"enabled\": false,\r\n    \"curve\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": -0.8,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.2,\r\n            \"inTangent\": -0.8,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": -0.8,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.2,\r\n            \"inTangent\": -0.8,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"curveMultiplier\": 1.0,\r\n    \"range\": {\r\n      \"x\": 0.0,\r\n      \"y\": 1.0\r\n    }\r\n  },\r\n  \"inheritVelocity\": {\r\n    \"enabled\": false,\r\n    \"mode\": \"Initial\",\r\n    \"curve\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"curveMultiplier\": 0.0\r\n  },\r\n  \"forceOverLifetime\": {\r\n    \"enabled\": false,\r\n    \"x\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"y\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"z\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"xMultiplier\": 0.0,\r\n    \"yMultiplier\": 0.0,\r\n    \"zMultiplier\": 0.0,\r\n    \"space\": \"Local\",\r\n    \"randomized\": false\r\n  },\r\n  \"colorOverLifetime\": {\r\n    \"enabled\": false,\r\n    \"color\": {\r\n      \"mode\": \"Gradient\",\r\n      \"color\": {\r\n        \"r\": -4.47556432E+24,\r\n        \"g\": 3.881597E-43,\r\n        \"b\": -4.05571371E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMin\": {\r\n        \"r\": 8.251721E+31,\r\n        \"g\": 3.89561E-43,\r\n        \"b\": -4.05571371E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMax\": {\r\n        \"r\": -4.47556432E+24,\r\n        \"g\": 3.881597E-43,\r\n        \"b\": -4.05571371E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"gradient\": {\r\n        \"colorKeys\": [\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"alphaKeys\": [\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"mode\": \"Blend\"\r\n      },\r\n      \"gradientMax\": {\r\n        \"colorKeys\": [\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"alphaKeys\": [\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"mode\": \"Blend\"\r\n      }\r\n    }\r\n  },\r\n  \"colorBySpeed\": {\r\n    \"enabled\": false,\r\n    \"color\": {\r\n      \"mode\": \"Gradient\",\r\n      \"color\": {\r\n        \"r\": -4.47556432E+24,\r\n        \"g\": 3.881597E-43,\r\n        \"b\": -4.05570322E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMin\": {\r\n        \"r\": 9.420229E+31,\r\n        \"g\": 3.89561E-43,\r\n        \"b\": -4.05570322E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMax\": {\r\n        \"r\": -4.47556432E+24,\r\n        \"g\": 3.881597E-43,\r\n        \"b\": -4.05570322E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"gradient\": {\r\n        \"colorKeys\": [\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"alphaKeys\": [\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"mode\": \"Blend\"\r\n      },\r\n      \"gradientMax\": {\r\n        \"colorKeys\": [\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"color\": {\r\n              \"r\": 1.0,\r\n              \"g\": 1.0,\r\n              \"b\": 1.0,\r\n              \"a\": 1.0\r\n            },\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"alphaKeys\": [\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 0.0\r\n          },\r\n          {\r\n            \"alpha\": 1.0,\r\n            \"time\": 1.0\r\n          }\r\n        ],\r\n        \"mode\": \"Blend\"\r\n      }\r\n    },\r\n    \"range\": {\r\n      \"x\": 0.0,\r\n      \"y\": 1.0\r\n    }\r\n  },\r\n  \"sizeOverLifetime\": {\r\n    \"enabled\": true,\r\n    \"size\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.159872591,\r\n            \"value\": 0.724215448,\r\n            \"inTangent\": 1.87499,\r\n            \"outTangent\": 1.87499,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.778818846,\r\n            \"value\": 0.4275009,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": -4.734862,\r\n            \"outTangent\": -4.734862,\r\n            \"inWeight\": 0.174301848,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.159872591,\r\n            \"value\": 0.724215448,\r\n            \"inTangent\": 1.87499,\r\n            \"outTangent\": 1.87499,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.778818846,\r\n            \"value\": 0.4275009,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": -4.734862,\r\n            \"outTangent\": -4.734862,\r\n            \"inWeight\": 0.174301848,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"sizeMultiplier\": 1.0,\r\n    \"x\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.159872591,\r\n            \"value\": 0.724215448,\r\n            \"inTangent\": 1.87499,\r\n            \"outTangent\": 1.87499,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.778818846,\r\n            \"value\": 0.4275009,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": -4.734862,\r\n            \"outTangent\": -4.734862,\r\n            \"inWeight\": 0.174301848,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.159872591,\r\n            \"value\": 0.724215448,\r\n            \"inTangent\": 1.87499,\r\n            \"outTangent\": 1.87499,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 0.778818846,\r\n            \"value\": 0.4275009,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.0,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": -4.734862,\r\n            \"outTangent\": -4.734862,\r\n            \"inWeight\": 0.174301848,\r\n            \"outWeight\": 0.0,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"xMultiplier\": 1.0,\r\n    \"y\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"yMultiplier\": 1.0,\r\n    \"z\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"zMultiplier\": 1.0,\r\n    \"separateAxes\": false\r\n  },\r\n  \"sizeBySpeed\": {\r\n    \"enabled\": false,\r\n    \"size\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"sizeMultiplier\": 1.0,\r\n    \"x\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"xMultiplier\": 1.0,\r\n    \"y\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"yMultiplier\": 1.0,\r\n    \"z\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"zMultiplier\": 1.0,\r\n    \"separateAxes\": false,\r\n    \"range\": {\r\n      \"x\": 0.0,\r\n      \"y\": 1.0\r\n    }\r\n  },\r\n  \"rotationOverLifetime\": {\r\n    \"enabled\": true,\r\n    \"x\": {\r\n      \"mode\": \"TwoConstants\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"xMultiplier\": 0.0,\r\n    \"y\": {\r\n      \"mode\": \"TwoConstants\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"yMultiplier\": 0.0,\r\n    \"z\": {\r\n      \"mode\": \"TwoConstants\",\r\n      \"constant\": -6.981317,\r\n      \"constantMin\": 6.981317,\r\n      \"constantMax\": -6.981317,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"zMultiplier\": -6.981317,\r\n    \"separateAxes\": false\r\n  },\r\n  \"rotationBySpeed\": {\r\n    \"enabled\": false,\r\n    \"x\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"xMultiplier\": 0.0,\r\n    \"y\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"yMultiplier\": 0.0,\r\n    \"z\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.7853982,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.7853982,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"zMultiplier\": 0.7853982,\r\n    \"separateAxes\": false,\r\n    \"range\": {\r\n      \"x\": 0.0,\r\n      \"y\": 1.0\r\n    }\r\n  },\r\n  \"externalForces\": {\r\n    \"enabled\": true,\r\n    \"multiplier\": 5.0\r\n  },\r\n  \"noise\": {\r\n    \"enabled\": false,\r\n    \"strength\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"strengthMultiplier\": 1.0,\r\n    \"strengthX\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"strengthXMultiplier\": 1.0,\r\n    \"strengthY\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"strengthYMultiplier\": 1.0,\r\n    \"strengthZ\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"strengthZMultiplier\": 1.0,\r\n    \"separateAxes\": false,\r\n    \"frequency\": 0.5,\r\n    \"damping\": true,\r\n    \"octaveCount\": 1,\r\n    \"octaveMultiplier\": 0.5,\r\n    \"octaveScale\": 2.0,\r\n    \"quality\": \"Medium\",\r\n    \"scrollSpeed\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"scrollSpeedMultiplier\": 0.0,\r\n    \"remapEnabled\": false,\r\n    \"remap\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"remapMultiplier\": 1.0,\r\n    \"remapX\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"remapXMultiplier\": 1.0,\r\n    \"remapY\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"remapYMultiplier\": 1.0,\r\n    \"remapZ\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": -1.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 2.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 2.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"remapZMultiplier\": 1.0,\r\n    \"positionAmount\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"rotationAmount\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"sizeAmount\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    }\r\n  },\r\n  \"collision\": {\r\n    \"enabled\": false,\r\n    \"type\": \"World\",\r\n    \"mode\": \"Collision2D\",\r\n    \"dampen\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"dampenMultiplier\": 1.0,\r\n    \"bounce\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"bounceMultiplier\": 1.0,\r\n    \"lifetimeLoss\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"lifetimeLossMultiplier\": 0.0,\r\n    \"minKillSpeed\": 0.0,\r\n    \"maxKillSpeed\": 0.0,\r\n    \"collidesWith\": 55,\r\n    \"enableDynamicColliders\": true,\r\n    \"maxCollisionShapes\": 256,\r\n    \"quality\": \"High\",\r\n    \"voxelSize\": 0.5,\r\n    \"radiusScale\": 1.0,\r\n    \"sendCollisionMessages\": false,\r\n    \"colliderForce\": 0.0,\r\n    \"multiplyColliderForceByCollisionAngle\": true,\r\n    \"multiplyColliderForceByParticleSpeed\": false,\r\n    \"multiplyColliderForceByParticleSize\": false\r\n  },\r\n  \"trigger\": {\r\n    \"enabled\": false,\r\n    \"inside\": \"Kill\",\r\n    \"outside\": \"Ignore\",\r\n    \"enter\": \"Ignore\",\r\n    \"exit\": \"Ignore\",\r\n    \"radiusScale\": 1.0\r\n  },\r\n  \"subEmitters\": {\r\n    \"enabled\": false,\r\n    \"subEmittersCount\": 0\r\n  },\r\n  \"textureSheetAnimation\": {\r\n    \"enabled\": false,\r\n    \"numTilesX\": 1,\r\n    \"numTilesY\": 1,\r\n    \"animation\": \"WholeSheet\",\r\n    \"rowMode\": \"Random\",\r\n    \"frameOverTime\": {\r\n      \"mode\": \"Curve\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 0.9999,\r\n      \"curve\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      },\r\n      \"curveMax\": {\r\n        \"keys\": [\r\n          {\r\n            \"time\": 0.0,\r\n            \"value\": 0.0,\r\n            \"inTangent\": 0.0,\r\n            \"outTangent\": 1.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          },\r\n          {\r\n            \"time\": 1.0,\r\n            \"value\": 1.0,\r\n            \"inTangent\": 1.0,\r\n            \"outTangent\": 0.0,\r\n            \"inWeight\": 0.333333343,\r\n            \"outWeight\": 0.333333343,\r\n            \"weightedMode\": \"None\"\r\n          }\r\n        ],\r\n        \"preWrapMode\": \"ClampForever\",\r\n        \"postWrapMode\": \"ClampForever\"\r\n      }\r\n    },\r\n    \"frameOverTimeMultiplier\": 0.9999,\r\n    \"startFrame\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 0.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 0.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"startFrameMultiplier\": 0.0,\r\n    \"cycleCount\": 1,\r\n    \"rowIndex\": 0\r\n  },\r\n  \"trails\": {\r\n    \"mode\": \"PerParticle\",\r\n    \"attachRibbonsToTransform\": false,\r\n    \"ribbonCount\": 1,\r\n    \"splitSubEmitterRibbons\": false,\r\n    \"textureScale\": {\r\n      \"x\": 1.0,\r\n      \"y\": 1.0\r\n    },\r\n    \"enabled\": false,\r\n    \"ratio\": 1.0,\r\n    \"lifetime\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"lifetimeMultiplier\": 1.0,\r\n    \"minVertexDistance\": 0.2,\r\n    \"textureMode\": \"Stretch\",\r\n    \"worldSpace\": false,\r\n    \"dieWithParticles\": true,\r\n    \"sizeAffectsWidth\": true,\r\n    \"sizeAffectsLifetime\": false,\r\n    \"inheritParticleColor\": true,\r\n    \"colorOverLifetime\": {\r\n      \"mode\": \"Color\",\r\n      \"color\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      },\r\n      \"colorMin\": {\r\n        \"r\": 2.46124188E-09,\r\n        \"g\": 3.89561E-43,\r\n        \"b\": -4.05555118E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMax\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      }\r\n    },\r\n    \"widthOverTrail\": {\r\n      \"mode\": \"Constant\",\r\n      \"constant\": 1.0,\r\n      \"constantMin\": 0.0,\r\n      \"constantMax\": 1.0,\r\n      \"curveMultiplier\": 1.0\r\n    },\r\n    \"widthOverTrailMultiplier\": 1.0,\r\n    \"colorOverTrail\": {\r\n      \"mode\": \"Color\",\r\n      \"color\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      },\r\n      \"colorMin\": {\r\n        \"r\": 2.50484788E-09,\r\n        \"g\": 3.89561E-43,\r\n        \"b\": -4.05555118E+11,\r\n        \"a\": 2.522337E-44\r\n      },\r\n      \"colorMax\": {\r\n        \"r\": 1.0,\r\n        \"g\": 1.0,\r\n        \"b\": 1.0,\r\n        \"a\": 1.0\r\n      }\r\n    }\r\n  },\r\n  \"customData\": {\r\n    \"enabled\": false\r\n  }\r\n}";

}