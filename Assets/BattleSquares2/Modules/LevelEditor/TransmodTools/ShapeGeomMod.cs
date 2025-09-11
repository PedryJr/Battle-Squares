using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "ShapeGeom", menuName = "ScriptableObjects/ShapeGeomMod", order = 1)]
public abstract class ShapeGeomMod : ScriptableObject
{

    public abstract JobHandle MakeModifierJob(JobHandle generator, SpriteShapeController spriteShapeController, NativeArray<ushort> indices,
        NativeSlice<Vector3> positions, NativeSlice<Vector2> texCoords, NativeSlice<Vector4> tangents,
        NativeArray<SpriteShapeSegment> segments, NativeArray<float2> colliderData);

}