using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using static BinaryVectors;
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
    public static Vector2 AngleToNormalizedCoordinate(float angle)
    {
        float radians = math.radians(angle);
        return new Vector2(math.cos(radians), math.sin(radians)).normalized;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ConvertVector2ToAngle(Vector2 direction)
    {
        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return angle;
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
    public static byte[] CompressRigidbody(Rigidbody2D rb)
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
        // --- Decode authoritative state ---
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
        lateness = Mathf.Clamp(lateness, 0f, 0.25f); // safety clamp

        // --- Predict ---
        Vector2 predictedPos = serverPos + serverVel * lateness;
        float predictedRot = serverRot + serverAngVel * lateness;

        // --- Error-based smoothing ---
        float error = Vector2.Distance(rb.position, predictedPos);

        float positionSharpness = Mathf.Lerp(
            0.5f,
            0.2f,
            Mathf.InverseLerp(1f, 200f, usedFrequency)
        );

        if (error > 1f)
            positionSharpness = 1f;

        // --- Apply ---
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

    public static string RemoveInvisibleChars(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var span = input.AsSpan().Trim();
        if (span.Length == 0) return string.Empty;

        bool hasInvisibleChars = false;
        foreach (char c in span)
        {
            if (char.IsControl(c) || c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\uFEFF')
            {
                hasInvisibleChars = true;
                break;
            }
        }

        if (!hasInvisibleChars) return span.ToString().Normalize(NormalizationForm.FormC);

        Span<char> buffer = span.Length <= 256 ? stackalloc char[span.Length] : new char[span.Length];
        int writeIndex = 0;
        foreach (char c in span)
        {
            if (!char.IsControl(c) && c != '\u200B' && c != '\u200C' && c != '\u200D' && c != '\uFEFF') buffer[writeIndex++] = c;
        }

        return new string(buffer.Slice(0, writeIndex)).Normalize(NormalizationForm.FormC);
    }

}
