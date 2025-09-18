using TMPro;
using UnityEngine;

public sealed class PopupTextBehaviour : MonoBehaviour
{

    [SerializeField]
    float startEndMultiplier = 0.5f;

    [SerializeField]
    float beginTime;
    [SerializeField]
    float stayTime;
    [SerializeField]
    float endTime;

    float animationTime;

    Vector3 beginScale;
    Vector3 stayScale;
    Vector3 endScale;

    [SerializeField]
    Color beginColor;
    [SerializeField]
    Color stayColor;
    [SerializeField]
    Color endColor;

    TMP_Text textField;

    AnimationState animationState = AnimationState.Begin;

    [SerializeField]
    Canvas canvas;
    private void Awake()
    {
        textField = GetComponent<TMP_Text>();

        endScale = transform.localScale * startEndMultiplier;
        beginScale = transform.localScale * startEndMultiplier;
        stayScale = transform.localScale;

        canvas.worldCamera = Camera.main;
    }

    private void Update()
    {

        animationTime += Time.deltaTime;

        float duration = animationState switch
        {
            AnimationState.Begin => beginTime,
            AnimationState.Stay => stayTime,
            AnimationState.End => endTime,
            _ => 0,
        };

        Color fromColor = animationState switch
        {
            AnimationState.Begin => beginColor,
            AnimationState.Stay => stayColor,
            AnimationState.End => stayColor,
            _ => new Color(),
        };

        Color toColor = animationState switch
        {
            AnimationState.Begin => stayColor,
            AnimationState.Stay => stayColor,
            AnimationState.End => endColor,
            _ => new Color(),
        };

        Vector3 fromScale = animationState switch
        {
            AnimationState.Begin => beginScale,
            AnimationState.Stay => stayScale,
            AnimationState.End => stayScale,
            _ => endScale,
        };

        Vector3 toScale = animationState switch
        {
            AnimationState.Begin => stayScale,
            AnimationState.Stay => stayScale,
            AnimationState.End => endScale,
            _ => endScale,
        };

        float lerp = animationTime / duration;

        transform.localScale = Vector3.Lerp(fromScale, toScale, lerp);
        textField.color = Color.Lerp(fromColor, toColor, lerp);

        if(lerp >= 1f)
        {
            if(animationState == AnimationState.End) Destroy(gameObject);
            else animationState++;
            animationTime = 0;
        }

    }

    public enum AnimationState
    {
        Begin = 0,
        Stay = 1,
        End = 2,
    }

}
