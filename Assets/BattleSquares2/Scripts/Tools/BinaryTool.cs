using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
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

    private static byte[][] cache = new byte[17][] 
    {
        new byte[1], // _ignore
        new byte[1],
        new byte[2],
        new byte[3],
        new byte[4],
        new byte[5],
        new byte[6],
        new byte[7],
        new byte[8],
        new byte[9],
        new byte[10],
        new byte[11],
        new byte[12],
        new byte[13],
        new byte[14],
        new byte[15],
        new byte[16]
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DecompressFloat(byte[] buffer, float min, float max)
    {
        fixed (byte* ptr = buffer) return DecompressFloat(ptr, min, max, buffer.Length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressFloatAlloc(float value, int bytes, float min, float max)
    {
        Span<byte> buffer = stackalloc byte[bytes];
        fixed (byte* ptr = buffer) CompressFloat(value, min, max, ptr, bytes);
        byte[] ret = new byte[buffer.Length];
        buffer.CopyTo(ret.AsSpan());
        return ret;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressFloatCache(float value, int bytes, float min, float max)
    {
        fixed (byte* ptr = cache[bytes]) CompressFloat(value, min, max, ptr, bytes);
        return cache[bytes];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressFloatPreAlloc(ref byte[] buffer, float value, int bytes, float min, float max)
    {
        fixed (byte* ptr = buffer) CompressFloat(value, min, max, ptr, bytes);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 DecompressVector2(byte[] buffer, int xBytes, int yBytes, float2 min, float2 max)
    {
        float2 decom = new float2();
        fixed (byte* ptr = buffer) DecompressVector2(ptr, xBytes, yBytes, ref min, ref max, ref decom);
        return decom;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector2(byte* ptr, int xBytes, int yBytes, ref float2 min, ref float2 max, ref float2 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector2Cached(float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        byte[] buffer = cache[xBytes + yBytes];
        fixed (byte* ptr = buffer) CompressVector2Fast(ptr, ref value, xBytes, yBytes, ref min, ref max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector2Alloc(float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        Span<byte> buffer = stackalloc byte[xBytes + yBytes];
        fixed (byte* ptr = buffer) CompressVector2Fast(ptr, ref value, xBytes, yBytes, ref min, ref max);
        byte[] ret = new byte[buffer.Length];
        buffer.CopyTo(ret.AsSpan());
        return ret;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector2PreAlloc(ref byte[] buffer, float2 value, int xBytes, int yBytes, float2 min, float2 max)
    {
        int bufferSize = xBytes + yBytes;
        if (!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector2Fast(ptr, ref value, xBytes, yBytes, ref min, ref max);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug,
        DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks,
        FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    public static void CompressVector2Fast(byte* p, ref float2 value, int xBytes, int yBytes, ref float2 min, ref float2 max)
    {
        float caster;
        float2 normalized = (value - min) * (1f / (max - min));
        float2 clamped = new float2(
            math.clamp(normalized.x, 0f, 1f),
            math.clamp(normalized.y, 0f, 1f)
        );
        caster = value.x;
        if (xBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (xBytes == 3) { uint q = (uint)(clamped.x * 16777215f + 0.5f); p[0] = (byte)(q >> 16); *(ushort*)(p + 1) = (ushort)q; }
        else if (xBytes == 2) *(ushort*)p = (ushort)(clamped.x * 65535f + 0.5f);
        else p[0] = (byte)(clamped.x * 255f + 0.5f);
        p += xBytes;
        caster = value.y;
        if (yBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (yBytes == 3) { uint q = (uint)(clamped.y * 16777215f + 0.5f); p[0] = (byte)(q >> 16); *(ushort*)(p + 1) = (ushort)q; }
        else if (yBytes == 2) *(ushort*)p = (ushort)(clamped.y * 65535f + 0.5f);
        else p[0] = (byte)(clamped.y * 255f + 0.5f);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 DecompressVector3(byte[] buffer, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        float3 decom = new float3();
        fixed (byte* ptr = buffer) DecompressVector3(ptr, xBytes, yBytes, zBytes, ref min, ref max, ref decom);
        return decom;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector3(byte* ptr, int xBytes, int yBytes, int zBytes, ref float3 min, ref float3 max, ref float3 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
        decom.z = DecompressFloat(ptr + xBytes + yBytes, min.z, max.z, zBytes);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector3Cached(float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        byte[] buffer = cache[xBytes + yBytes + zBytes];
        fixed (byte* ptr = buffer) CompressVector3Fast(ptr, ref value, xBytes, yBytes, zBytes, ref min, ref max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector3Alloc(float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        Span<byte> buffer = stackalloc byte[xBytes + yBytes + zBytes];
        fixed (byte* ptr = buffer) CompressVector3Fast(ptr, ref value, xBytes, yBytes, zBytes, ref min, ref max);
        byte[] ret = new byte[buffer.Length];
        buffer.CopyTo(ret.AsSpan());
        return ret;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector3PreAlloc(ref byte[] buffer, float3 value, int xBytes, int yBytes, int zBytes, float3 min, float3 max)
    {
        int bufferSize = xBytes + yBytes + zBytes;
        if (!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector3Fast(ptr, ref value, xBytes, yBytes, zBytes, ref min, ref max);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug,
        DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks,
        FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    public static void CompressVector3Fast(byte* p, ref float3 value, int xBytes, int yBytes, int zBytes, ref float3 min, ref float3 max)
    {
        float caster;
        float3 normalized = (value - min) * (1f / (max - min));
        float3 clamped = new float3(
            math.clamp(normalized.x, 0f, 1f),
            math.clamp(normalized.y, 0f, 1f),
            math.clamp(normalized.z, 0f, 1f)
        );
        caster = value.x;
        if (xBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (xBytes == 3) { uint q = (uint)(clamped.x * 16777215f + 0.5f); p[0] = (byte)(q >> 16); *(ushort*)(p + 1) = (ushort)q; }
        else if (xBytes == 2) *(ushort*)p = (ushort)(clamped.x * 65535f + 0.5f);
        else p[0] = (byte)(clamped.x * 255f + 0.5f);
        p += xBytes;
        caster = value.y;
        if (yBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (yBytes == 3) { uint q = (uint)(clamped.y * 16777215f + 0.5f); p[0] = (byte)(q >> 16); *(ushort*)(p + 1) = (ushort)q; }
        else if (yBytes == 2) *(ushort*)p = (ushort)(clamped.y * 65535f + 0.5f);
        else p[0] = (byte)(clamped.y * 255f + 0.5f);
        p += yBytes;
        caster = value.z;
        if (zBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (zBytes == 3) { uint q = (uint)(clamped.z * 16777215f + 0.5f); p[0] = (byte)(q >> 16); *(ushort*)(p + 1) = (ushort)q; }
        else if (zBytes == 2) *(ushort*)p = (ushort)(clamped.z * 65535f + 0.5f);
        else p[0] = (byte)(clamped.z * 255f + 0.5f);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 DecompressVector4(byte[] buffer, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        float4 decom = new float4();
        fixed (byte* ptr = buffer) DecompressVector4(ptr, xBytes, yBytes, zBytes, wBytes, ref min, ref max, ref decom);
        return decom;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector4(byte* ptr, int xBytes, int yBytes, int zBytes, int wBytes, ref float4 min, ref float4 max, ref float4 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
        decom.z = DecompressFloat(ptr + xBytes + yBytes, min.z, max.z, zBytes);
        decom.w = DecompressFloat(ptr + xBytes + yBytes + zBytes, min.w, max.w, wBytes);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector4Cached(float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        byte[] buffer = cache[xBytes + yBytes + zBytes + wBytes];
        fixed (byte* ptr = buffer) CompressVector4Fast(ptr, ref value, xBytes, yBytes, zBytes, wBytes, ref min, ref max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector4Alloc(float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        Span<byte> buffer = stackalloc byte[xBytes + yBytes + zBytes + wBytes];
        fixed (byte* ptr = buffer) CompressVector4Fast(ptr, ref value, xBytes, yBytes, zBytes, wBytes, ref min, ref max);
        byte[] ret = new byte[buffer.Length];
        buffer.CopyTo(ret.AsSpan());
        return ret;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector4PreAlloc(ref byte[] buffer, float4 value, int xBytes, int yBytes, int zBytes, int wBytes, float4 min, float4 max)
    {
        int bufferSize = xBytes + yBytes + zBytes + wBytes;
        if (!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector4Fast(ptr, ref value, xBytes, yBytes, zBytes, wBytes, ref min, ref max);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug,
        DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks,
        FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    public static void CompressVector4Fast(byte* p, ref float4 value, int xBytes, int yBytes, int zBytes, int wBytes, ref float4 min, ref float4 max)
    {
        float caster;
        float4 normalized, clamped;
        normalized = (value - min) * (1f / (max - min));

        clamped = new float4(
            normalized.x < 0f ? 0f : (normalized.x > 1f ? 1f : normalized.x),
            normalized.y < 0f ? 0f : (normalized.y > 1f ? 1f : normalized.y),
            normalized.z < 0f ? 0f : (normalized.z > 1f ? 1f : normalized.z),
            normalized.w < 0f ? 0f : (normalized.w > 1f ? 1f : normalized.w)
        );
        caster = value.x;
        if (xBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (xBytes == 3)
        {
            uint q = (uint)(clamped.x * 16777215f + 0.5f);
            p[0] = (byte)(q >> 16);
            *(ushort*)(p + 1) = (ushort)q;
        }
        else if (xBytes == 2) *(ushort*)p = (ushort)(clamped.x * 65535f + 0.5f);
        else p[0] = (byte)(clamped.x * 255f + 0.5f);
        p += xBytes;
        caster = value.y;
        if (yBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (yBytes == 3)
        {
            uint q = (uint)(clamped.y * 16777215f + 0.5f);
            p[0] = (byte)(q >> 16);
            *(ushort*)(p + 1) = (ushort)q;
        }
        else if (yBytes == 2) *(ushort*)p = (ushort)(clamped.y * 65535f + 0.5f);
        else p[0] = (byte)(clamped.y * 255f + 0.5f);
        p += yBytes;
        caster = value.z;
        if (zBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (zBytes == 3)
        {
            uint q = (uint)(clamped.z * 16777215f + 0.5f);
            p[0] = (byte)(q >> 16);
            *(ushort*)(p + 1) = (ushort)q;
        }
        else if (zBytes == 2) *(ushort*)p = (ushort)(clamped.z * 65535f + 0.5f);
        else p[0] = (byte)(clamped.z * 255f + 0.5f);
        p += zBytes;
        caster = value.w;
        if (wBytes == 4) *(uint*)p = *(uint*)&caster;
        else if (wBytes == 3)
        {
            uint q = (uint)(clamped.w * 16777215f + 0.5f);
            p[0] = (byte)(q >> 16);
            *(ushort*)(p + 1) = (ushort)q;
        }
        else if (wBytes == 2) *(ushort*)p = (ushort)(clamped.w * 65535f + 0.5f);
        else p[0] = (byte)(clamped.w * 255f + 0.5f);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressFloat(float value, float min, float max, byte* buffer, int length)
    {
        if (length == 4)
        {
            *(float*)buffer = value;
            return;
        }
        float normalized = (value - min) / (max - min);
        normalized = normalized < 0f ? 0f : (normalized > 1f ? 1f : normalized);

        if (length == 3)
        {
            uint q = (uint)(normalized * 16777215f + 0.5f);
            buffer[0] = (byte)(q >> 16);
            *(ushort*)(buffer + 1) = (ushort)q; 
        }
        else if (length == 2) *(ushort*)buffer = (ushort)(normalized * 65535f + 0.5f);
        else buffer[0] = (byte)(normalized * 255f + 0.5f);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static float DecompressFloat(byte* buffer, float min, float max, int length)
    {
        if (length == 4) return *(float*)buffer;
        float normalized;
        if (length == 3) normalized = (((uint)buffer[0] << 16) | *(ushort*)(buffer + 1)) * inv16777215;
        else if (length == 2) normalized = (*(ushort*)buffer) * inv65535;
        else normalized = buffer[0] * inv255;
        return min + normalized * (max - min);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateBuffer(byte[] buffer, int validationSize) => (buffer != null && buffer.Length == validationSize);

}
