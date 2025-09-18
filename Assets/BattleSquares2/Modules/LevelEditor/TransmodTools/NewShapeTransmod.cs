using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public sealed class NewShapeTransmod : LevelTransMod
{

    [SerializeField]
    GameObject shapeSource;
    [SerializeField]
    GameObject engineShape;
    [ContextMenu("Add Shape")]
    void AddShapeInEngine()
    {

        ShapeTemplate[] newTemplates = new ShapeTemplate[engineTemplates.Length + 1];
        for (int i = 0; i < engineTemplates.Length; i++) newTemplates[i] = engineTemplates[i];
        ShapeTemplate newTemplate = new ShapeTemplate();

        SpriteShapeController original = engineShape.GetComponent<SpriteShapeController>();

        int vertexCount = original.spline.GetPointCount();

        Vector2[] borderPoints = new Vector2[vertexCount];
        IList<Vector2[]> borderPointsAsIList = new List<Vector2[]>();
        borderPointsAsIList.Add(new Vector2[vertexCount]);

        for (int i = 0; i < vertexCount; i++)
        {
            borderPoints[i] = original.spline.GetPosition(i);
            borderPointsAsIList[0][i] = borderPoints[i];
        }
        PolygonTriangulator.Triangulate(borderPoints, out newTemplate.shape.vertices, out newTemplate.shape.indices);

        Vector2[] ColliderPoints = new Vector2[0];
        Sprite worldGeomSprite = PolygonTriangulator.RasterizeMesh(newTemplate.shape.vertices, newTemplate.shape.indices, 2048);

        Vector3[] shadowShape = new Vector3[newTemplate.shape.vertices.Length];
        for (int i = 0; i < newTemplate.shape.vertices.Length; i++)
        {
            shadowShape[i] = newTemplate.shape.vertices[i];
        }


        GameObject test = Instantiate(shapeSource);
        ShadowCaster2D shadowCaster = test.GetComponent<ShadowCaster2D>();
        SpriteRenderer shadowSprite = test.GetComponent<SpriteRenderer>();
        PolygonCollider2D spriteCollider = test.GetComponent<PolygonCollider2D>();

        shadowSprite.sprite = worldGeomSprite;
        spriteCollider.points = borderPoints;
        spriteCollider.offset = -PolygonTriangulator.GetCenterBounds(newTemplate.shape.vertices)/2f;

        newTemplates[newTemplates.Length - 1] = newTemplate;
        engineTemplates = newTemplates;
    }


    [SerializeField]
    ShapeTemplate[] engineTemplates;

    List<ShapeTemplate> userTemplates;

    List<ShapeTemplate> shapeTemplates;

    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        LoadUserTemplates();

        LoadAllTemplates();
    }

    void LoadUserTemplates()
    {
        userTemplates = new List<ShapeTemplate>();
        //Add saved user templates...
    }

    void LoadAllTemplates()
    {
        shapeTemplates = new List<ShapeTemplate>();
        foreach (ShapeTemplate template in userTemplates) shapeTemplates.Add(template);
        foreach (ShapeTemplate template in engineTemplates) shapeTemplates.Add(template);
    }

    void InitializeTransMod()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CanUse = true;
    }

    protected override void InheritStart()
    {

        leftClickCallback += LeftClickCallback;
        leftHoldCallback += LeftHoldCallback;
        rightClickCallback += RightClickCallback;
        rightHoldCallback += RightHoldCallback;
        releaseCallback += ReleaseCallback;
    }

    protected override void InheritDestroy()
    {
        leftClickCallback -= LeftClickCallback;
        leftHoldCallback -= LeftHoldCallback;
        rightClickCallback -= RightClickCallback;
        rightHoldCallback -= RightHoldCallback;
        releaseCallback -= ReleaseCallback;
    }

    void LeftClickCallback(Vector2 position)
    {
        //Left click
    }
    void LeftHoldCallback(Vector2 position)
    {
        //Left hold
    }
    void RightClickCallback(Vector2 position)
    {
        //Right Click
    }
    void RightHoldCallback(Vector2 position)
    {
        //Right Hold
    }

    void ReleaseCallback(bool releaseState)
    {
        spriteRenderer.color = Color.white;
    }

    [Serializable]
    struct ShapeTemplate
    {
        public Shape shape;
        public string name;
    }

}
