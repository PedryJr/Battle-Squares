using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
public struct SafeMathDep
{
    //Math helper functions

    // Epsilon threshold to prevent division by zero
    public const float EPSILON = 1e-5f;

    /// <summary>
    /// Safe normalization of float2. Returns zero vector if length is too small.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public float2 SafeNormalize(in float2 v)
    {
        float lenSq = math.lengthsq(v);
        if (lenSq > EPSILON)
            return v / math.sqrt(lenSq);
        return 0f;
    }

    /// <summary>
    /// Safe normalization of float3. Returns zero vector if length is too small.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public float3 SafeNormalize(in float3 v)
    {
        float lenSq = math.lengthsq(v);
        if (lenSq > EPSILON)
            return v / math.sqrt(lenSq);
        return 0f;
    }

    /// <summary>
    /// Safe slerp for quaternions. Avoids NaNs if blending values are out of bounds or input is invalid.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public quaternion SafeSlerp(in quaternion a, in quaternion b, float t)
    {
        t = math.clamp(t, 0f, 1f);

        // Handle invalid input quaternions
        if (!IsValidQuaternion(a)) return b;
        if (!IsValidQuaternion(b)) return a;

        return math.slerp(a, b, t);
    }

    /// <summary>
    /// Checks if quaternion contains finite numbers only.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public bool IsValidQuaternion(in quaternion q)
    {
        return math.all(math.isfinite(q.value));
    }

    /// <summary>
    /// Checks if float2 contains finite values.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public bool IsValidFloat2(in float2 v)
    {
        return math.all(math.isfinite(v));
    }

    /// <summary>
    /// Checks if float is a valid number.
    /// </summary>
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    [MethodImpl(512)]
    public bool IsValidFloat(float v)
    {
        return math.isfinite(v);
    }
}
