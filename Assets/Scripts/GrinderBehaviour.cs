using Unity.Physics;
using UnityEngine;
using UnityEngine.U2D;

public class GrinderBehaviour : MonoBehaviour
{

    [SerializeField]
    Transform[] points;

    [SerializeField]
    float lapsPerSecond;

    [SerializeField]
    float rotationsPerSecond;

    [SerializeField]
    AnimationCurve perPositionLerp;

    SpriteShapeRenderer spriteShapeRenderer;
    MapSynchronizer mapSynchronizer;
    Rigidbody2D rb;
    Collider2D grindCollider;

    float pointstimer = 0;
    float rotation = 0;
    float oldTimer = 0;
    float lerp;
    int index = 0;

    public Vector2 fromPos;
    public Vector2 toPos;
    Vector2 previousPosition;

    private void Awake()
    {

        spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        grindCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        previousPosition = rb.position;
        fromPos = rb.position;
        toPos = points[index].position;

    }

    private void FixedUpdate()
    {

        pointstimer = Mathf.Repeat(mapSynchronizer.baseTime * lapsPerSecond * points.Length, 1);
        rotation = Mathf.Repeat(mapSynchronizer.baseTime * rotationsPerSecond * 360, 360);
        lerp = perPositionLerp.Evaluate(pointstimer);
        if (oldTimer > pointstimer)
        {

            index += 1;

            if (index >= points.Length) index = 0;

            fromPos = toPos;
            toPos = points[index].position;

        }

        oldTimer = pointstimer;
        rb.position = Vector2.Lerp(fromPos, toPos, lerp);
        rb.linearVelocity = (rb.position - previousPosition) / Time.fixedDeltaTime;
        rb.angularVelocity = rotationsPerSecond * 360;
        rb.rotation = rotation;

        previousPosition = rb.position;

    }

}
