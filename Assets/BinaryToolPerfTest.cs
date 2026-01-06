using FMOD.Studio;
using System;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BinaryToolPerfTest : MonoBehaviour
{
    [Header("Test Settings")]
    public int iterations = 5_000_000;
    public int warmupIterations = 100_000;

    [Header("Vector Ranges")]
    public float minValue = -1000f;
    public float maxValue = 1000f;

    [Header("Bytes Per Component")]
    public int xBytes = 2;
    public int yBytes = 2;
    public int zBytes = 2;
    public int wBytes = 2;

    Stopwatch sw;

    [ContextMenu("Run tests")]
    void RunAllTests()
    {

        GC.Collect();
        Resources.UnloadUnusedAssets();

        sw = new Stopwatch();

        Debug.Log("==== BinaryTool Performance Tests ====");

        TestFloat();
        TestVector2();
        TestVector3();
        TestVector4();

        Debug.Log("==== DONE ====");
        enabled = false;
        sw = null;
    }

    void TestFloat()
    {
        float value = 123.456f;
        float min = minValue;
        float max = maxValue;
        int bytes = xBytes;

        byte[] preAlloc = new byte[bytes];

        Warmup(() =>
        {
            byte[] anotherThing = BinaryTool.CompressFloatCache(value, xBytes, min, max);
            value = BinaryTool.DecompressFloat(anotherThing, min, max);
        });


        GC.Collect();
        Resources.UnloadUnusedAssets();

        Run($"Float / Alloc / {bytes}B", iterations, bytes, () =>
        {
            byte[] buf = BinaryTool.CompressFloatAlloc(value, bytes, min, max);
            value = BinaryTool.DecompressFloat(buf, min, max);
        });

        GC.Collect();
        Resources.UnloadUnusedAssets();

        Run($"Float / Cached / {bytes}B", iterations, bytes, () =>
        {
            byte[] buf = BinaryTool.CompressFloatCache(value, bytes, min, max);
            value = BinaryTool.DecompressFloat(buf, min, max);
        });

        GC.Collect();
        Resources.UnloadUnusedAssets();

        Run($"Float / PreAlloc / {bytes}B", iterations, bytes, () =>
        {
            BinaryTool.CompressFloatPreAlloc(ref preAlloc, value, bytes, min, max);
            value = BinaryTool.DecompressFloat(preAlloc, min, max);
        });
    }

    void TestVector2()
    {
        float2 value = new float2(1.1f, -2.2f);
        float2 min = minValue;
        float2 max = maxValue;
        int totalBytes = xBytes + yBytes;

        byte[] preAlloc = new byte[totalBytes];

        Warmup(() =>
        {
            BinaryTool.CompressVector2Alloc(value, xBytes, yBytes, min, max);
            BinaryTool.CompressVector2Cached(value, xBytes, yBytes, min, max);
            BinaryTool.CompressVector2PreAlloc(ref preAlloc, value, xBytes, yBytes, min, max);
        });

        Run("Vector2 / Alloc", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector2Alloc(value, xBytes, yBytes, min, max);
            value = BinaryTool.DecompressVector2(buf, xBytes, yBytes, min, max);
        });

        Run("Vector2 / Cached", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector2Cached(value, xBytes, yBytes, min, max);
            value = BinaryTool.DecompressVector2(buf, xBytes, yBytes, min, max);
        });

        Run("Vector2 / PreAlloc", iterations, totalBytes, () =>
        {
            BinaryTool.CompressVector2PreAlloc(ref preAlloc, value, xBytes, yBytes, min, max);
            value = BinaryTool.DecompressVector2(preAlloc, xBytes, yBytes, min, max);
        });
    }

    void TestVector3()
    {
        float3 value = new float3(1.1f, -2.2f, 3.3f);
        float3 min = minValue;
        float3 max = maxValue;
        int totalBytes = xBytes + yBytes + zBytes;

        byte[] preAlloc = new byte[totalBytes];

        Warmup(() =>
        {
            BinaryTool.CompressVector3Alloc(value, xBytes, yBytes, zBytes, min, max);
            BinaryTool.CompressVector3Cached(value, xBytes, yBytes, zBytes, min, max);
            BinaryTool.CompressVector3PreAlloc(ref preAlloc, value, xBytes, yBytes, zBytes, min, max);
        });

        Run("Vector3 / Alloc", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector3Alloc(value, xBytes, yBytes, zBytes, min, max);
            value = BinaryTool.DecompressVector3(buf, xBytes, yBytes, zBytes, min, max);
        });

        Run("Vector3 / Cached", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector3Cached(value, xBytes, yBytes, zBytes, min, max);
            value = BinaryTool.DecompressVector3(buf, xBytes, yBytes, zBytes, min, max);
        });

        Run("Vector3 / PreAlloc", iterations, totalBytes, () =>
        {
            BinaryTool.CompressVector3PreAlloc(ref preAlloc, value, xBytes, yBytes, zBytes, min, max);
            value = BinaryTool.DecompressVector3(preAlloc, xBytes, yBytes, zBytes, min, max);
        });
    }

    void TestVector4()
    {
        float4 value = new float4(1.1f, -2.2f, 3.3f, -4.4f);
        float4 min = minValue;
        float4 max = maxValue;
        int totalBytes = xBytes + yBytes + zBytes + wBytes;

        byte[] preAlloc = new byte[totalBytes];

        Warmup(() =>
        {
            BinaryTool.CompressVector4Alloc(value, xBytes, yBytes, zBytes, wBytes, min, max);
            BinaryTool.CompressVector4Cached(value, xBytes, yBytes, zBytes, wBytes, min, max);
            BinaryTool.CompressVector4PreAlloc(ref preAlloc, value, xBytes, yBytes, zBytes, wBytes, min, max);
        });

        Run("Vector4 / Alloc", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector4Alloc(value, xBytes, yBytes, zBytes, wBytes, min, max);
            value = BinaryTool.DecompressVector4(buf, xBytes, yBytes, zBytes, wBytes, min, max);
        });

        Run("Vector4 / Cached", iterations, totalBytes, () =>
        {
            byte[] buf = BinaryTool.CompressVector4Cached(value, xBytes, yBytes, zBytes, wBytes, min, max);
            value = BinaryTool.DecompressVector4(buf, xBytes, yBytes, zBytes, wBytes, min, max);
        });

        Run("Vector4 / PreAlloc", iterations, totalBytes, () =>
        {
            BinaryTool.CompressVector4PreAlloc(ref preAlloc, value, xBytes, yBytes, zBytes, wBytes, min, max);
            value = BinaryTool.DecompressVector4(preAlloc, xBytes, yBytes, zBytes, wBytes, min, max);
        });
    }

    void Warmup(Action a)
    {
        for (int i = 0; i < warmupIterations; i++)
            a();
    }

    void Run(string label, int ops, int bytesPerOp, Action a)
    {
        sw.Restart();
        for (int i = 0; i < ops; i++) a();
        sw.Stop();

        double seconds = sw.Elapsed.TotalSeconds;
        long totalBytes = (long)ops * bytesPerOp * 2;

        double opsPerSec = ops / seconds;
        double mbPerSec = (totalBytes / seconds) / (1024.0 * 1024.0);

        Debug.Log(
            $"{label}\n" +
            $"  Time: {seconds:F4}s\n" +
            $"  Ops: {ops:N0}\n" +
            $"  Ops/s: {opsPerSec:N0}\n" +
            $"  Bandwidth: {mbPerSec:F2} MB/s\n"
        );
    }
}
