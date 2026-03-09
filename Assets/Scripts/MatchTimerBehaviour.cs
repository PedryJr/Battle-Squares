using System.Runtime.CompilerServices;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class MatchTimerBehaviour : MonoBehaviour
{
    [SerializeField] TMP_Text uiTimer;
    [SerializeField] float matchDurationM = 3f;
    [SerializeField] float matchDurationS = 0f;
    [SerializeField] AnimationCurve tickAnimation;
    PlayerSynchronizer playerSynchronizer;
    MLTrainingManager mLTrainingManager;
    ScoreManager scoreManager;
    RectTransform rectTransform;

    [SerializeField]
    Vector2 sizeA;
    [SerializeField]
    Vector2 anchorA;
    [SerializeField]
    Vector2 anchorMaxA;
    [SerializeField]
    Vector2 anchorMinA;
    [SerializeField]
    Vector2 offsetMinA;
    [SerializeField]
    Vector2 offsetMaxA;
    [SerializeField]
    Vector3 localPositionA;
    [SerializeField]
    Vector3 localScaleA;
    [SerializeField]
    Quaternion localRotationA;

    [SerializeField]
    Vector2 sizeB;
    [SerializeField]
    Vector2 anchorB;
    [SerializeField]
    Vector2 anchorMaxB;
    [SerializeField]
    Vector2 anchorMinB;
    [SerializeField]
    Vector2 offsetMinB;
    [SerializeField]
    Vector2 offsetMaxB;
    [SerializeField]
    Vector3 localPositionB;
    [SerializeField]
    Vector3 localScaleB;
    [SerializeField]
    Quaternion localRotationB;

    [ContextMenu("Assign A")]
    public void AssignSizeA()
    {
        RectTransform t = GetComponent<RectTransform>();
        sizeA = t.sizeDelta;
        anchorA = t.anchoredPosition;
        anchorMaxA = t.anchorMax;
        anchorMinA = t.anchorMin;
        offsetMinA = t.offsetMin;
        offsetMaxA = t.offsetMax;
        localPositionA = t.localPosition;
        localScaleA = t.localScale;
        localRotationA = t.localRotation;
    }

    [ContextMenu("Assign B")]
    public void AssignSizeB()
    {
        RectTransform t = GetComponent<RectTransform>();
        sizeB = t.sizeDelta;
        anchorB = t.anchoredPosition;
        anchorMaxB = t.anchorMax;
        anchorMinB = t.anchorMin;
        offsetMinB = t.offsetMin;
        offsetMaxB = t.offsetMax;
        localPositionB = t.localPosition;
        localScaleB = t.localScale;
        localRotationB = t.localRotation;
    }

    float matchTimer;

    bool matchEnded = false;
    bool overTime = false;

    private void Awake()
    {
        mLTrainingManager = FindAnyObjectByType<MLTrainingManager>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        if (playerSynchronizer.playerIdentities.Count <= 1)
        {
            Destroy(this.gameObject);
            return;
        }
        rectTransform = GetComponent<RectTransform>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        matchTimer = matchDurationM * 60f + matchDurationS;
    }

    void Update()
    {
        if (playerSynchronizer.localSquare.spawnBuffer)
        {
            UpdateTimerUI();
            return;
        }
        else if(!mLTrainingManager.isTraining)
        {
            matchTimer -= Time.deltaTime;
            matchTimer = Mathf.Clamp(matchTimer, 0, matchDurationM * 60f + matchDurationS);
            UpdateTimerUI();
            if (!matchEnded) CheckForMatchEnd();
        }
    }

    void UpdateTimerUI()
    {
        if (overTime) uiTimer.text = "OVERTIME";
        else
        {
            if(matchTimer <= 0) return;
            int minutesLeft, secondsLeft;
            (minutesLeft, secondsLeft) = ConvertToMinutesSeconds(Mathf.FloorToInt(matchTimer));
            uiTimer.text = FormatTime(minutesLeft, secondsLeft);
        }

        Vector2 temp2;
        Vector3 temp3;
        Quaternion tempQ;

        float t = tickAnimation.Evaluate(math.frac(matchTimer));

        LerpUnclamped(ref sizeA, ref sizeB, t, out temp2);
        rectTransform.sizeDelta = temp2;

        LerpUnclamped(ref anchorA, ref anchorB, t, out temp2);
        rectTransform.anchoredPosition = temp2;

        LerpUnclamped(ref anchorMaxA, ref anchorMaxB, t, out temp2);
        rectTransform.anchorMax = temp2;

        LerpUnclamped(ref anchorMinA, ref anchorMinB, t, out temp2);
        rectTransform.anchorMin = temp2;

        LerpUnclamped(ref offsetMinA, ref offsetMinB, t, out temp2);
        rectTransform.offsetMin = temp2;

        LerpUnclamped(ref offsetMaxA, ref offsetMaxB, t, out temp2);
        rectTransform.offsetMax = temp2;

        LerpUnclamped(ref localPositionA, ref localPositionB, t, out temp3);
        rectTransform.localPosition = temp3;

        SlerpUnclamped(ref localRotationA, ref localRotationB, t, out tempQ);
        rectTransform.localRotation = tempQ;

        LerpUnclamped(ref localScaleA, ref localScaleB, t, out temp3);
        rectTransform.localScale = temp3;


    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LerpUnclamped(ref Vector2 a, ref Vector2 b, float t, out Vector2 result)
    {
        result.x = a.x + (b.x - a.x) * t;
        result.y = a.y + (b.y - a.y) * t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LerpUnclamped(ref Vector3 a, ref Vector3 b, float t, out Vector3 result)
    {
        result.x = a.x + (b.x - a.x) * t;
        result.y = a.y + (b.y - a.y) * t;
        result.z = a.z + (b.z - a.z) * t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SlerpUnclamped(ref Quaternion a, ref Quaternion b, float t, out Quaternion result)
    {
        float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        float blend = dot < 0f ? -1f : 1f;

        result.x = a.x + (b.x * blend - a.x) * t;
        result.y = a.y + (b.y * blend - a.y) * t;
        result.z = a.z + (b.z * blend - a.z) * t;
        result.w = a.w + (b.w * blend - a.w) * t;

        float invMag = 1.0f / Mathf.Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);
        result.x *= invMag;
        result.y *= invMag;
        result.z *= invMag;
        result.w *= invMag;
    }


    void CheckForMatchEnd()
    {
        if (matchTimer > 0) return;

        PlayerBehaviour highest = null;
        PlayerBehaviour secondHighest = null;

        foreach (var playerData in playerSynchronizer.playerIdentities)
        {
            PlayerBehaviour player = playerData.square;

            if (highest == null || player.score > highest.score)
            {
                secondHighest = highest;
                highest = player;
            }
            else if (secondHighest == null || player.score > secondHighest.score)
            {
                secondHighest = player;
            }
        }

        if (secondHighest != null && secondHighest.score == highest.score)
        {
            overTime = true;
            return;
        }

        matchEnded = true;
        scoreManager.ForceEndGame();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string FormatTime(int minutes, int seconds) => $"{minutes:D2}:{seconds:D2}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int minutes, int seconds) ConvertToMinutesSeconds(int totalSeconds) => (totalSeconds / 60, totalSeconds % 60);


}
