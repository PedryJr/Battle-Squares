using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

public class BinaryToolPerfTest : MonoBehaviour
{
    [SerializeField] private TMP_Text logText;

    private StringBuilder log = new StringBuilder();
    private int passedTests = 0;
    private int failedTests = 0;
    private const float TOLERANCE = 0.01f; // 1% tolerance for lossy compression

    [ContextMenu("Run Test")]
    private void Start()
    {
        if(log == null) log = new StringBuilder();
        log.Clear();
        if (logText == null)
        {
            Debug.LogError("TMP_Text component not assigned!");
            return;
        }

        RunAllTests();
        DisplayResults();
    }

    private void RunAllTests()
    {
        LogHeader("BINARY TOOL C COMPRESSION TESTS");

        // Float tests
        LogSection("Float Compression Tests");
        TestFloatCompression(50f, 1, 0f, 100f);
        TestFloatCompression(25.5f, 2, 0f, 100f);
        TestFloatCompression(75.25f, 3, 0f, 100f);
        TestFloatCompression(42.123f, 4, 0f, 100f);
        TestFloatCompression(-10f, 2, -50f, 50f);
        TestFloatCompression(0f, 1, -100f, 100f);
        TestFloatCompression(100f, 2, 0f, 100f);

        // Vector2 tests
        LogSection("Vector2 Compression Tests");
        TestVector2Compression(new float2(10f, 20f), 1, 1, new float2(0f, 0f), new float2(100f, 100f));
        TestVector2Compression(new float2(50.5f, 75.25f), 2, 2, new float2(0f, 0f), new float2(100f, 100f));
        TestVector2Compression(new float2(33.33f, 66.66f), 3, 3, new float2(0f, 0f), new float2(100f, 100f));
        TestVector2Compression(new float2(-25f, 25f), 2, 2, new float2(-50f, -50f), new float2(50f, 50f));
        TestVector2Compression(new float2(1.234f, 5.678f), 4, 4, new float2(0f, 0f), new float2(10f, 10f));

        // Vector3 tests
        LogSection("Vector3 Compression Tests");
        TestVector3Compression(new float3(10f, 20f, 30f), 1, 1, 1, new float3(0f, 0f, 0f), new float3(100f, 100f, 100f));
        TestVector3Compression(new float3(25.5f, 50.5f, 75.5f), 2, 2, 2, new float3(0f, 0f, 0f), new float3(100f, 100f, 100f));
        TestVector3Compression(new float3(12.34f, 56.78f, 90.12f), 3, 3, 3, new float3(0f, 0f, 0f), new float3(100f, 100f, 100f));
        TestVector3Compression(new float3(-10f, 0f, 10f), 2, 2, 2, new float3(-50f, -50f, -50f), new float3(50f, 50f, 50f));
        TestVector3Compression(new float3(1.1f, 2.2f, 3.3f), 4, 4, 4, new float3(0f, 0f, 0f), new float3(10f, 10f, 10f));

        // Vector4 tests
        LogSection("Vector4 Compression Tests");
        TestVector4Compression(new float4(10f, 20f, 30f, 40f), 1, 1, 1, 1, new float4(0f, 0f, 0f, 0f), new float4(100f, 100f, 100f, 100f));
        TestVector4Compression(new float4(25f, 50f, 75f, 100f), 2, 2, 2, 2, new float4(0f, 0f, 0f, 0f), new float4(100f, 100f, 100f, 100f));
        TestVector4Compression(new float4(15.5f, 35.5f, 65.5f, 85.5f), 3, 3, 3, 3, new float4(0f, 0f, 0f, 0f), new float4(100f, 100f, 100f, 100f));
        TestVector4Compression(new float4(-20f, -10f, 10f, 20f), 2, 2, 2, 2, new float4(-50f, -50f, -50f, -50f), new float4(50f, 50f, 50f, 50f));
        TestVector4Compression(new float4(1.11f, 2.22f, 3.33f, 4.44f), 4, 4, 4, 4, new float4(0f, 0f, 0f, 0f), new float4(10f, 10f, 10f, 10f));

        // Edge case tests
        LogSection("Edge Case Tests");
        TestFloatCompression(0f, 1, 0f, 1f);
        TestFloatCompression(1f, 1, 0f, 1f);
        TestVector2Compression(new float2(0f, 0f), 1, 1, new float2(0f, 0f), new float2(1f, 1f));
        TestVector3Compression(new float3(1f, 1f, 1f), 2, 2, 2, new float3(0f, 0f, 0f), new float3(1f, 1f, 1f));
    }

    private void TestFloatCompression(float value, int bytes, float min, float max)
    {
        try
        {
            byte[] compressed = BinaryTool.CompressFloatAlloc(value, bytes, min, max);
            float decompressed = BinaryTool.DecompressFloat(compressed, min, max);

            float expectedTolerance = bytes == 4 ? 0.0001f : CalculateTolerance(min, max, bytes);
            bool passed = Mathf.Abs(value - decompressed) <= expectedTolerance;

            LogTest($"Float({bytes}B): {value:F3} -> {decompressed:F3}", passed,
                $"Expected: {value:F3}, Got: {decompressed:F3}, Error: {Mathf.Abs(value - decompressed):F4}");
        }
        catch (Exception e)
        {
            LogTest($"Float({bytes}B): {value:F3}", false, $"Exception: {e.Message}");
        }
    }

    private void TestVector2Compression(float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        try
        {
            byte[] compressed = BinaryTool.CompressVector2Alloc(value, xBytes, yBytes, min, max);
            float2 decompressed = BinaryTool.DecompressVector2(compressed, xBytes, yBytes, min, max);

            float toleranceX = xBytes == 4 ? 0.0001f : CalculateTolerance(min.x, max.x, xBytes);
            float toleranceY = yBytes == 4 ? 0.0001f : CalculateTolerance(min.y, max.y, yBytes);

            bool passed = Mathf.Abs(value.x - decompressed.x) <= toleranceX &&
                         Mathf.Abs(value.y - decompressed.y) <= toleranceY;

            LogTest($"Vector2({xBytes},{yBytes}B): ({value.x:F2}, {value.y:F2})", passed,
                $"Got: ({decompressed.x:F2}, {decompressed.y:F2}), " +
                $"Error: ({Mathf.Abs(value.x - decompressed.x):F4}, {Mathf.Abs(value.y - decompressed.y):F4})");
        }
        catch (Exception e)
        {
            LogTest($"Vector2({xBytes},{yBytes}B): ({value.x:F2}, {value.y:F2})", false, $"Exception: {e.Message}");
        }
    }

    private void TestVector3Compression(float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        try
        {
            byte[] compressed = BinaryTool.CompressVector3Alloc(value, xBytes, yBytes, zBytes, min, max);
            float3 decompressed = BinaryTool.DecompressVector3(compressed, xBytes, yBytes, zBytes, min, max);

            float toleranceX = xBytes == 4 ? 0.0001f : CalculateTolerance(min.x, max.x, xBytes);
            float toleranceY = yBytes == 4 ? 0.0001f : CalculateTolerance(min.y, max.y, yBytes);
            float toleranceZ = zBytes == 4 ? 0.0001f : CalculateTolerance(min.z, max.z, zBytes);

            bool passed = Mathf.Abs(value.x - decompressed.x) <= toleranceX &&
                         Mathf.Abs(value.y - decompressed.y) <= toleranceY &&
                         Mathf.Abs(value.z - decompressed.z) <= toleranceZ;

            LogTest($"Vector3({xBytes},{yBytes},{zBytes}B): ({value.x:F2}, {value.y:F2}, {value.z:F2})", passed,
                $"Got: ({decompressed.x:F2}, {decompressed.y:F2}, {decompressed.z:F2})");
        }
        catch (Exception e)
        {
            LogTest($"Vector3({xBytes},{yBytes},{zBytes}B)", false, $"Exception: {e.Message}");
        }
    }

    private void TestVector4Compression(float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        try
        {
            byte[] compressed = BinaryTool.CompressVector4Alloc(value, xBytes, yBytes, zBytes, wBytes, min, max);
            float4 decompressed = BinaryTool.DecompressVector4(compressed, xBytes, yBytes, zBytes, wBytes, min, max);

            float toleranceX = xBytes == 4 ? 0.0001f : CalculateTolerance(min.x, max.x, xBytes);
            float toleranceY = yBytes == 4 ? 0.0001f : CalculateTolerance(min.y, max.y, yBytes);
            float toleranceZ = zBytes == 4 ? 0.0001f : CalculateTolerance(min.z, max.z, zBytes);
            float toleranceW = wBytes == 4 ? 0.0001f : CalculateTolerance(min.w, max.w, wBytes);

            bool passed = Mathf.Abs(value.x - decompressed.x) <= toleranceX &&
                         Mathf.Abs(value.y - decompressed.y) <= toleranceY &&
                         Mathf.Abs(value.z - decompressed.z) <= toleranceZ &&
                         Mathf.Abs(value.w - decompressed.w) <= toleranceW;

            LogTest($"Vector4({xBytes},{yBytes},{zBytes},{wBytes}B): ({value.x:F1}, {value.y:F1}, {value.z:F1}, {value.w:F1})",
                passed, $"Got: ({decompressed.x:F1}, {decompressed.y:F1}, {decompressed.z:F1}, {decompressed.w:F1})");
        }
        catch (Exception e)
        {
            LogTest($"Vector4({xBytes},{yBytes},{zBytes},{wBytes}B)", false, $"Exception: {e.Message}");
        }
    }

    private float CalculateTolerance(float min, float max, int bytes)
    {
        float range = max - min;
        int maxValue = (1 << (bytes * 8)) - 1;
        return range / maxValue * 2; // Allow 2 steps of error
    }

    private void LogHeader(string header)
    {
        log.AppendLine($"\n<b><size=18>{header}</size></b>");
        log.AppendLine(new string('=', 50));
    }

    private void LogSection(string section)
    {
        log.AppendLine($"\n<b><color=#00FFFF>{section}</color></b>");
    }

    private void LogTest(string testName, bool passed, string details)
    {
        if (passed)
        {
            passedTests++;
            log.AppendLine($"<color=#00FF00>V</color> {testName}");
        }
        else
        {
            failedTests++;
            log.AppendLine($"<color=#FF0000>F</color> {testName}");
            log.AppendLine($"  <color=#FFAA00>{details}</color>");
        }
    }

    private void DisplayResults()
    {
        int totalTests = passedTests + failedTests;
        float passRate = totalTests > 0 ? (passedTests / (float)totalTests) * 100f : 0f;

        log.AppendLine("\n" + new string('=', 50));
        log.AppendLine($"<b><size=16>TEST RESULTS</size></b>");
        log.AppendLine($"Total Tests: {totalTests}");
        log.AppendLine($"<color=#00FF00>Passed: {passedTests}</color>");
        log.AppendLine($"<color=#FF0000>Failed: {failedTests}</color>");
        log.AppendLine($"<b>Pass Rate: {passRate:F1}%</b>");

        logText.text = log.ToString();

        Debug.Log($"BinaryTool Tests Complete: {passedTests}/{totalTests} passed ({passRate:F1}%)");
    }
}