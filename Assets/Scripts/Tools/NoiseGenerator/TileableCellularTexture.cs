
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


[BurstCompile]
public class TileableCausticTexture : MonoBehaviour
{
    [Header("Texture Settings")]
    public int resolution = 1024;
    public string outputPath = "Scripts/Tools/NoiseGenerator/Noise.png";

    [Header("Noise Structure")]
    public int cellCount = 8;
    public int octaves = 3;
    public float lacunarity = 2f;
    public float persistence = 0.5f;

    [Header("Caustic Controls")]
    public float causticSharpness = 12f;
    public float domainWarpStrength = 0.2f;
    public bool useF2MinusF1 = true;

    [Header("Appearance")]
    public float contrast = 2f;
    public float brightness = 1f;
    public bool invert = false;

    [Header("Animation")]
    public float time = 0f;
    public float animationSpeed = 1f;

    [Header("Random")]
    public int seed = 0;

    Texture2D generatedTexture;

#if UNITY_EDITOR
    private double _lastValidateTime;
    private bool _pendingGenerate;

    private void OnValidate()
    {
        _lastValidateTime = UnityEditor.EditorApplication.timeSinceStartup;

        if (!_pendingGenerate)
        {
            _pendingGenerate = true;
            UnityEditor.EditorApplication.update += WaitAndGenerate;
        }
    }

    private void WaitAndGenerate()
    {
        if (UnityEditor.EditorApplication.timeSinceStartup - _lastValidateTime >= 0.1)
        {
            _pendingGenerate = false;
            UnityEditor.EditorApplication.update -= WaitAndGenerate;
            Generate();
        }
    }
#endif

    void Start()
    {
        Generate();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyColor
    {
        const float inv255 = 1f / 255f;
        byte R;
        byte G;
        byte B;
        byte A;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MyColor(float ri, float gi, float bi, float ai) { R = G = B = A = 0; r = ri; g = gi; b = bi; a = ai; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MyColor(byte r, byte g, byte b, byte a) { R = r; G = g; B = b; A = a; }
        public float r
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => R * inv255;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => R = (byte)(math.clamp(value, 0f, 1f) * 255f);
        }
        public float g
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => G * inv255;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => G = (byte)(math.clamp(value, 0f, 1f) * 255f);
        }
        public float b
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => B * inv255;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => B = (byte)(math.clamp(value, 0f, 1f) * 255f);
        }
        public float a
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => A * inv255;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => A = (byte)(math.clamp(value, 0f, 1f) * 255f);
        }
    }

    float[] randomTable = null;
    int prevSeed = 0;


    [ContextMenu("Generate Caustic Texture")]
    public void Generate()
    {
        generatedTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        generatedTexture.wrapMode = TextureWrapMode.Repeat;

        if (randomTable == null || prevSeed != seed)
        {
            randomTable = InitializeRandomTable(seed, 4096);
            prevSeed = seed;
        }

        NativeArray<float2> featurePoints = new NativeArray<float2>(cellCount * cellCount, Allocator.Temp);
        
        GenerateFeaturePoints(
            cellCount,
            seed,
            randomTable,
            featurePoints);

        NativeArray<MyColor> pixels = new NativeArray<MyColor>(resolution * resolution, Allocator.Temp);

        FractalCaustic(ref pixels, ref featurePoints, resolution, octaves, lacunarity, persistence, causticSharpness, 
            domainWarpStrength, useF2MinusF1, cellCount, time, animationSpeed, contrast, brightness,invert);

        generatedTexture.SetPixelData(pixels, 0, 0);
        generatedTexture.Apply();

        byte[] pngBytes = generatedTexture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Application.dataPath, outputPath), pngBytes);
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public static void GenerateFeaturePoints(int cellCount, int seed, float[] randomTable, NativeArray<float2> points)
    {
        for (int y = 0; y < cellCount; y++)
        {
            for (int x = 0; x < cellCount; x++)
            {
                points[y * cellCount + x] = new float2((x + RandomFromTable(seed++, randomTable)) / cellCount, (y + RandomFromTable(seed++, randomTable)) / cellCount);
            }
        }
    }


    [BurstCompile]
    static void FractalCaustic(
        ref NativeArray<MyColor> pixels,
        ref NativeArray<float2> featurePoints,
        int resolution,
        int octaves,
        float lacunarity,
        float persistence,
        float causticSharpness,
        float domainWarpStrength,
        bool useF2MinusF1,
        int cellCount,
        float time,
        float animationSpeed,
        float contrast,
        float brightness,
        bool invert)
    {
        float t = time * animationSpeed;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (float)x / resolution;
                float v = (float)y / resolution;

                float amplitude = 1f;
                float frequency = 1f;
                float sum = 0f;
                float maxSum = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float uf = u * frequency;
                    float vf = v * frequency;
                    float warp = math.sin((uf + t) * math.PI * 2f) * math.cos((vf + t) * math.PI * 2f);

                    float warpedU = Repeat(uf + warp * domainWarpStrength, 1f);
                    float warpedV = Repeat(vf + warp * domainWarpStrength, 1f);

                    float min1 = float.MaxValue;
                    float min2 = float.MaxValue;

                    float scaledU = warpedU * cellCount;
                    float scaledV = warpedV * cellCount;

                    for (int iy = 0; iy < cellCount; iy++)
                    {
                        for (int ix = 0; ix < cellCount; ix++)
                        {
                            float2 p = featurePoints[iy * cellCount + ix];

                            float dx = math.abs(scaledU - p.x * cellCount);
                            float dy = math.abs(scaledV - p.y * cellCount);

                            dx = math.min(dx, cellCount - dx);
                            dy = math.min(dy, cellCount - dy);

                            float dist = dx * dx + dy * dy;

                            if (dist < min1)
                            {
                                min2 = min1;
                                min1 = dist;
                            }
                            else if (dist < min2) min2 = dist;
                        }
                    }

                    float value = useF2MinusF1 ? math.sqrt(min2) - math.sqrt(min1) : math.sqrt(min1);
                    value = math.exp(-value * causticSharpness);
                    sum += value * amplitude;
                    maxSum += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                float pixel = math.pow(sum / maxSum, contrast) * brightness;
                pixel = math.clamp(pixel, 0f, 1f);

                if (invert) pixel = 1f - pixel;

                pixels[y * resolution + x] = new MyColor(pixel, pixel, pixel, 1f);
            }
        }
    }


    public static float[] InitializeRandomTable(int seed, int tableSize)
    {
        UnityEngine.Random.InitState(seed);
        float[] table = new float[tableSize];
        for (int i = 0; i < tableSize; i++) table[i] = UnityEngine.Random.value;
        return table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RandomFromTable(int index, float[] table) => table[index & (table.Length - 1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Repeat(float t, float length) => math.clamp(t - math.floor(t / length) * length, 0f, length);
}