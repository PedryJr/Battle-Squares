using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AnimationCurveCreatorTest : MonoBehaviour
{

    [Serializable]
    public class AnimationCurveFile
    {
        public CurveData curve;
    }

    [Serializable]
    public class CurveData
    {
        public List<KeyframeData> m_Curve;
    }

    [Serializable]
    public class KeyframeData
    {
        public float time;
        public float value;
        public float inSlope;
        public float outSlope;
        public int tangentMode;
        public int weightedMode;
        public float inWeight;
        public float outWeight;
    }

    [SerializeField] AnimationCurve Curve;
    [SerializeField] string name;

    public void ExportJson()
    {
        string path = "Assets/BuildHelper/" + name + ".json";
        ExportJson(path, Curve);
    }
    public static void ExportJson(string path, AnimationCurve curve)
    {

        if (string.IsNullOrEmpty(path)) return;

        AnimationCurveFile file = new AnimationCurveFile
        {
            curve = new CurveData
            {
                m_Curve = new List<KeyframeData>()
            }
        };

        foreach (Keyframe k in curve.keys)
        {
            file.curve.m_Curve.Add(new KeyframeData
            {
                time = k.time,
                value = k.value,
                inSlope = k.inTangent,
                outSlope = k.outTangent,
                inWeight = k.inWeight,
                outWeight = k.outWeight,
                weightedMode = (int)k.weightedMode,
                tangentMode = 0
            });
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));

    }
    public void ImportJson()
    {
        string path = "Assets/BuildHelper/" + name + ".json";
    }
    public static AnimationCurve ImportJson(string path)
    {

        AnimationCurve Curve = new AnimationCurve();


        if (string.IsNullOrEmpty(path))
        {
            Curve.keys = new Keyframe[2] 
            {
                new Keyframe(0, 1),
                new Keyframe(1, 1),
            };
            return Curve;
        }

        if (!File.Exists(path))
        {
            Curve.keys = new Keyframe[2]
            {
                new Keyframe(0, 1),
                new Keyframe(1, 1),
            };
            return Curve;
        }

        var file = JsonConvert.DeserializeObject<AnimationCurveFile>(
            File.ReadAllText(path));

        Curve.ClearKeys();

        foreach (var k in file.curve.m_Curve)
        {
            var key = new Keyframe(
                k.time,
                k.value,
                k.inSlope,
                k.outSlope,
                k.inWeight,
                k.outWeight
            )
            {
                weightedMode = (WeightedMode)k.weightedMode
            };

            Curve.AddKey(key);
        }

        return Curve;
    }

}
