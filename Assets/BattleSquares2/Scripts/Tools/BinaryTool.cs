using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
    DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
    FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
public unsafe static class BinaryTool
{
    public const float inv255 = 0.0039215686274509803921568627f;
    public const float inv65535 = 0.0000152587890625f;
    public const float inv16777215 = 0.000000059604644775390625f;
    public const bool compileSynchronously = true;
    public const bool debug = false;
    public const bool disableDirectCall = false;
    public const bool disableSafetyChecks = true;
    public const FloatMode floatMode = FloatMode.Fast;
    public const FloatPrecision floatPrecision = FloatPrecision.Low;
    public const OptimizeFor optimizeFor = OptimizeFor.Performance;
    public const MethodImplOptions impl = MethodImplOptions.AggressiveInlining;

    private static byte[][] cache = new byte[17][]
    {
        new byte[1], new byte[1], new byte[2], new byte[3],
        new byte[4], new byte[5], new byte[6], new byte[7],
        new byte[8], new byte[9], new byte[10], new byte[11],
        new byte[12], new byte[13], new byte[14], new byte[15],
        new byte[16]
    };

    [MethodImpl(impl)]
    public static float DecompressFloat(byte[] buffer, float min, float max)
    {
        fixed (byte* ptr = buffer) return DecompressFloatCore(ptr, min, max, buffer.Length);
    }
    [MethodImpl(impl)]
    public static byte[] CompressFloatAlloc(float value, int bytes, float min, float max)
    {
        byte[] ret = new byte[bytes];
        fixed (byte* ptr = ret) CompressFloatCore(value, min, max, ptr, bytes);
        return ret;
    }
    [MethodImpl(impl)]
    public static byte[] CompressFloatCache(float value, int bytes, float min, float max)
    {
        fixed (byte* ptr = cache[bytes]) CompressFloatCore(value, min, max, ptr, bytes);
        return cache[bytes];
    }
    [MethodImpl(impl)]
    public static void CompressFloatPreAlloc(ref byte[] buffer, float value, int bytes, float min, float max)
    {
        fixed (byte* ptr = buffer) CompressFloatCore(value, min, max, ptr, bytes);
    }

    [MethodImpl(impl)]
    public static float2 DecompressVector2(byte[] buffer, int xBytes, int yBytes, float2 min, float2 max)
    {
        float2 result = default;
        fixed (byte* ptr = buffer) DecompressVector2Core(ptr, xBytes, yBytes, in min, in max, ref result);
        return result;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector2Cached(float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        byte[] buffer = cache[xBytes + yBytes];
        fixed (byte* ptr = buffer) CompressVector2Core(ptr, in value, xBytes, yBytes, in min, in max);
        return buffer;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector2Alloc(float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        byte[] ret = new byte[xBytes + yBytes];
        fixed (byte* ptr = ret) CompressVector2Core(ptr, in value, xBytes, yBytes, in min, in max);
        return ret;
    }
    [MethodImpl(impl)]
    public static void CompressVector2PreAlloc(ref byte[] buffer, float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        int bufferSize = xBytes + yBytes;
        if (buffer == null || buffer.Length != bufferSize) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector2Core(ptr, in value, xBytes, yBytes, in min, in max);
    }

    [MethodImpl(impl)]
    public static float3 DecompressVector3(byte[] buffer, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        float3 result = default;
        fixed (byte* ptr = buffer) DecompressVector3Core(ptr, xBytes, yBytes, zBytes, in min, in max, ref result);
        return result;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector3Cached(float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        byte[] buffer = cache[xBytes + yBytes + zBytes];
        fixed (byte* ptr = buffer) CompressVector3Core(ptr, in value, xBytes, yBytes, zBytes, in min, in max);
        return buffer;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector3Alloc(float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        byte[] ret = new byte[xBytes + yBytes + zBytes];
        fixed (byte* ptr = ret) CompressVector3Core(ptr, in value, xBytes, yBytes, zBytes, in min, in max);
        return ret;
    }
    [MethodImpl(impl)]
    public static void CompressVector3PreAlloc(ref byte[] buffer, float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        int bufferSize = xBytes + yBytes + zBytes;
        if (buffer == null || buffer.Length != bufferSize) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector3Core(ptr, in value, xBytes, yBytes, zBytes, in min, in max);
    }

    [MethodImpl(impl)]
    public static float4 DecompressVector4(byte[] buffer, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        float4 result = default;
        fixed (byte* ptr = buffer) DecompressVector4Core(ptr, xBytes, yBytes, zBytes, wBytes, in min, in max, ref result);
        return result;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector4Cached(float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        byte[] buffer = cache[xBytes + yBytes + zBytes + wBytes];
        fixed (byte* ptr = buffer) CompressVector4Core(ptr, in value, xBytes, yBytes, zBytes, wBytes, in min, in max);
        return buffer;
    }
    [MethodImpl(impl)]
    public static byte[] CompressVector4Alloc(float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        byte[] ret = new byte[xBytes + yBytes + zBytes + wBytes];
        fixed (byte* ptr = ret) CompressVector4Core(ptr, in value, xBytes, yBytes, zBytes, wBytes, in min, in max);
        return ret;
    }
    [MethodImpl(impl)]
    public static void CompressVector4PreAlloc(ref byte[] buffer, float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        int bufferSize = xBytes + yBytes + zBytes + wBytes;
        if (buffer == null || buffer.Length != bufferSize) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector4Core(ptr, in value, xBytes, yBytes, zBytes, wBytes, in min, in max);
    }


    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressFloatCore(float value, float min, float max, byte* buffer, int length)
    {
        float range = max - min;
        float norm = math.saturate((value - min) / range);
        uint scaled = (uint)(norm * ((1u << (length << 3)) - 1) + 0.5f);
        int shift = (length - 1) << 3;
        if (length == 4)
        {
            *(uint*)buffer = *(uint*)&value;
            return;
        }
        buffer[0] = (byte)(scaled >> shift);
        if (length > 1) *(ushort*)(buffer + length - 2) = (ushort)scaled;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static float DecompressFloatCore(byte* buffer, float min, float max, int length)
    {
        if (length == 4) return *(float*)buffer;
        uint value1 = ((uint)buffer[0] << 16) | *(ushort*)(buffer + 1);
        uint value2 = *(ushort*)buffer;
        uint value3 = buffer[0];
        uint value = length == 3 ? value1 : (length == 2 ? value2 : value3);
        float inv = length == 3 ? inv16777215 : (length == 2 ? inv65535 : inv255);
        return min + value * inv * (max - min);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector2Core(byte* p, in float2 value, int xBytes, int yBytes, in float2 min, in float2 max)
    {
        float2 range = max - min;
        float2 norm = math.saturate((value - min) / range);
        uint scaledX = (uint)(norm.x * ((1u << (xBytes << 3)) - 1) + 0.5f);
        int shiftX = (xBytes - 1) << 3;
        if (xBytes == 4)
        {
            float tempX = value.x;
            *(uint*)p = *(uint*)&tempX;
        }
        else
        {
            p[0] = (byte)(scaledX >> shiftX);
            if (xBytes > 1) *(ushort*)(p + xBytes - 2) = (ushort)scaledX;
        }
        p += xBytes;
        uint scaledY = (uint)(norm.y * ((1u << (yBytes << 3)) - 1) + 0.5f);
        int shiftY = (yBytes - 1) << 3;
        if (yBytes == 4)
        {
            float tempY = value.y;
            *(uint*)p = *(uint*)&tempY;
        }
        else
        {
            p[0] = (byte)(scaledY >> shiftY);
            if (yBytes > 1) *(ushort*)(p + yBytes - 2) = (ushort)scaledY;
        }
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector2Core(byte* ptr, int xBytes, int yBytes, in float2 min, in float2 max, ref float2 result)
    {
        if (xBytes == 4)
        {
            result.x = *(float*)ptr;
        }
        else
        {
            uint valueX1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueX2 = *(ushort*)ptr;
            uint valueX3 = ptr[0];
            uint valueX = xBytes == 3 ? valueX1 : (xBytes == 2 ? valueX2 : valueX3);
            float invX = xBytes == 3 ? inv16777215 : (xBytes == 2 ? inv65535 : inv255);
            result.x = min.x + valueX * invX * (max.x - min.x);
        }
        ptr += xBytes;
        if (yBytes == 4)
        {
            result.y = *(float*)ptr;
        }
        else
        {
            uint valueY1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueY2 = *(ushort*)ptr;
            uint valueY3 = ptr[0];
            uint valueY = yBytes == 3 ? valueY1 : (yBytes == 2 ? valueY2 : valueY3);
            float invY = yBytes == 3 ? inv16777215 : (yBytes == 2 ? inv65535 : inv255);
            result.y = min.y + valueY * invY * (max.y - min.y);
        }
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector3Core(byte* p, in float3 value, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max)
    {
        float3 range = max - min;
        float3 norm = math.saturate((value - min) / range);
        uint scaledX = (uint)(norm.x * ((1u << (xBytes << 3)) - 1) + 0.5f);
        int shiftX = (xBytes - 1) << 3;
        if (xBytes == 4)
        {
            float tempX = value.x;
            *(uint*)p = *(uint*)&tempX;
        }
        else
        {
            p[0] = (byte)(scaledX >> shiftX);
            if (xBytes > 1) *(ushort*)(p + xBytes - 2) = (ushort)scaledX;
        }
        p += xBytes;
        uint scaledY = (uint)(norm.y * ((1u << (yBytes << 3)) - 1) + 0.5f);
        int shiftY = (yBytes - 1) << 3;
        if (yBytes == 4)
        {
            float tempY = value.y;
            *(uint*)p = *(uint*)&tempY;
        }
        else
        {
            p[0] = (byte)(scaledY >> shiftY);
            if (yBytes > 1) *(ushort*)(p + yBytes - 2) = (ushort)scaledY;
        }
        p += yBytes;
        uint scaledZ = (uint)(norm.z * ((1u << (zBytes << 3)) - 1) + 0.5f);
        int shiftZ = (zBytes - 1) << 3;
        if (zBytes == 4)
        {
            float tempZ = value.z;
            *(uint*)p = *(uint*)&tempZ;
        }
        else
        {
            p[0] = (byte)(scaledZ >> shiftZ);
            if (zBytes > 1) *(ushort*)(p + zBytes - 2) = (ushort)scaledZ;
        }
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector3Core(byte* ptr, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max, ref float3 result)
    {
        if (xBytes == 4)
        {
            result.x = *(float*)ptr;
        }
        else
        {
            uint valueX1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueX2 = *(ushort*)ptr;
            uint valueX3 = ptr[0];
            uint valueX = xBytes == 3 ? valueX1 : (xBytes == 2 ? valueX2 : valueX3);
            float invX = xBytes == 3 ? inv16777215 : (xBytes == 2 ? inv65535 : inv255);
            result.x = min.x + valueX * invX * (max.x - min.x);
        }
        ptr += xBytes;
        if (yBytes == 4)
        {
            result.y = *(float*)ptr;
        }
        else
        {
            uint valueY1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueY2 = *(ushort*)ptr;
            uint valueY3 = ptr[0];
            uint valueY = yBytes == 3 ? valueY1 : (yBytes == 2 ? valueY2 : valueY3);
            float invY = yBytes == 3 ? inv16777215 : (yBytes == 2 ? inv65535 : inv255);
            result.y = min.y + valueY * invY * (max.y - min.y);
        }
        ptr += yBytes;
        if (zBytes == 4)
        {
            result.z = *(float*)ptr;
        }
        else
        {
            uint valueZ1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueZ2 = *(ushort*)ptr;
            uint valueZ3 = ptr[0];
            uint valueZ = zBytes == 3 ? valueZ1 : (zBytes == 2 ? valueZ2 : valueZ3);
            float invZ = zBytes == 3 ? inv16777215 : (zBytes == 2 ? inv65535 : inv255);
            result.z = min.z + valueZ * invZ * (max.z - min.z);
        }
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector4Core(byte* p, in float4 value, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max)
    {
        float4 range = max - min;
        float4 norm = math.saturate((value - min) / range);
        uint scaledX = (uint)(norm.x * ((1u << (xBytes << 3)) - 1) + 0.5f);
        int shiftX = (xBytes - 1) << 3;
        if (xBytes == 4)
        {
            float tempX = value.x;
            *(uint*)p = *(uint*)&tempX;
        }
        else
        {
            p[0] = (byte)(scaledX >> shiftX);
            if (xBytes > 1) *(ushort*)(p + xBytes - 2) = (ushort)scaledX;
        }
        p += xBytes;
        uint scaledY = (uint)(norm.y * ((1u << (yBytes << 3)) - 1) + 0.5f);
        int shiftY = (yBytes - 1) << 3;
        if (yBytes == 4)
        {
            float tempY = value.y;
            *(uint*)p = *(uint*)&tempY;
        }
        else
        {
            p[0] = (byte)(scaledY >> shiftY);
            if (yBytes > 1) *(ushort*)(p + yBytes - 2) = (ushort)scaledY;
        }
        p += yBytes;
        uint scaledZ = (uint)(norm.z * ((1u << (zBytes << 3)) - 1) + 0.5f);
        int shiftZ = (zBytes - 1) << 3;
        if (zBytes == 4)
        {
            float tempZ = value.z;
            *(uint*)p = *(uint*)&tempZ;
        }
        else
        {
            p[0] = (byte)(scaledZ >> shiftZ);
            if (zBytes > 1) *(ushort*)(p + zBytes - 2) = (ushort)scaledZ;
        }
        p += zBytes;
        uint scaledW = (uint)(norm.w * ((1u << (wBytes << 3)) - 1) + 0.5f);
        int shiftW = (wBytes - 1) << 3;
        if (wBytes == 4)
        {
            float tempW = value.w;
            *(uint*)p = *(uint*)&tempW;
        }
        else
        {
            p[0] = (byte)(scaledW >> shiftW);
            if (wBytes > 1) *(ushort*)(p + wBytes - 2) = (ushort)scaledW;
        }
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector4Core(byte* ptr, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max, ref float4 result)
    {
        if (xBytes == 4)
        {
            result.x = *(float*)ptr;
        }
        else
        {
            uint valueX1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueX2 = *(ushort*)ptr;
            uint valueX3 = ptr[0];
            uint valueX = xBytes == 3 ? valueX1 : (xBytes == 2 ? valueX2 : valueX3);
            float invX = xBytes == 3 ? inv16777215 : (xBytes == 2 ? inv65535 : inv255);
            result.x = min.x + valueX * invX * (max.x - min.x);
        }
        ptr += xBytes;
        if (yBytes == 4)
        {
            result.y = *(float*)ptr;
        }
        else
        {
            uint valueY1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueY2 = *(ushort*)ptr;
            uint valueY3 = ptr[0];
            uint valueY = yBytes == 3 ? valueY1 : (yBytes == 2 ? valueY2 : valueY3);
            float invY = yBytes == 3 ? inv16777215 : (yBytes == 2 ? inv65535 : inv255);
            result.y = min.y + valueY * invY * (max.y - min.y);
        }
        ptr += yBytes;
        if (zBytes == 4)
        {
            result.z = *(float*)ptr;
        }
        else
        {
            uint valueZ1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueZ2 = *(ushort*)ptr;
            uint valueZ3 = ptr[0];
            uint valueZ = zBytes == 3 ? valueZ1 : (zBytes == 2 ? valueZ2 : valueZ3);
            float invZ = zBytes == 3 ? inv16777215 : (zBytes == 2 ? inv65535 : inv255);
            result.z = min.z + valueZ * invZ * (max.z - min.z);
        }
        ptr += zBytes;
        if (wBytes == 4)
        {
            result.w = *(float*)ptr;
        }
        else
        {
            uint valueW1 = ((uint)ptr[0] << 16) | *(ushort*)(ptr + 1);
            uint valueW2 = *(ushort*)ptr;
            uint valueW3 = ptr[0];
            uint valueW = wBytes == 3 ? valueW1 : (wBytes == 2 ? valueW2 : valueW3);
            float invW = wBytes == 3 ? inv16777215 : (wBytes == 2 ? inv65535 : inv255);
            result.w = min.w + valueW * invW * (max.w - min.w);
        }
    }
}