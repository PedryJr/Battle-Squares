using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public sealed class SplineMod : MonoBehaviour
{
    const MethodImplOptions inline = MethodImplOptions.AggressiveInlining;


    [SerializeField]
    SplineDrag Center;
    [SerializeField]
    SplineDrag Left;
    [SerializeField]
    SplineDrag Right;

    [MethodImpl(inline)] public SplineDrag GetEndMod() => Center;
    [MethodImpl(inline)] public SplineDrag GetLeftMod() => Left;
    [MethodImpl(inline)] public SplineDrag GetRightMod() => Right;

    [MethodImpl(inline)] public Transform GetEnd() => Center.transform;
    [MethodImpl(inline)] public Transform GetLeft() => Left.transform;
    [MethodImpl(inline)] public Transform GetRight() => Right.transform;

    [MethodImpl(inline)] public void AssignAnchor(AnimationAnchor anchor)
    {
        Center.SetConnectedAnchor(anchor);
        Left.SetConnectedAnchor(anchor);
        Right.SetConnectedAnchor(anchor);
    }

    public void DragAll(Vector2 toPos, bool additive)
    {

        Vector2 c, l, r;

        c = additive ? Center.GetTarget() + toPos : toPos;
        l = additive ? Left.GetTarget() + toPos : toPos;
        r = additive ? Right.GetTarget() + toPos : toPos;

        Center.DoDrag(c);
        Left.DoDrag(l);
        Right.DoDrag(r);
    }

    public void DragAll(float2x3 toPos, bool additive)
    {

        Vector2 c, l, r;

        c = additive ? (float2)Center.GetTarget() + toPos.c0 : toPos.c0;
        l = additive ? (float2)Left.GetTarget() + toPos.c1 : toPos.c1;
        r = additive ? (float2)Right.GetTarget() + toPos.c2 : toPos.c2;

        Center.DoDrag(c);
        Left.DoDrag(l);
        Right.DoDrag(r);
    }

}
