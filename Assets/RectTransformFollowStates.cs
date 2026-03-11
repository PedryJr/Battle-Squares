using UnityEngine;

public class RectTransformFollowStates : MonoBehaviour
{
    [SerializeField]
    RectTransform[] targets;

    [SerializeField]
    float transitionSpeed = 7.0f;

    [SerializeField]
    int testState = 0;
    [ContextMenu("Test State")]
    void TS() => SetTargetState(testState);

    private int targetState = 0;
    private int fromState = 0;
    private float transitionProgress = 1.0f;

    private Vector3 fromPosition;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransformFollowStates requires a RectTransform component!");
            return;
        }

        CacheCurrentTransform();
    }

    void Update()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (targets == null || targets.Length == 0) return;

        transitionProgress += Time.deltaTime * transitionSpeed;
        transitionProgress = Mathf.Clamp01(transitionProgress);

        ApplyTransition();
    }

    void OnDrawGizmos()
    {
        //funny thing
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    public void SetTargetState(int newState)
    {
        if (targets == null || newState < 0 || newState >= targets.Length)
        {
            Debug.LogWarning($"Invalid target state {newState}. Valid range: 0-{(targets?.Length ?? 0) - 1}");
            return;
        }

        if (targets[newState] == null)
        {
            Debug.LogWarning($"Target at index {newState} is null!");
            return;
        }

        CacheCurrentTransform();

        fromState = targetState;
        targetState = newState;
        transitionProgress = 0.0f;
    }

    public void SetTargetStateImmediate(int newState)
    {
        if (targets == null || newState < 0 || newState >= targets.Length || targets[newState] == null)
            return;

        targetState = newState;
        fromState = newState;
        transitionProgress = 1.0f;

        CopyFromTarget(targets[newState]);
    }

    void CacheCurrentTransform()
    {
        if (rectTransform == null)
            return;

        fromPosition = rectTransform.position;
    }

    void ApplyTransition()
    {
        if (targetState >= targets.Length || targets[targetState] == null)
            return;

        RectTransform target = targets[targetState];
        float t = MyExtentions.EaseOutQuad(transitionProgress);

        Vector3 position = Vector2.Lerp(fromPosition, target.position, t);
        rectTransform.position = position;
        Vector3 localPosition = rectTransform.localPosition;
        localPosition.z = 0f;
        rectTransform.localPosition = localPosition;

    }

    void CopyFromTarget(RectTransform target) => rectTransform.position = target.position;

    void OnDrawGizmosSelected()
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue; 
            Gizmos.color = i == targetState ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(targets[i].position, targets[i].sizeDelta);
        }
    }
}