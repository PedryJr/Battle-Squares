using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class RankAnimationBehaviour : MonoBehaviour
{

    [SerializeField] public double requiredMMR = 1000.0;
    [SerializeField] public int rankNR = 0;

    [SerializeField] Image[] dimImages;
    [SerializeField] Image[] brightImages;

    [SerializeField] Transform[] targetGraphics;
    [SerializeField] Transform[] showTargets;
    [SerializeField] Transform[] hideTargets;
    [SerializeField] AnimationData animationData;

    ModelData[] cachedFrom;
    ModelData[] cachedTo;

    AnimationState currentState;
    AnimationState targetState;

    float timeLine;
    float animationDuration;
    AnimationCurve animationCurve;
    bool isAnimating;


    [ContextMenu("Test Show")]
    void TestShow()
    {
        Init(AnimationState.Hide, null);
        SetAnimationState(AnimationState.Show);
    }

    [ContextMenu("Test Hide")]
    void TestHide()
    {
        Init(AnimationState.Show, null);
        SetAnimationState(AnimationState.Hide);
    }

    #region Data

    struct ModelData
    {
        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale;

        public static ModelData FromTransform(Transform t)
        {
            return new ModelData
            {
                localPosition = t.localPosition,
                localRotation = t.localEulerAngles,
                localScale = t.localScale
            };
        }

        public void ApplyTo(Transform t)
        {
            t.localPosition = localPosition;
            t.localEulerAngles = localRotation;
            t.localScale = localScale;
        }

        public static ModelData Lerp(ModelData a, ModelData b, float t)
        {
            return new ModelData
            {
                localPosition = Vector3.LerpUnclamped(a.localPosition, b.localPosition, t),
                localRotation = Vector3.LerpUnclamped(a.localRotation, b.localRotation, t),
                localScale = Vector3.LerpUnclamped(a.localScale, b.localScale, t)
            };
        }
    }

    [Serializable]
    struct AnimationData
    {
        public AnimationCurve currentToHideCurve;
        public float currentToHideAnimationTime;

        public AnimationCurve currentToShowCurve;
        public float currentToShowAnimationDuration;
    }

    #endregion

    #region Public API

    public void Init(AnimationState state, PlayerBehaviour player)
    {
        if (player)
        {
            for (int i = 0; i < dimImages.Length; i++) dimImages[i].color = player.PlayerColor.SecondaryColor;
            for (int i = 0; i < brightImages.Length; i++) brightImages[i].color = player.PlayerColor.PrimaryColor;
        }

        currentState = state;
        targetState = state;
        isAnimating = false;

        Transform[] targets = GetTargetsForState(state);
        ApplyInstant(targets);
    }

    public void SetAnimationState(AnimationState state)
    {
        if (state == targetState)
            return;

        CacheFrom();
        CacheTo(state);

        targetState = state;
        currentState = state;

        SelectAnimationData(state);

        timeLine = 0f;
        isAnimating = true;
    }

    #endregion

    void Update()
    {
        if (!isAnimating)
            return;

        timeLine += Time.deltaTime;
        float t = Mathf.Clamp01(timeLine / animationDuration);
        float curveT = animationCurve.Evaluate(t);

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            ModelData blended = ModelData.Lerp(cachedFrom[i], cachedTo[i], curveT);
            blended.ApplyTo(targetGraphics[i]);
        }

        if (t >= 1f)
        {
            isAnimating = false;
        }
    }

    #region Helpers

    void CacheFrom()
    {
        cachedFrom = new ModelData[targetGraphics.Length];
        for (int i = 0; i < targetGraphics.Length; i++)
            cachedFrom[i] = ModelData.FromTransform(targetGraphics[i]);
    }

    void CacheTo(AnimationState state)
    {
        Transform[] targets = GetTargetsForState(state);
        cachedTo = new ModelData[targets.Length];

        for (int i = 0; i < targets.Length; i++)
            cachedTo[i] = ModelData.FromTransform(targets[i]);
    }

    void ApplyInstant(Transform[] targets)
    {
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            targetGraphics[i].localPosition = targets[i].localPosition;
            targetGraphics[i].localEulerAngles = targets[i].localEulerAngles;
            targetGraphics[i].localScale = targets[i].localScale;
        }
    }

    Transform[] GetTargetsForState(AnimationState state)
    {
        return state == AnimationState.Show ? showTargets : hideTargets;
    }

    void SelectAnimationData(AnimationState state)
    {
        if (state == AnimationState.Show)
        {
            animationCurve = animationData.currentToShowCurve;
            animationDuration = animationData.currentToShowAnimationDuration;
        }
        else
        {
            animationCurve = animationData.currentToHideCurve;
            animationDuration = animationData.currentToHideAnimationTime;
        }
    }
    public enum AnimationState
    {
        Show,
        Hide
    }

    #endregion
}
