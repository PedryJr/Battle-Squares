using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe static class BinaryTool
{

    //Burst code settings
    const bool compileSynchronously = false;
    const bool debug = false;
    const bool disableDirectCall = false;
    const bool disableSafetyChecks = true;
    const FloatMode floatMode = FloatMode.Fast;
    const FloatPrecision floatPrecision = FloatPrecision.Low;
    const OptimizeFor optimizeFor = OptimizeFor.Performance;



    //Generic float compression
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DecompressFloat(byte[] buffer, float min, float max)
    {
        fixed (byte* ptr = buffer) return DecompressFloat(ptr, min, max, buffer.Length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressFloat(float value, int bytes, float min, float max)
    {
        Span<byte> result = stackalloc byte[bytes];
        fixed (byte* ptr = result) CompressFloat(value, min, max, ptr, bytes);
        return result.ToArray();
    }



    //Vector2 compression/decompression
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 DecompressVector2(byte[] buffer, int xBytes, int yBytes, in Vector2 min, in Vector2 max)
    {
        float2 decom = new float2();
        fixed (byte* ptr = buffer) DecompressVector2(ptr, xBytes, yBytes, min, max, ref decom);
        return decom;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector2(byte* ptr, int xBytes, int yBytes, in float2 min, in float2 max, ref float2 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector2(in float2 value, int xBytes, int yBytes, in float2 min, in float2 max)
    {
        byte[] buffer = new byte[xBytes + yBytes];
        CompressVector2(ref buffer, value, xBytes, yBytes, min, max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector2(ref byte[] buffer, in float2 value, int xBytes, int yBytes, in float2 min, in float2 max)
    {
        int bufferSize = xBytes + yBytes;
        if(!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector2(ptr, value, xBytes, yBytes, min, max);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector2(byte* ptr, in float2 value, int xBytes, int yBytes, in float2 min, in float2 max)
    {
        Span<byte> compressedX = stackalloc byte[xBytes];
        Span<byte> compressedY = stackalloc byte[yBytes];

        fixed (byte* ptrX = compressedX)
        fixed (byte* ptrY = compressedY)
        {
            CompressFloat(value.x, min.x, max.x, ptrX, xBytes);
            CompressFloat(value.y, min.y, max.y, ptrY, yBytes);
        }

        int xStart = 0;
        int yStart = xBytes;

        for (int x = 0; x < xBytes; x++) ptr[xStart + x] = compressedX[x];
        for (int y = 0; y < yBytes; y++) ptr[yStart + y] = compressedY[y];
    }



    //Vector3 compression/decompression
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 DecompressVector3(byte[] buffer, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max)
    {
        float3 decom = new float3();
        fixed (byte* ptr = buffer) DecompressVector3(ptr, xBytes, yBytes, zBytes, min, max, ref decom);
        return decom;
    }

    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector3(byte* ptr, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max, ref float3 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
        decom.z = DecompressFloat(ptr + xBytes + yBytes, min.z, max.z, zBytes);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector3(in float3 value, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max)
    {
        byte[] buffer = new byte[xBytes + yBytes + zBytes];
        CompressVector3(ref buffer, value, xBytes, yBytes, zBytes, min, max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector3(ref byte[] buffer, in float3 value, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max)
    {
        int bufferSize = xBytes + yBytes + zBytes;
        if (!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector3(ptr, value, xBytes, yBytes, zBytes, min, max);
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector3(byte* ptr, in float3 value, int xBytes, int yBytes, int zBytes, in float3 min, in float3 max)
    {
        Span<byte> compressedX = stackalloc byte[xBytes];
        Span<byte> compressedY = stackalloc byte[yBytes];
        Span<byte> compressedZ = stackalloc byte[zBytes];

        fixed (byte* ptrX = compressedX)
        fixed (byte* ptrY = compressedY)
        fixed (byte* ptrZ = compressedZ)
        {
            CompressFloat(value.x, min.x, max.x, ptrX, xBytes);
            CompressFloat(value.y, min.y, max.y, ptrY, yBytes);
            CompressFloat(value.z, min.z, max.z, ptrZ, zBytes);
        }

        int xStart = 0;
        int yStart = xBytes;
        int zStart = xBytes + yBytes;

        for (int x = 0; x < xBytes; x++) ptr[xStart + x] = compressedX[x];
        for (int y = 0; y < yBytes; y++) ptr[yStart + y] = compressedY[y];
        for (int z = 0; z < zBytes; z++) ptr[zStart + z] = compressedZ[z];
    }



    //Vector4 compression/decompression
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 DecompressVector4(byte[] buffer, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max)
    {
        float4 decom = new float4();
        fixed (byte* ptr = buffer) DecompressVector4(ptr, xBytes, yBytes, zBytes, wBytes, min, max, ref decom);
        return decom;
    }
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void DecompressVector4(byte* ptr, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max, ref float4 decom)
    {
        decom.x = DecompressFloat(ptr, min.x, max.x, xBytes);
        decom.y = DecompressFloat(ptr + xBytes, min.y, max.y, yBytes);
        decom.z = DecompressFloat(ptr + xBytes + yBytes, min.z, max.z, zBytes);
        decom.w = DecompressFloat(ptr + xBytes + yBytes + zBytes, min.w, max.w, wBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressVector4(in float4 value, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max)
    {
        byte[] buffer = new byte[xBytes + yBytes + zBytes + wBytes];
        CompressVector4(ref buffer, value, xBytes, yBytes, zBytes, wBytes, min, max);
        return buffer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompressVector4(ref byte[] buffer, float4 value, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, Vector4 max)
    {
        int bufferSize = xBytes + yBytes + zBytes + wBytes;
        if (!ValidateBuffer(buffer, bufferSize)) buffer = new byte[bufferSize];
        fixed (byte* ptr = buffer) CompressVector4(ptr, value, xBytes, yBytes, zBytes, wBytes, min, max);
    }

    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressVector4(byte* ptr, in float4 value, int xBytes, int yBytes, int zBytes, int wBytes, in float4 min, in float4 max)
    {
        Span<byte> compressedX = stackalloc byte[xBytes];
        Span<byte> compressedY = stackalloc byte[yBytes];
        Span<byte> compressedZ = stackalloc byte[zBytes];
        Span<byte> compressedW = stackalloc byte[wBytes];

        fixed (byte* ptrX = compressedX)
        fixed (byte* ptrY = compressedY)
        fixed (byte* ptrZ = compressedZ)
        fixed (byte* ptrW = compressedW)
        {
            CompressFloat(value.x, min.x, max.x, ptrX, xBytes);
            CompressFloat(value.y, min.y, max.y, ptrY, yBytes);
            CompressFloat(value.z, min.z, max.z, ptrZ, zBytes);
            CompressFloat(value.z, min.z, max.z, ptrW, wBytes);
        }

        int xStart = 0;
        int yStart = xBytes;
        int zStart = xBytes + yBytes;
        int wStart = xBytes + yBytes + zBytes;

        for (int x = 0; x < xBytes; x++) ptr[xStart + x] = compressedX[x];
        for (int y = 0; y < yBytes; y++) ptr[yStart + y] = compressedY[y];
        for (int z = 0; z < zBytes; z++) ptr[zStart + z] = compressedZ[z];
        for (int w = 0; w < wBytes; w++) ptr[wStart + w] = compressedW[w];
    }



    //Internal float compression
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static void CompressFloat(float value, float min, float max, byte* buffer, int length)
    {
        float normalized = math.clamp((value - min) / (max - min), 0f, 1f);

        switch (length)
        {
            case 4:
                {
                    uint asInt = math.asuint(value);
                    byte* bytes = (byte*)&asInt;
                    for (int i = 0; i < 4; i++) buffer[i] = bytes[i];
                    break;
                }
            case 3:
                {
                    uint quant24 = (uint)math.round(normalized * 16777215f);
                    buffer[0] = (byte)((quant24 >> 16) & 0xFF);
                    buffer[1] = (byte)((quant24 >> 8) & 0xFF);
                    buffer[2] = (byte)(quant24 & 0xFF);
                    break;
                }
            case 2:
                {
                    ushort quant16 = (ushort)math.round(normalized * 65535f);
                    buffer[0] = (byte)((quant16 >> 8) & 0xFF);
                    buffer[1] = (byte)(quant16 & 0xFF);
                    break;
                }
            case 1:
                buffer[0] = (byte)math.round(normalized * 255f);
                break;
        }
    }

    //Internal float decompression
    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode, FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static float DecompressFloat(byte* buffer, float min, float max, int length)
    {
        float normalized = 0f;

        switch (length)
        {
            case 4:
                uint asInt = *(uint*)buffer;
                float value = math.asfloat(asInt);
                return value;

            case 3:
                uint quant24 = ((uint)buffer[0] << 16) | ((uint)buffer[1] << 8) | buffer[2];
                normalized = quant24 / 16777215f;
                break;

            case 2:
                ushort quant16 = (ushort)((buffer[0] << 8) | buffer[1]);
                normalized = quant16 / 65535f;
                break;

            case 1:
                byte quant8 = buffer[0];
                normalized = quant8 / 255f;
                break;
        }
        return normalized * (max - min) + min;
    }

    //Internal buffer validation
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidateBuffer(byte[] buffer, int validationSize) => (buffer != null && buffer.Length == validationSize);

}
