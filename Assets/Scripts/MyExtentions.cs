using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using static BinaryVectors;

[BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
    DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
    FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
public static class MyExtentions
{

    public const bool compileSynchronously = false;
    public const bool debug = false;
    public const bool disableDirectCall = false;
    public const bool disableSafetyChecks = true;
    public const FloatMode floatMode = FloatMode.Fast;
    public const FloatPrecision floatPrecision = FloatPrecision.Low;
    public const OptimizeFor optimizeFor = OptimizeFor.Performance;

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
        if (bytes.Length != 2) throw new ArgumentException("Input must be exactly 2 bytes."); 
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
    public static float EaseInOutCubic(float x) => x < 0.5 ? 4 * x * x * x : 1 - (float)math.pow(-2 * x + 2, 3) / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInExpo(float x) => x == 0 ? 0 : (float)math.pow(2, 10 * x - 10);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInQuad(float x) => x * x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOutQuad(float x) => 1 - (1 - x) * (1 - x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 DegreesToVector2(float angleInDegrees) => RadiansToVector2(Mathf.Deg2Rad * angleInDegrees);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 RadiansToVector2(float angleInRadians) => new Vector2(math.cos(angleInRadians), math.sin(angleInRadians));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Vector2ToDegrees(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Vector2ToRadians(Vector2 direction) => Mathf.Atan2(direction.y, direction.x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SanitizeMessage(string message, int maxLength = 120) => Regex.Replace(message.Length > maxLength ? message.Substring(0, maxLength) : message,
            @"[^\p{L}\p{N}\p{Sc}\p{Sm}\p{Mn}\p{Pc}\p{Pd}\p{Zs}!@#$%\^&\*\(\)_\+\-=\[\]\\\{\}\|;:'"",\.\/<>?\`~]", string.Empty);
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


    public const float PosABS = 64f;
    public const float VelABS = 30f;
    public const float AngVelABS = 1000f;
    const byte PlayerPositionXBytes = 3;
    const byte PlayerPositionYBytes = 3;
    const byte PlayerVelocityXBytes = 3;
    const byte PlayerVelocityYBytes = 3;
    const byte PlayerRotBytes = 3;
    const byte PlayerAngVelBytes = 3;

    public const float MaxDeg = 360f;
    const float MaxRot = MaxDeg * Mathf.Deg2Rad;
    const float MinRot = 0f;
    const float MaxAngVel = AngVelABS * Mathf.Deg2Rad;
    const float MinAngVel = -AngVelABS * Mathf.Deg2Rad;
    const float MinPos = -PosABS;
    const float MaxPos = PosABS;
    const float MinVel = -VelABS;
    const float MaxVel = VelABS;

    const byte TotalBytes =
        PlayerPositionXBytes + PlayerPositionYBytes +
        PlayerVelocityXBytes + PlayerVelocityYBytes +
        PlayerRotBytes + PlayerAngVelBytes;

    const byte PlayerPositionBytes = PlayerPositionXBytes + PlayerPositionYBytes;
    const byte PlayerVelocityBytes = PlayerVelocityXBytes + PlayerVelocityYBytes;

    const byte PositionBufferOffset = 0;
    const byte VelocityBufferOffset = PlayerPositionBytes;
    const byte RotationBufferOffset = VelocityBufferOffset + PlayerVelocityBytes;
    const byte AngularVBufferOffset = RotationBufferOffset + PlayerRotBytes;

    static SByte2 playerPositionCompressor = new SByte2() 
    { 
        byteVec = { data = new byte[4] },
        min = { x = MinPos, y = MinPos }, 
        max = { x = MaxPos, y = MaxPos },
        xBytes = PlayerPositionXBytes,
        yBytes = PlayerPositionYBytes,
    };

    static SByte2 playerVelocityCompressor = new SByte2()
    {
        byteVec = { data = new byte[4] },
        min = { x = MinVel, y = MinVel },
        max = { x = MaxVel, y = MaxVel },
        xBytes = PlayerVelocityXBytes,
        yBytes = PlayerVelocityYBytes,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] CompressPlayerPosition(Vector2 pos)
    {
        playerPositionCompressor.SetFromVec2(pos);
        return playerPositionCompressor.GetByte2().data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector2 DecompressPlayerPosition(byte[] pos)
    {
        playerPositionCompressor.SetFromByteArr(pos);
        return playerPositionCompressor.GetVec2();
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] CompressPlayerVelocity(Vector2 vel)
    {
        playerVelocityCompressor.SetFromVec2(vel);
        return playerVelocityCompressor.GetByte2().data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector2 DecompressPlayerVelocity(byte[] vel)
    {
        playerVelocityCompressor.SetFromByteArr(vel);
        return playerVelocityCompressor.GetVec2();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] CompressPlayerRotation(float zAngles)
    {
        return BinaryTool.CompressFloatAlloc(Mathf.Repeat(zAngles, MaxDeg) * Mathf.Deg2Rad, PlayerRotBytes, MinRot, MaxRot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float DecompressPlayerRotation(byte[] data)
    {
        return BinaryTool.DecompressFloat(data, MinRot, MaxRot) * Mathf.Rad2Deg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] CompressPlayerAngularVelocity(float zAngles)
    {
        return BinaryTool.CompressFloatAlloc(zAngles * Mathf.Deg2Rad, PlayerAngVelBytes, MinAngVel, MaxAngVel);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float DecompressPlayerAngularVelocity(byte[] data)
    {
        return BinaryTool.DecompressFloat(data, MinAngVel, MaxAngVel) * Mathf.Rad2Deg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] CompressPlayerRigidbody(Rigidbody2D rb)
    {
        byte[] buffer = new byte[TotalBytes];
        Span<byte> span = buffer.AsSpan();
        CompressPlayerPosition(rb.position).CopyTo(span.Slice(PositionBufferOffset, PlayerPositionBytes));
        CompressPlayerVelocity(rb.linearVelocity).CopyTo(span.Slice(VelocityBufferOffset, PlayerVelocityBytes));
        CompressPlayerRotation(rb.rotation).CopyTo(span.Slice(RotationBufferOffset, PlayerRotBytes));
        CompressPlayerAngularVelocity(rb.angularVelocity).CopyTo(span.Slice(AngularVBufferOffset, PlayerAngVelBytes));
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecompressRigidbody(
        byte[] data,
        Rigidbody2D rb,
        float usedFrequency
    )
    {
        Vector2 serverPos =
            DecompressPlayerPosition(
                Slice(data, PositionBufferOffset, PlayerPositionBytes)
            );

        Vector2 serverVel =
            DecompressPlayerVelocity(
                Slice(data, VelocityBufferOffset, PlayerVelocityBytes)
            );

        float serverRot =
            DecompressPlayerRotation(
                Slice(data, RotationBufferOffset, PlayerRotBytes)
            );

        float serverAngVel =
            DecompressPlayerAngularVelocity(
                Slice(data, AngularVBufferOffset, PlayerAngVelBytes)
            );

        NetworkTimeSystem timeSystem = NetworkTimeSystem.ServerTimeSystem();
        double now = timeSystem.LocalTime;
        double remote = NetworkTimeSystem.ServerTimeSystem().ServerTime;


        double highPrecisionLatency = now - remote;
        float lateness = (float) highPrecisionLatency;
        lateness = Mathf.Clamp(lateness, 0f, 0.25f); 

        Vector2 predictedPos = serverPos + serverVel * lateness;
        float predictedRot = serverRot + serverAngVel * lateness;

        float error = Vector2.Distance(rb.position, predictedPos);

        float positionSharpness = Mathf.Lerp(
            0.5f,
            0.2f,
            Mathf.InverseLerp(1f, 200f, usedFrequency)
        );

        if (error > 1f)
            positionSharpness = 1f;

        rb.position = Vector2.Lerp(
            rb.position,
            predictedPos,
            positionSharpness
        );

        rb.rotation = Mathf.LerpAngle(
            rb.rotation,
            predictedRot,
            positionSharpness
        );

        rb.linearVelocity = serverVel;
        rb.angularVelocity = serverAngVel;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] Slice(byte[] src, int offset, int length)
    {
        byte[] result = new byte[length];
        for (int i = 0; i < length; i++) result[i] = src[offset + i];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetDirSpanContents(in Span<Vector2> span)
    {
        span[0] = new Vector2(1f, 0f);
        span[1] = new Vector2(0.7071f, 0.7071f);
        span[2] = new Vector2(0f, 1f);
        span[3] = new Vector2(-0.7071f, 0.7071f);
        span[4] = new Vector2(-1f, 0f);
        span[5] = new Vector2(-0.7071f, -0.7071f);
        span[6] = new Vector2(0f, -1f);
        span[7] = new Vector2(0.7071f, -0.7071f);
    }
    private const int DIRS_COUNT = 8;
    private const Int32 ENVIRONTMENT_MASK = 0b00000000000000000000001000000000;
    private static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[1];

    public static RaycastHit2D GetClosestEnvironmentPoint(Vector2 origin, float maxDistance = 100f)
    {
        Span<Vector2> DIRS_8 = stackalloc Vector2[DIRS_COUNT];
        SetDirSpanContents(DIRS_8);
        ref RaycastHit2D hitRef = ref hitBuffer[0];


        float shortestDistance = float.PositiveInfinity;
        RaycastHit2D closestHit = default;

        for (int i = 0; i < DIRS_COUNT; i++)
        {
            int hitCount = Physics2D.RaycastNonAlloc(origin, DIRS_8[i], hitBuffer, maxDistance, ENVIRONTMENT_MASK);
            if (hitCount <= 0) continue;
            float dist = hitRef.distance;
            if (dist >= shortestDistance) continue;
            shortestDistance = dist;
            closestHit = hitRef;
        }

        return closestHit;
    }

    public static unsafe string RemoveInvisibleChars(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var span = input.AsSpan();
        if (span.Length == 0) return string.Empty;

        fixed (char* pInput = &MemoryMarshal.GetReference(span))
        {
            int pInputLength = span.Length;
            char* pOutput = stackalloc char[pInputLength];
            int pOutputLength = 0;

            // Cast char* → ushort* to satisfy Burst
            RemoveInvisibleCharsInternal(
                (ushort*)pOutput, ref pOutputLength,
                (ushort*)pInput, pInputLength);

            return new string(pOutput, 0, pOutputLength);
        }
    }

    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    public static unsafe void RemoveInvisibleCharsInternal(
        ushort* pOutput, ref int pOutputLength,
        ushort* pInput, int pInputLength)
    {
        ushort* current = pInput;
        ushort* writePtr = pOutput;
        ushort* endVec = current + (pInputLength & ~7);
        ushort* endPtr = current + pInputLength;

        while (current < endVec)
        {
            ulong* src = (ulong*)current;
            ulong val0 = src[0];
            ulong val1 = src[1];

            ushort c0 = (ushort)(val0 & 0xFFFF);
            ushort c1 = (ushort)((val0 >> 16) & 0xFFFF);
            ushort c2 = (ushort)((val0 >> 32) & 0xFFFF);
            ushort c3 = (ushort)((val0 >> 48) & 0xFFFF);
            ushort c4 = (ushort)(val1 & 0xFFFF);
            ushort c5 = (ushort)((val1 >> 16) & 0xFFFF);
            ushort c6 = (ushort)((val1 >> 32) & 0xFFFF);
            ushort c7 = (ushort)((val1 >> 48) & 0xFFFF);

            int keep0 = (c0 >= 32 && (c0 < 0x7F || c0 > 0x9F) && c0 != 0x200B && c0 != 0x200C && c0 != 0x200D && c0 != 0xFEFF) ? 1 : 0;
            int keep1 = (c1 >= 32 && (c1 < 0x7F || c1 > 0x9F) && c1 != 0x200B && c1 != 0x200C && c1 != 0x200D && c1 != 0xFEFF) ? 1 : 0;
            int keep2 = (c2 >= 32 && (c2 < 0x7F || c2 > 0x9F) && c2 != 0x200B && c2 != 0x200C && c2 != 0x200D && c2 != 0xFEFF) ? 1 : 0;
            int keep3 = (c3 >= 32 && (c3 < 0x7F || c3 > 0x9F) && c3 != 0x200B && c3 != 0x200C && c3 != 0x200D && c3 != 0xFEFF) ? 1 : 0;
            int keep4 = (c4 >= 32 && (c4 < 0x7F || c4 > 0x9F) && c4 != 0x200B && c4 != 0x200C && c4 != 0x200D && c4 != 0xFEFF) ? 1 : 0;
            int keep5 = (c5 >= 32 && (c5 < 0x7F || c5 > 0x9F) && c5 != 0x200B && c5 != 0x200C && c5 != 0x200D && c5 != 0xFEFF) ? 1 : 0;
            int keep6 = (c6 >= 32 && (c6 < 0x7F || c6 > 0x9F) && c6 != 0x200B && c6 != 0x200C && c6 != 0x200D && c6 != 0xFEFF) ? 1 : 0;
            int keep7 = (c7 >= 32 && (c7 < 0x7F || c7 > 0x9F) && c7 != 0x200B && c7 != 0x200C && c7 != 0x200D && c7 != 0xFEFF) ? 1 : 0;

            writePtr[0] = c0; writePtr += keep0;
            writePtr[0] = c1; writePtr += keep1;
            writePtr[0] = c2; writePtr += keep2;
            writePtr[0] = c3; writePtr += keep3;
            writePtr[0] = c4; writePtr += keep4;
            writePtr[0] = c5; writePtr += keep5;
            writePtr[0] = c6; writePtr += keep6;
            writePtr[0] = c7; writePtr += keep7;

            current += 8;
        }

        while (current < endPtr)
        {
            ushort c = *current;
            int keep = (c >= 32 && (c < 0x7F || c > 0x9F) && c != 0x200B && c != 0x200C && c != 0x200D && c != 0xFEFF) ? 1 : 0;
            *writePtr = c;
            writePtr += keep;
            current++;
        }

        pOutputLength = (int)(writePtr - pOutput);
    }
    public static void GizmoDrawCircle(Vector2 center, float radius, Color color)
    {
        Color oldGizmoColor = Gizmos.color;
        Gizmos.color = color;

        const int segments = 32;
        float angleStep = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
            Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
            Gizmos.DrawLine(p1, p2);
        }
        Gizmos.color = oldGizmoColor;
    }

    public static void DebugDrawCircle(Vector2 center, float radius, Color color, float duration)
    {
        const int segments = 32;
        float angleStep = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
            Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
            Debug.DrawLine(p1, p2, color, duration);
        }
    }

    public static Texture2D LoadTexture(string FilePath)
    {
        Texture2D Tex2D = new Texture2D(2, 2);
        byte[] FileData;

        if (File.Exists(FilePath))
        {
            FileData = File.ReadAllBytes(FilePath);
            if (Tex2D.LoadImage(FileData)) return Tex2D;
        }
        return Tex2D;
    }

    public static unsafe string Format(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        int inputLen = input.Length;

        char* output = stackalloc char[inputLen * 32];
        char* closeTags = stackalloc char[inputLen * 8];

        int outPos = 0;
        int closePos = 0;

        fixed (char* inputPtr = input) FormatPtr(inputLen, (ushort*)output, (ushort*)closeTags, (ushort*)inputPtr, ref outPos, ref closePos);

        return new string(output, 0, outPos);
    }

    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static unsafe void FormatPtr(int inputLen, ushort* output, ushort* closeTags, ushort* inputPtr, ref int outPos, ref int closePos)
    {
        for (int i = 0; i < inputLen; i++)
        {
            ushort c = inputPtr[i];

            if ((c == '§' || c == '&') && i + 1 < inputLen)
            {
                ushort code = inputPtr[++i];

                if (code == 'r')
                {
                    for (int j = closePos - 1; j >= 0; j--) output[outPos++] = closeTags[j]; closePos = 0;
                }
                else
                {
                    bool isColor = false;

                    switch (code)
                    {
                        case '0':
                            WriteColor(output, ref outPos, '#', '0', '0', '0', '0', '0', '0');
                            isColor = true;
                            break;
                        case '1':
                            WriteColor(output, ref outPos, '#', '0', '0', '0', '0', 'A', 'A');
                            isColor = true;
                            break;
                        case '2':
                            WriteColor(output, ref outPos, '#', '0', '0', 'A', 'A', '0', '0');
                            isColor = true;
                            break;
                        case '3':
                            WriteColor(output, ref outPos, '#', '0', '0', 'A', 'A', 'A', 'A');
                            isColor = true;
                            break;
                        case '4':
                            WriteColor(output, ref outPos, '#', 'A', 'A', '0', '0', '0', '0');
                            isColor = true;
                            break;
                        case '5':
                            WriteColor(output, ref outPos, '#', 'A', 'A', '0', '0', 'A', 'A');
                            isColor = true;
                            break;
                        case '6':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'A', 'A', '0', '0');
                            isColor = true;
                            break;
                        case '7':
                            WriteColor(output, ref outPos, '#', 'A', 'A', 'A', 'A', 'A', 'A');
                            isColor = true;
                            break;
                        case '8':
                            WriteColor(output, ref outPos, '#', '5', '5', '5', '5', '5', '5');
                            isColor = true;
                            break;
                        case '9':
                            WriteColor(output, ref outPos, '#', '5', '5', '5', '5', 'F', 'F');
                            isColor = true;
                            break;
                        case 'a':
                            WriteColor(output, ref outPos, '#', '5', '5', 'F', 'F', '5', '5');
                            isColor = true;
                            break;
                        case 'b':
                            WriteColor(output, ref outPos, '#', '5', '5', 'F', 'F', 'F', 'F');
                            isColor = true;
                            break;
                        case 'c':
                            WriteColor(output, ref outPos, '#', 'F', 'F', '5', '5', '5', '5');
                            isColor = true;
                            break;
                        case 'd':
                            WriteColor(output, ref outPos, '#', 'F', 'F', '5', '5', 'F', 'F');
                            isColor = true;
                            break;
                        case 'e':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'F', 'F', '5', '5');
                            isColor = true;
                            break;
                        case 'f':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'F', 'F', 'F', 'F');
                            isColor = true;
                            break;
                        case 'g':
                            WriteColor(output, ref outPos, '#', 'D', 'D', 'D', '6', '0', '5');
                            isColor = true;
                            break;
                        case 'h':
                            WriteColor(output, ref outPos, '#', 'E', '3', 'D', '4', 'D', '1');
                            isColor = true;
                            break;
                        case 'i':
                            WriteColor(output, ref outPos, '#', 'C', 'E', 'C', 'A', 'C', 'A');
                            isColor = true;
                            break;
                        case 'j':
                            WriteColor(output, ref outPos, '#', '4', '4', '3', 'A', '3', 'B');
                            isColor = true;
                            break;
                        case 'm':
                            WriteColor(output, ref outPos, '#', '9', '7', '1', '6', '0', '7');
                            isColor = true;
                            break;
                        case 'n':
                            WriteColor(output, ref outPos, '#', 'B', '4', '6', '8', '4', 'D');
                            isColor = true;
                            break;
                        case 'p':
                            WriteColor(output, ref outPos, '#', 'D', 'E', 'B', '1', '2', 'D');
                            isColor = true;
                            break;
                        case 'q':
                            WriteColor(output, ref outPos, '#', '4', '7', 'A', '0', '3', '6');
                            isColor = true;
                            break;
                        case 's':
                            WriteColor(output, ref outPos, '#', '2', 'C', 'B', 'A', 'A', '8');
                            isColor = true;
                            break;
                        case 't':
                            WriteColor(output, ref outPos, '#', '2', '1', '4', '9', '7', 'B');
                            isColor = true;
                            break;
                        case 'u':
                            WriteColor(output, ref outPos, '#', '9', 'A', '5', 'C', 'C', '6');
                            isColor = true;
                            break;
                        case 'v':
                            WriteColor(output, ref outPos, '#', 'E', 'B', '7', '1', '1', '4');
                            isColor = true;
                            break;
                    }

                    if (isColor)
                    {
                        closeTags[closePos++] = '>';
                        closeTags[closePos++] = 'r';
                        closeTags[closePos++] = 'o';
                        closeTags[closePos++] = 'l';
                        closeTags[closePos++] = 'o';
                        closeTags[closePos++] = 'c';
                        closeTags[closePos++] = '/';
                        closeTags[closePos++] = '<';
                    }
                    else
                    {
                        switch (code)
                        {
                            case 'l':
                                output[outPos++] = '<';
                                output[outPos++] = 'b';
                                output[outPos++] = '>';
                                closeTags[closePos++] = '>';
                                closeTags[closePos++] = 'b';
                                closeTags[closePos++] = '/';
                                closeTags[closePos++] = '<';
                                break;

                            case 'o':
                                output[outPos++] = '<';
                                output[outPos++] = 'i';
                                output[outPos++] = '>';
                                closeTags[closePos++] = '>';
                                closeTags[closePos++] = 'i';
                                closeTags[closePos++] = '/';
                                closeTags[closePos++] = '<';
                                break;
                            default: output[outPos++] = c; break;
                        }
                    }
                }
            }
            else output[outPos++] = c;
        }

        for (int j = closePos - 1; j >= 0; j--) output[outPos++] = closeTags[j];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteColor(ushort* buffer, ref int pos, ushort c0, ushort c1, ushort c2, ushort c3, ushort c4, ushort c5, ushort c6)
        {
            v256* asV256 = (v256*)(buffer + pos);
            asV256->ULong0 = 0x006C006F0063003CUL;
            asV256->ULong1 = ((ulong)c0 << 48) | 0x00003D0072006FUL;
            asV256->ULong2 = ((ulong)c4 << 48) | ((ulong)c3 << 32) | ((ulong)c2 << 16) | c1;
            asV256->ULong3 = ((uint)c6 << 16) | c5;
            buffer[pos + 14] = 62;
            pos += 15;
        }
    }

    public static Color CodeToColor(char code)
    {
        return code switch
        {
            '0' => C(0x00, 0x00, 0x00),
            '1' => C(0x00, 0x00, 0xAA),
            '2' => C(0x00, 0xAA, 0x00),
            '3' => C(0x00, 0xAA, 0xAA),
            '4' => C(0xAA, 0x00, 0x00),
            '5' => C(0xAA, 0x00, 0xAA),
            '6' => C(0xFF, 0xAA, 0x00),
            '7' => C(0xAA, 0xAA, 0xAA),
            '8' => C(0x55, 0x55, 0x55),
            '9' => C(0x55, 0x55, 0xFF),
            'a' => C(0x55, 0xFF, 0x55),
            'b' => C(0x55, 0xFF, 0xFF),
            'c' => C(0xFF, 0x55, 0x55),
            'd' => C(0xFF, 0x55, 0xFF),
            'e' => C(0xFF, 0xFF, 0x55),
            'f' => C(0xFF, 0xFF, 0xFF),
            'g' => C(0xDD, 0xD6, 0x05),
            'h' => C(0xE3, 0xD4, 0xD1),
            'i' => C(0xCE, 0xCA, 0xCA),
            'j' => C(0x44, 0x3A, 0x3B),
            'm' => C(0x97, 0x16, 0x07),
            'n' => C(0xB4, 0x68, 0x4D),
            'p' => C(0xDE, 0xB1, 0x2D),
            'q' => C(0x47, 0xA0, 0x36),
            's' => C(0x2C, 0xBA, 0xA8),
            't' => C(0x21, 0x49, 0x7B),
            'u' => C(0x9A, 0x5C, 0xC6),
            'v' => C(0xEB, 0x71, 0x14),
            _ => Color.white
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FlagIsSet<T>(T flags, T flag)
        where T : struct, Enum
    {
        return EnumFlagOps<T>.IsSet(flags, flag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlagSet<T>(ref T flags, T flag)
        where T : struct, Enum
    {
        flags = EnumFlagOps<T>.Set(flags, flag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlagUnset<T>(ref T flags, T flag)
        where T : struct, Enum
    {
        flags = EnumFlagOps<T>.Unset(flags, flag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlagFlip<T>(ref T flags, T flag)
        where T : struct, Enum
    {
        flags = EnumFlagOps<T>.Flip(flags, flag);
    }

    private static class EnumFlagOps<T>
        where T : struct, Enum
    {
        private static readonly int size = Unsafe.SizeOf<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSet(T flags, T flag)
        {
            if (size == 1)
            {
                byte a = Unsafe.As<T, byte>(ref flags);
                byte b = Unsafe.As<T, byte>(ref flag);
                return (a & b) != 0;
            }

            if (size == 2)
            {
                ushort a = Unsafe.As<T, ushort>(ref flags);
                ushort b = Unsafe.As<T, ushort>(ref flag);
                return (a & b) != 0;
            }

            if (size == 4)
            {
                uint a = Unsafe.As<T, uint>(ref flags);
                uint b = Unsafe.As<T, uint>(ref flag);
                return (a & b) != 0;
            }

            {
                ulong a = Unsafe.As<T, ulong>(ref flags);
                ulong b = Unsafe.As<T, ulong>(ref flag);
                return (a & b) != 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Set(T flags, T flag)
        {
            if (size == 1)
            {
                byte result = (byte)(Unsafe.As<T, byte>(ref flags) | Unsafe.As<T, byte>(ref flag));
                return Unsafe.As<byte, T>(ref result);
            }

            if (size == 2)
            {
                ushort result = (ushort)(Unsafe.As<T, ushort>(ref flags) | Unsafe.As<T, ushort>(ref flag));
                return Unsafe.As<ushort, T>(ref result);
            }

            if (size == 4)
            {
                uint result = Unsafe.As<T, uint>(ref flags) | Unsafe.As<T, uint>(ref flag);
                return Unsafe.As<uint, T>(ref result);
            }

            {
                ulong result = Unsafe.As<T, ulong>(ref flags) | Unsafe.As<T, ulong>(ref flag);
                return Unsafe.As<ulong, T>(ref result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Unset(T flags, T flag)
        {
            if (size == 1)
            {
                byte result = (byte)(Unsafe.As<T, byte>(ref flags) & ~Unsafe.As<T, byte>(ref flag));
                return Unsafe.As<byte, T>(ref result);
            }

            if (size == 2)
            {
                ushort result = (ushort)(Unsafe.As<T, ushort>(ref flags) & ~Unsafe.As<T, ushort>(ref flag));
                return Unsafe.As<ushort, T>(ref result);
            }

            if (size == 4)
            {
                uint result = Unsafe.As<T, uint>(ref flags) & ~Unsafe.As<T, uint>(ref flag);
                return Unsafe.As<uint, T>(ref result);
            }

            {
                ulong result = Unsafe.As<T, ulong>(ref flags) & ~Unsafe.As<T, ulong>(ref flag);
                return Unsafe.As<ulong, T>(ref result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Flip(T flags, T flag)
        {
            if (size == 1)
            {
                byte result = (byte)(Unsafe.As<T, byte>(ref flags) ^ Unsafe.As<T, byte>(ref flag));
                return Unsafe.As<byte, T>(ref result);
            }

            if (size == 2)
            {
                ushort result = (ushort)(Unsafe.As<T, ushort>(ref flags) ^ Unsafe.As<T, ushort>(ref flag));
                return Unsafe.As<ushort, T>(ref result);
            }

            if (size == 4)
            {
                uint result = Unsafe.As<T, uint>(ref flags) ^ Unsafe.As<T, uint>(ref flag);
                return Unsafe.As<uint, T>(ref result);
            }

            {
                ulong result = Unsafe.As<T, ulong>(ref flags) ^ Unsafe.As<T, ulong>(ref flag);
                return Unsafe.As<ulong, T>(ref result);
            }
        }
    }

}
