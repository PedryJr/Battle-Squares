using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;
using static Unity.Collections.AllocatorManager;

public class LevelLoader : MonoBehaviour
{

    Level? currentlyLoadedLevel;

    Dictionary<int, LevelPiece> levelPieces;

    private void Awake()
    {
        Level emptyLevel = new Level();
        ReloadLevel(emptyLevel);
    }

    public void ReloadLevel(Level levelToLoad)
    {
        if(currentlyLoadedLevel.HasValue)
        {
            //ReloadDiffs
        }
    }

    public void ReloadPiece()
    {

    }

}
[Serializable]
public struct Level
{
    public Piece[] pieces;

    public void AddPiece(Piece piece)
    {
        int index = FindPieceIndex(piece.id);
        if (index >= 0)
        {
            pieces[index] = piece;
        }
        else
        {
            Array.Resize(ref pieces, pieces.Length + 1);
            pieces[^1] = piece;
        }
    }

    public void RemovePiece(int pieceId)
    {
        int index = FindPieceIndex(pieceId);
        if (index < 0) return;

        for (int i = index; i < pieces.Length - 1; i++)
        {
            pieces[i] = pieces[i + 1];
        }
        Array.Resize(ref pieces, pieces.Length - 1);
    }

    public void AddBlocker(int pieceId, Blocker blocker)
    {
        int pieceIndex = FindPieceIndex(pieceId);
        if (pieceIndex < 0) throw new ArgumentException("Piece not found");

        Piece piece = pieces[pieceIndex];
        piece.AddBlocker(blocker);
        pieces[pieceIndex] = piece;
    }

    public void RemoveBlocker(int pieceId, int blockerId)
    {
        int pieceIndex = FindPieceIndex(pieceId);
        if (pieceIndex < 0) return;

        Piece piece = pieces[pieceIndex];
        piece.RemoveBlocker(blockerId);
        pieces[pieceIndex] = piece;
    }

    public void AddShape(int pieceId, Shape shape)
    {
        int pieceIndex = FindPieceIndex(pieceId);
        if (pieceIndex < 0) throw new ArgumentException("Piece not found");

        Piece piece = pieces[pieceIndex];
        piece.AddShape(shape);
        pieces[pieceIndex] = piece;
    }

    public void RemoveShape(int pieceId, int shapeId)
    {
        int pieceIndex = FindPieceIndex(pieceId);
        if (pieceIndex < 0) return;

        Piece piece = pieces[pieceIndex];
        piece.RemoveShape(shapeId);
        pieces[pieceIndex] = piece;
    }

    public Piece? GetPiece(int pieceId)
    {
        int index = FindPieceIndex(pieceId);
        return index >= 0 ? pieces[index] : null;
    }

    public Blocker? GetBlocker(int blockerId)
    {
        foreach (Piece piece in pieces)
        {
            Blocker? blocker = piece.GetBlocker(blockerId);
            if (blocker != null) return blocker;
        }
        return null;
    }

    public Piece? GetPieceForBlocker(int blockerId)
    {
        foreach (Piece piece in pieces)
        {
            if (piece.ContainsBlocker(blockerId)) return piece;
        }
        return null;
    }

    private int FindPieceIndex(int pieceId)
    {
        for (int i = 0; i < pieces.Length; i++)
            if (pieces[i].id == pieceId)
                return i;
        return -1;
    }
}
[Serializable]
public struct Piece
{
    public int id;
    public Blocker[] blockers;
    public Shape[] shapes;

    public Piece(int id)
    {
        this.id = id;
        blockers = Array.Empty<Blocker>();
        shapes = Array.Empty<Shape>();
    }

    public void AddBlocker(Blocker blocker)
    {
        blocker.pieceId = id;  // Maintain parent reference
        int index = FindBlockerIndex(blocker.id);
        if (index >= 0)
        {
            blockers[index] = blocker;
        }
        else
        {
            Array.Resize(ref blockers, blockers.Length + 1);
            blockers[^1] = blocker;
        }
    }

    public void RemoveBlocker(int blockerId)
    {
        int index = FindBlockerIndex(blockerId);
        if (index < 0) return;

        for (int i = index; i < blockers.Length - 1; i++)
        {
            blockers[i] = blockers[i + 1];
        }
        Array.Resize(ref blockers, blockers.Length - 1);
    }

    public Blocker? GetBlocker(int blockerId)
    {
        int index = FindBlockerIndex(blockerId);
        return index >= 0 ? blockers[index] : null;
    }

    public bool ContainsBlocker(int blockerId) =>
        FindBlockerIndex(blockerId) >= 0;

    private int FindBlockerIndex(int blockerId)
    {
        for (int i = 0; i < blockers.Length; i++)
            if (blockers[i].id == blockerId)
                return i;
        return -1;
    }

    // Similar methods for shapes (AddShape, RemoveShape, GetShape, ContainsShape)
    // Omitted for brevity - follow same pattern as blockers

    public void AddShape(Shape shape)
    {
        shape.pieceId = id;  // Maintain parent reference
        int index = FindShapeIndex(shape.id);
        if (index >= 0)
        {
            shapes[index] = shape;
        }
        else
        {
            Array.Resize(ref shapes, shapes.Length + 1);
            shapes[^1] = shape;
        }
    }

    public void RemoveShape(int shapeId)
    {
        int index = FindShapeIndex(shapeId);
        if (index < 0) return;

        for (int i = index; i < shapes.Length - 1; i++)
        {
            shapes[i] = shapes[i + 1];
        }
        Array.Resize(ref shapes, shapes.Length - 1);
    }

    public Shape? GetShape(int shapeId)
    {
        int index = FindShapeIndex(shapeId);
        return index >= 0 ? shapes[index] : null;
    }

    public bool ContainsShape(int shapeId) =>
        FindShapeIndex(shapeId) >= 0;

    private int FindShapeIndex(int shapeId)
    {
        for (int i = 0; i < shapes.Length; i++)
            if (shapes[i].id == shapeId)
                return i;
        return -1;
    }

}

[Serializable]
public struct Blocker
{
    public int id;
    public int pieceId;  // Parent reference
    public Vector2 pointA;
    public Vector2 pointB;

    public void SetPoints(Vector2 newPointA, Vector2 newPointB)
    {
        pointA = newPointA;
        pointB = newPointB;
    }
}

[Serializable]
public struct Shape
{
    public int id;
    public int pieceId;  // Parent reference
    public Vector2[] vertices;
    public int[] indices;

    public void SetPolygon(Vector2[] newVertices, int[] newIndices)
    {
        vertices = newVertices;
        indices = newIndices;
    }

    public ushort[] IndicesAsUShort()
    {
        ushort[] result = new ushort[indices.Length];
        for (int i = 0; i < result.Length; i++) result[i] = (ushort)indices[i];
        return result;
    }

    public void ApplyToShapeController(SpriteShapeController original, SpriteShapeController newController)
    {

        NativeArray<float2> vertsAsFloat2 = new NativeArray<float2>(vertices.Length, Allocator.Persistent);
        Vector3[] convertedVertices = new Vector3[vertices.Length];
        ushort[] convertedIndices = new ushort[indices.Length];

        for (int i = 0; i < convertedVertices.Length; i++) convertedVertices[i] = vertices[i];
        for (int i = 0; i < convertedVertices.Length; i++) convertedIndices[i] = (ushort) indices[i];
        for (int i = 0; i < convertedVertices.Length; i++) vertsAsFloat2[i] = vertices[i];

        Mesh tempMesh = new Mesh();

        SpriteShapeSegment segment = new SpriteShapeSegment();
        segment.vertexCount = vertices.Length;
        segment.indexCount = indices.Length;
        segment.spriteIndex = 0;
        segment.geomIndex = 0;
        NativeArray<SpriteShapeSegment> spriteShapeSegments = new NativeArray<SpriteShapeSegment>(new SpriteShapeSegment[] { segment }, Allocator.Persistent);

        tempMesh.vertices = convertedVertices;
        tempMesh.triangles = indices;

        tempMesh.RecalculateNormals();
        tempMesh.RecalculateTangents();
        tempMesh.RecalculateUVDistributionMetrics();

        NativeArray<ushort> usedIndices = new NativeArray<ushort>(convertedIndices, Allocator.Persistent);
        NativeArray<Vector3> usedVertices = new NativeArray<Vector3>(convertedVertices, Allocator.Persistent);
        NativeArray<Vector2> usedUvs = new NativeArray<Vector2>(tempMesh.uv, Allocator.Persistent);
        NativeArray<Vector4> usedTangents = new NativeArray<Vector4>(tempMesh.tangents, Allocator.Persistent);

        JobHandle jobHandle = original.spriteShapeCreator.MakeCreatorJob(
            newController,
            usedIndices,
            usedVertices,
            usedUvs,
            usedTangents, 
            spriteShapeSegments, 
            vertsAsFloat2);

        jobHandle.Complete();

        usedTangents.Dispose();
        usedUvs.Dispose();
        usedVertices.Dispose();
        usedIndices.Dispose();
        spriteShapeSegments.Dispose();
        vertsAsFloat2.Dispose();

    }

}