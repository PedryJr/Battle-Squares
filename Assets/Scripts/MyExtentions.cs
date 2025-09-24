using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;
public static class MyExtentions
{

    public static int scoreCapture;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] EncodePosition(float x, float y)
    {

        int scaledX = (int)(x * 32);
        int scaledY = (int)(y * 32);

        byte x1 = (byte)((scaledX >> 12) & 0xF);
        byte x2 = (byte)((scaledX >> 8) & 0xF);
        byte x3 = (byte)((scaledX >> 4) & 0xF);
        byte x4 = (byte)(scaledX & 0xF);

        byte y1 = (byte)((scaledY >> 12) & 0xF);
        byte y2 = (byte)((scaledY >> 8) & 0xF);
        byte y3 = (byte)((scaledY >> 4) & 0xF);
        byte y4 = (byte)(scaledY & 0xF);

        byte[] result = new byte[4];
        result[0] = (byte)((x1 << 4) | y1);
        result[1] = (byte)((x2 << 4) | y2);
        result[2] = (byte)((x3 << 4) | y3);
        result[3] = (byte)((x4 << 4) | y4);

        return result;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float, float) DecodePosition(byte[] bytes)
    {

        int x1 = (bytes[0] >> 4) & 0xF;
        int y1 = bytes[0] & 0xF;

        int x2 = (bytes[1] >> 4) & 0xF;
        int y2 = bytes[1] & 0xF;

        int x3 = (bytes[2] >> 4) & 0xF;
        int y3 = bytes[2] & 0xF;

        int x4 = (bytes[3] >> 4) & 0xF;
        int y4 = bytes[3] & 0xF;

        int scaledX = (x1 << 12) | (x2 << 8) | (x3 << 4) | x4;
        int scaledY = (y1 << 12) | (y2 << 8) | (y3 << 4) | y4;

        float x = scaledX / 32.0f;
        float y = scaledY / 32.0f;

        return (x, y);

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] EncodeRotation(float rotation)
    {

        int scaledRotation = (int)((rotation / 360.0f) * 65535);

        byte highByte = (byte)((scaledRotation >> 8) & 0xFF);
        byte lowByte = (byte)(scaledRotation & 0xFF);

        byte[] result = new byte[2];
        result[0] = highByte;
        result[1] = lowByte;

        return result;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DecodeRotation(byte[] bytes)
    {

        if (bytes.Length != 2)
            throw new ArgumentException("Input must be exactly 2 bytes.");

        int highByte = bytes[0] & 0xFF;
        int lowByte = bytes[1] & 0xFF;

        int scaledRotation = (highByte << 8) | lowByte;

        float rotation = (scaledRotation / 65535.0f) * 360.0f;

        return rotation;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] EncodeFloat(float value)
    {

        float minValue = -1000.0f;
        float maxValue = 1000.0f;
        int range = (int)(maxValue - minValue);

        int scaledValue = (int)(((value - minValue) / range) * 16777215);

        scaledValue = math.max(0, math.min(16777215, scaledValue));

        byte byte1 = (byte)((scaledValue >> 16) & 0xFF);
        byte byte2 = (byte)((scaledValue >> 8) & 0xFF);
        byte byte3 = (byte)(scaledValue & 0xFF);

        return new byte[] { byte1, byte2, byte3 };

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DecodeFloat(byte[] bytes) => -1000.0f + ((((bytes[0] << 16) | (bytes[1] << 8) | bytes[2]) / 16777215.0f) * (int)(1000.0f - -1000.0f));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] EncodeNozzlePosition(float x, float y) => new byte[] { (byte)((math.clamp(x, -1, 1) + 1.0f) * 127.5f), (byte)((math.clamp(y, -1, 1) + 1.0f) * 127.5f) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float, float) DecodeNozzlePosition(byte[] bytes) => ((bytes[0] / 127.5f) - 1.0f, (bytes[1] / 127.5f) - 1.0f);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInOutCubic(float x) => x < 0.5 ? 4 * x * x * x : 1 - (float) math.pow(-2 * x + 2, 3) / 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInExpo(float x) => x == 0 ? 0 : (float)math.pow(2, 10 * x - 10);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInQuad(float x) => x * x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOutQuad(float x) => 1 - (1 - x) * (1 - x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 AngleToNormalizedCoordinate(float angle)
    {
        float radians = math.radians(angle);
        return new Vector2(math.cos(radians), math.sin(radians)).normalized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SanitizeMessage(string message) => Regex.Replace(message.Length > 120 ? message.Substring(0, 120) : message,
            @"[^\p{L}\p{N}\p{Sc}\p{Sm}\p{Mn}\p{Pc}\p{Pd}\p{Zs}.,<>{}|_+=!?;:'""\-\(\)]", string.Empty);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOnHover(float x) => 1 - math.pow(1 - x, 5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOffHover(float x) => ((1.70158f + 1) * x * x * x - 1.70158f * x * x) + (math.exp(-4.1f * x) * math.sin(-3.8f * x));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOnClick(float x) => 1 - math.pow(1 - x, 5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] BoolArrayToByteArray(bool[] boolArray)
    {

        int boolCount = boolArray.Length;
        int byteCount = (boolArray.Length + 7) / 8;
        byte[] byteArray = new byte[byteCount];

        ref byte byteSpace = ref MemoryMarshal.GetReference(byteArray.AsSpan());
        ref bool searchSpace = ref MemoryMarshal.GetReference(boolArray.AsSpan());

        for (int i = 0; i < boolCount; i++) if (Unsafe.Add(ref searchSpace, i)) Unsafe.Add(ref byteSpace, i / 8) |= (byte)(1 << (i % 8));
        return byteArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool[] ByteArrayToBoolArray(byte[] byteArray, int boolArrayLength)
    {
        bool[] boolArray = new bool[boolArrayLength];

        ref byte byteSpace = ref MemoryMarshal.GetReference(byteArray.AsSpan());
        ref bool searchSpace = ref MemoryMarshal.GetReference(boolArray.AsSpan());

        for (int i = 0; i < boolArrayLength; i++) Unsafe.Add(ref searchSpace, i) = (Unsafe.Add(ref byteSpace, i / 8) & (1 << (i % 8))) != 0;

        return boolArray;
    }

    public static string BoolArrayToString(bool[] boolArray)
    {
        if (boolArray.Length != 116)
        {
            boolArray = new bool[116];
            ref bool defaultSS = ref MemoryMarshal.GetReference(boolArray.AsSpan());
            for (int i = 0; i < boolArray.Length; i++) Unsafe.Add(ref defaultSS, i) = true;
        }

        byte[] byteArray = new byte[16];

        ref byte byteSpace = ref MemoryMarshal.GetReference(byteArray.AsSpan());
        ref bool searchSpace = ref MemoryMarshal.GetReference(boolArray.AsSpan());

        for (int i = 0; i < boolArray.Length; i++) if (Unsafe.Add(ref searchSpace, i)) Unsafe.Add(ref byteSpace, i / 8) |= (byte)(1 << (i % 8));

        StringBuilder hexString = new StringBuilder(byteArray.Length * 2);

        for (int i = 0; i < byteArray.Length; i++) hexString.Append(Unsafe.Add(ref byteSpace, i).ToString("X2"));

        StringBuilder formattedString = new StringBuilder();
        for (int i = 0; i < hexString.Length; i += 8)
        {
            if (i > 0) formattedString.Append('-');
            formattedString.Append(hexString.ToString().Substring(i, 8));
        }

        return formattedString.ToString();
    }

    public static bool[] StringToBoolArray(string encodedString)
    {
        string cleanedString = encodedString.Replace("-", "");

        int byteCount = cleanedString.Length / 2;
        byte[] byteArray = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            byteArray[i] = Convert.ToByte(cleanedString.Substring(i * 2, 2), 16);
        }

        if (byteArray.Length != 16)
        {
            throw new ArgumentException("Invalid encoded string length.");
        }

        bool[] boolArray = new bool[116];
        for (int i = 0; i < boolArray.Length; i++)
        {
            boolArray[i] = (byteArray[i / 8] & (1 << (i % 8))) != 0;
        }

        return boolArray;
    }


}
