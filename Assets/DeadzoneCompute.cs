
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class DeadzoneCompute : MonoBehaviour
{

    public static DeadzoneCompute Instance;

    [SerializeField][Range(0f, 1f)] float deadzoneRadius = 0.5f;
    [SerializeField][Range(0f, 1f)] float coneBias = 0.5f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        radialInputCreator.x = Mathf.Cos(Time.time);
        radialInputCreator.y = Mathf.Sin(Time.time);
    }

    Vector2 radialInputCreator = new Vector2();

    float NormalizeAngle(float a)
    {
        if (a < 0f) a += Mathf.PI * 2f;
        return a;
    }
    float AngleDelta(float a, float b)
    {
        float d = Mathf.Abs(a - b);
        return d > Mathf.PI ? (Mathf.PI * 2f - d) : d;
    }
    Vector2 DirectionFromIndex(int i)
    {
        return i switch
        {
            0 => Vector2.right,
            1 => new Vector2(1, 1).normalized,
            2 => Vector2.up,
            3 => new Vector2(-1, 1).normalized,
            4 => Vector2.left,
            5 => new Vector2(-1, -1).normalized,
            6 => Vector2.down,
            7 => new Vector2(1, -1).normalized,
            _ => Vector2.zero
        };
    }




    float GetConeHalfWidth(bool straight)
    {
        float baseHalf = (Mathf.PI / 4f) * 0.5f;

        float bias = Mathf.Lerp(-1f, 1f, coneBias);

        float scale = straight
            ? Mathf.Lerp(0.6f, 1.4f, coneBias)
            : Mathf.Lerp(1.4f, 0.6f, coneBias);

        return baseHalf * scale;
    }


    public Vector2 ProcessDeadzone(Vector2 input)
    {
        float magnitude = input.magnitude;

        if (magnitude <= deadzoneRadius)
            return Vector2.zero;

        Vector2 dir = input / magnitude;
        float angle = NormalizeAngle(Mathf.Atan2(dir.y, dir.x));

        int chosenIndex = -1;
        float smallestDelta = float.MaxValue;

        for (int i = 0; i < 8; i++)
        {
            bool isStraight = (i % 2 == 0);

            float centerAngle = i * (Mathf.PI / 4f);
            float halfWidth = GetConeHalfWidth(isStraight);

            float delta = AngleDelta(angle, centerAngle);

            if (delta <= halfWidth && delta < smallestDelta)
            {
                smallestDelta = delta;
                chosenIndex = i;
            }
        }

        if (chosenIndex == -1) return Vector2.zero;

        float scaledMagnitude = Mathf.InverseLerp(deadzoneRadius, 1f, magnitude);
        return DirectionFromIndex(chosenIndex) * scaledMagnitude;
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

    float GetConeAngleSize(bool straight)
    {
        // 0.5 = equal
        // Straights get bigger as bias → 1
        // Diagonals get smaller as bias → 1
        float baseAngle = Mathf.PI / 4f; // 45°

        float bias = Mathf.Lerp(-0.4f, 0.4f, coneBias);

        return straight
            ? baseAngle * (1f + bias)
            : baseAngle * (1f - bias);
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

            Gizmos.color = isStraight
                ? new Color(1f, 0.5f, 0.1f, 0.6f)   // straight
                : new Color(0.2f, 1f, 1f, 0.6f);    // diagonal

            DrawCone(origin, start, end, radius);
        }
    }



    Vector2 AngleToDir(float angleRad)
    {
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
        Gizmos.DrawLine(origin, origin + ProcessDeadzone(radialInputCreator));
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
