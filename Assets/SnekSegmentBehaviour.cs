using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public partial class SnekSegmentBehaviour : AutoPooledBehaviour
{

    Rigidbody2D rb;
    CircleCollider2D col;

    Transform target;
    PlayerBehaviour owner;
    SnekTailBehaviour snekTail;

    SnekSegmentState previousState = SnekSegmentState.None;
    SnekSegmentState currentState = SnekSegmentState.None;

    [SerializeField]
    SnekSegmentParams objParams;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        target = transform; //Just for safety, will be set to player or next segment.
    }

    private void OnDestroy()
    {
        snekTail.FixLinks();
    }

    //Run only when segment is appended to another tail
    public void Initialize(SnekTailBehaviour snekTail)
    {
        owner = snekTail.owner;
        target = snekTail.nextTarget;

        this.snekTail = snekTail;
    }

        //Run only on a list where a segment has been lost
    public void RelinkSegment(Transform newTarget)
    {
        target = newTarget;
    }

    protected override void OnReturnedToPool()
    {
        previousState = SnekSegmentState.None;
        currentState = SnekSegmentState.None;
    }

    protected override void OnSpawned()
    {
        previousState = SnekSegmentState.None;
        currentState = SnekSegmentState.None;
    }

    public void SetState(SnekSegmentState newState) => currentState = newState;

    void Update()
    {

        if (DidTransitionFromSpawnedToHeld(previousState, currentState)) TransitionFromSpawnToHeld();
        if (DidTransitionFromHeldToDeadOwner(previousState, currentState)) TransitionFromHeldToDeadOwner();
        if (DidTransitionFromHeldToDeadSegment(previousState, currentState)) TransitionFromHeldToDeadSegment();

        switch (currentState)
        {
            case SnekSegmentState.Spawned: RunTargetingU(objParams.spawnedStateParameters); break;
            case SnekSegmentState.Held: RunTargetingU(objParams.heldStateParameters); break;
            case SnekSegmentState.DeadSegment: break;
            case SnekSegmentState.DeadOwner: RunTargetingU(objParams.deadOwnerStateParameters); break;
        }

        previousState = currentState;

    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case SnekSegmentState.Spawned: RunTargetingFU(objParams.spawnedStateParameters); break;
            case SnekSegmentState.Held: RunTargetingFU(objParams.heldStateParameters); break;
            case SnekSegmentState.DeadSegment: break;
            case SnekSegmentState.DeadOwner: RunTargetingFU(objParams.deadOwnerStateParameters); break;
        }
    }

    void RunTargetingU(StateParameters currentParams)
    {
        Vector3 targetScale = new Vector3(currentParams.targetScale.x, currentParams.targetScale.y, 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, currentParams.scalingSpeed * Time.deltaTime);
    }
    void RunTargetingFU(StateParameters currentParams)
    {

        float distanceToTarget = Vector2.Distance(rb.position, target.position);

        Vector2 toTarget = ((Vector2)target.position) - rb.position;
        Vector2 breakVec = toTarget.normalized * currentParams.targetDistance;

        float overShoot = breakVec.magnitude;

        if(overShoot > distanceToTarget)
        {
            rb.AddForce(-rb.linearVelocity * currentParams.breakingForce, ForceMode2D.Force);
        }
        else
        {

            Vector2 calculatedForce = (toTarget - breakVec) * currentParams.followSpeed;
            rb.AddForce(calculatedForce, ForceMode2D.Force);
        }

        Vector2 resultingVelocity = rb.linearVelocity;
        float resultingSpeed = resultingVelocity.magnitude;
        float breakingFrac = Mathf.Pow(resultingSpeed / currentParams.maxSpeed, currentParams.maxSpeedExpo);

        rb.AddForce(-resultingVelocity * breakingFrac);

    }
}

//Transitions
public partial class SnekSegmentBehaviour
{ 

    void TransitionFromSpawnToHeld()
    {
        VLog.Log("&cSegment transition &f[&aSPAWN -> &aHELD&f]");
    }

    void TransitionFromHeldToDeadOwner()
    {
        VLog.Log("&cSegment transition &f[&HELD -> &DEAD_OWNER&f]");
    }

    void TransitionFromHeldToDeadSegment()
    {
        VLog.Log("&cSegment transition &f[&HELD -> &DEAD_SEGMENT&f]");
    }

}


    //State helpers
public partial class SnekSegmentBehaviour
{

    [Serializable]
    public struct StateParameters
    {
        [Header("Velocities/Speeds")]
        [SerializeField] public float colorChangeSpeed;
        [SerializeField] public float followSpeed;
        [SerializeField] public float scalingSpeed;
        [SerializeField] public float breakingForce;
        [SerializeField] public float maxSpeed;
        [SerializeField] public float maxSpeedExpo;

        [Header("Targets")]
        [SerializeField] public float targetDistance;
        [SerializeField] public float maxAccelDistance;
        [SerializeField] public Vector2 targetScale;
    }

    public enum SnekSegmentState
    {
        Spawned = 0,
        Held = 1,
        DeadSegment = 2,
        DeadOwner = 3,
        None = 4,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool DidTransitionFromSpawnedToHeld(SnekSegmentState prev, SnekSegmentState curr)
    {
        return (prev == SnekSegmentState.Spawned && curr == SnekSegmentState.Held);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool DidTransitionFromHeldToDeadSegment(SnekSegmentState prev, SnekSegmentState curr)
    {
        return (prev == SnekSegmentState.Held && curr == SnekSegmentState.DeadSegment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool DidTransitionFromHeldToDeadOwner(SnekSegmentState prev, SnekSegmentState curr)
    {
        return (prev == SnekSegmentState.Held && curr == SnekSegmentState.DeadOwner);
    }

}
