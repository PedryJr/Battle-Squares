using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuEffectBehaviour : MonoBehaviour
{

    static bool runOnce = false;

    [SerializeField]
    float animationTime;

    [SerializeField]
    Visual[] visuals;

    [SerializeField]
    Shapes[] shapes;

    [Serializable]
    struct Visual
    {
        public Color startColor;
        public Color endColor;
        public AnimationCurve curve;
        public int type;
        public Image image;
        public TMP_Text text;
    }

    [Serializable]
    struct Shapes
    {
        public RectTransform startTransform;
        public RectTransform endTransform;
        public RectTransform targetTransform;
        public AnimationCurve curve;
    }

    float timer;

    private void Awake()
    {
        if (runOnce)
        {
            timer = 1F;
            for (int i = 0; i < shapes.Length; i++)
            {
                AnimateTransform(shapes[i]);
            }

            for (int i = 0; i < visuals.Length; i++)
            {
                AnimateColor(visuals[i]);
            }
            Destroy(this);
        }
    }

    void Update()
    {

        timer += Time.deltaTime * 1F/animationTime;

        if(timer >= 1F) 
        {
            timer = 1F;

            for (int i = 0; i < shapes.Length; i++)
            {
                AnimateTransform(shapes[i]);
            }

            for (int i = 0; i < visuals.Length; i++)
            {
                AnimateColor(visuals[i]);
            }
            runOnce = true;
            Destroy(this);
        }
        else
        {

            for (int i = 0; i < shapes.Length; i++)
            {
                AnimateTransform(shapes[i]);
            }

            for (int i = 0; i < visuals.Length; i++)
            {
                AnimateColor(visuals[i]);
            }
        }

    }

    void AnimateColor(Visual visual)
    {

        Color result = Color.Lerp(visual.startColor, visual.endColor, visual.curve.Evaluate(timer));

        switch (visual.type)
        {
            case 0: visual.image.color = result; break;
            case 1: visual.text.color = result; break;
        }

    }
    void AnimateTransform(Shapes shape)
    {

        Vector3 result1 = Vector3.Lerp(shape.startTransform.position, shape.endTransform.position, shape.curve.Evaluate(timer));
        Vector3 result2 = Vector3.Lerp(shape.startTransform.localScale, shape.endTransform.localScale, shape.curve.Evaluate(timer));

        shape.targetTransform.position = result1;
        shape.targetTransform.localScale = result2;

    }

}
