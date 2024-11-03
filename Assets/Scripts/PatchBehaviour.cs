using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class PatchBehaviour : MonoBehaviour
{
    SpriteShapeController spriteShapeController;
    PolygonCollider2D polygonCollider;

    void Start()
    {
        spriteShapeController = GetComponent<SpriteShapeController>();
        polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.enabled = true;

        List<Vector2> shapePoints = GetSpriteShapePoints();

        polygonCollider.SetPath(0, shapePoints.ToArray());
        SpriteShapeRenderer spriteShapeRenderer = spriteShapeController.spriteShapeRenderer;
        Destroy(spriteShapeController);
        Destroy(spriteShapeRenderer);
        Destroy(this);

    }

    List<Vector2> GetSpriteShapePoints()
    {
        List<Vector2> points = new List<Vector2>();

        Spline spline = spriteShapeController.spline;

        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            Vector3 splinePoint = spline.GetPosition(i);

            points.Add(new Vector2(splinePoint.x, splinePoint.y));
        }

        return points;
    }
}
