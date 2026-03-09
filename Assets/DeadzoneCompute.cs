/*
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class DeadzoneCompute : MonoBehaviour
{

    public static DeadzoneCompute Instance;

    void Awake()
    {
        Instance = this;
    }




    void DrawCone(Vector2 origin, float startAngle, float endAngle, float radius, int steps = 16)
    {
        Vector2 prev = origin + AngleToDir(startAngle) * radius;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector2 next = origin + AngleToDir(angle) * radius;

            Gizmos.DrawLine(origin, next);
            Gizmos.DrawLine(prev, next);

            prev = next;
        }
    }
    void DrawCircle(Vector2 center, float radius, int segments)
    {
        float step = Mathf.PI * 2f / segments;
        Vector2 prev = center + new Vector2(radius, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            Vector2 next = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    void DrawBiasedCones(Vector2 origin)
    {
        float radius = 1f; 
        for (int i = 0; i < 8; i++)
        {
            bool isStraight = (i % 2 == 0); 
            float centerAngle = i * (Mathf.PI / 4f);
            float halfWidth = GetConeHalfWidth(isStraight); 
            float start = centerAngle - halfWidth;
            float end = centerAngle + halfWidth; 
            Gizmos.color = isStraight ? new Color(1f, 0.5f, 0.1f, 0.6f) : new Color(0.2f, 1f, 1f, 0.6f); 
            DrawCone(origin, start, end, radius);
        }
    }



    Vector2 AngleToDir(float angleRad)
    {
        MyExtentions.DegreesToVector2(angleRad);
        return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    private void OnDrawGizmos()
    {
        Vector2 origin = transform.position;

        // Deadzone
        Gizmos.color = Color.blue;
        DrawCircle(origin, deadzoneRadius, 32);

        // Biased cones
        DrawBiasedCones(origin);

        // Output direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + ProcessDeadzoneAnd8Directions(radialInputCreator));
        Gizmos.DrawLine(origin, origin + radialInputCreator);

        ForceGizmoUpdate();
    }



    void ForceGizmoUpdate()
    {
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

}
*/