using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ProjectileDebugBehaviour : MonoBehaviour
{

    const float AoeCircleDuration = 0.4f;
    readonly Color AoeCircleColor = new Color(0.825f, 0f, 0f);
    readonly Color AoeFanColor = new Color(0.725f, 0f, 0f);

    ProjectileBehaviour projectile;

    float aoeRange;
    Vector2 projectilePos;

    void FetchProjectileData()
    {
        if (!projectile) return;
        aoeRange = projectile.data.aoe;
        projectilePos = projectile.transform.position;
    }

    void Awake()
    {
        projectile = GetComponent<ProjectileBehaviour>();
    }

    void Update()
    {
        FetchProjectileData();
    }

    private void OnDestroy()
    {
        DrawAoeDamageCircle();
    }

    void DrawAoeDamageCircle()
    {
        const int CircleRes = 32;
        const float CircleStep = 360f / CircleRes;

        Span<Vector2> sphericalP = stackalloc Vector2[CircleRes];

        for(int i = 0; i < CircleRes; i++)
        {
            Vector2 dir = (MyExtentions.DegreesToVector2((float)i * CircleStep)).normalized;
            RaycastHit2D hitInfo = Physics2D.Raycast(projectilePos, dir, aoeRange, PhysicsMasks.ENVIRONTMENT_MASK);
            sphericalP[i] = projectilePos + (dir * (hitInfo.transform ? Vector2.Distance(hitInfo.point, projectilePos) : aoeRange));
        }

        for (int i = 0; i < sphericalP.Length - 1; i++) Debug.DrawLine(sphericalP[i], sphericalP[i + 1], AoeCircleColor, AoeCircleDuration);
        Debug.DrawLine(sphericalP[sphericalP.Length - 1], sphericalP[0], AoeCircleColor, AoeCircleDuration);
        for (int i = 0; i < sphericalP.Length; i++) Debug.DrawLine(sphericalP[i], projectilePos, AoeFanColor, AoeCircleDuration);
    }

}
