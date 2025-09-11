using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public class SplineDrag : MonoBehaviour
{

    [SerializeField]
    SplineMod splineMod;
    AnimationAnchor animationAnchor;
    Vector2 targetPos;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoDrag(Vector2 toPos) => targetPos = toPos;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetConnectedAnchor(AnimationAnchor anchor) => animationAnchor = anchor;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveMe() => animationAnchor.RemoveSplineMod(splineMod);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AnimationAnchor GetAnchor() => animationAnchor;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetTarget() => targetPos;


    private void Update() => UpdateAnchorPosition();


    void UpdateAnchorPosition()
    {

        if (transform.childCount > 0)
        {

            Span<float3> childPositions = stackalloc float3[transform.childCount];
            for (int i = 0; i < transform.childCount; i++) childPositions[i] = transform.GetChild(i).position;
            transform.position = Vector3.Lerp(transform.position, new Vector3(targetPos.x, targetPos.y, transform.position.z), Time.deltaTime * AnimationAnchor.animationSpeed);
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).position = new Vector3(childPositions[i].x, childPositions[i].y, transform.GetChild(i).position.z);

        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(targetPos.x, targetPos.y, transform.position.z), Time.deltaTime * AnimationAnchor.animationSpeed);
        }
    }

    public void HardUpdateAnchorPosition(Vector2 pos)
    {

        if (transform.childCount > 0)
        {

            Span<float3> childPositions = stackalloc float3[transform.childCount];
            for (int i = 0; i < transform.childCount; i++) childPositions[i] = transform.GetChild(i).position;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).position = new Vector3(childPositions[i].x, childPositions[i].y, transform.GetChild(i).position.z);

        }
        else
        {
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }
    }

}
