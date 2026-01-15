using System;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{



    public readonly struct HitInfo
    {
        public HitInfo(bool HasHit, Vector2 Point, Vector2 Normal, float Distance)
        {
            this.HasHit = HasHit;
            this.Point = Point;
            this.Normal = Normal;
            this.Distance = Distance;
        }
        public readonly bool HasHit;
        public readonly Vector2 Point;
        public readonly Vector2 Normal;
        public readonly float Distance;
    }

    public static class PhysBridge
    {
        internal static Func<Vector2, Vector2, float, int, HitInfo> RaycastInternal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HitInfo Raycast(
            Vector2 origin,
            Vector2 direction,
            float distance,
            int layerMask)
        {
            return RaycastInternal(origin, direction, distance, layerMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HitInfo Raycast(
            Vector2 origin,
            Vector2 direction)
        {
            return Raycast(origin, direction, float.PositiveInfinity, ~0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HitInfo Raycast(
            Vector2 origin,
            Vector2 direction,
            float distance)
        {
            return Raycast(origin, direction, distance, ~0);
        }
    }
}