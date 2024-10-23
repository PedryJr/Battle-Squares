using System.Text;
using TMPro;
using UnityEngine;

public class PainterExec : MonoBehaviour
{

    [SerializeField]
    CursorBehaviour cursorBehaviour;

    PixelManager pixelManager;

    [SerializeField]
    TMP_Text requiredField;

    [SerializeField]
    string requiredFormat;

    StringBuilder stringBuilder = new StringBuilder();

    public bool filling;
    public bool lastFilling;

    public bool previewFilling;
    public bool lastPreviewFilling;

    public bool isHovering;

    float colorTimer;
    float oldColorTimer;

    float colorLerp;

    Color fromColor;
    Color toColor;

    public float newSlowHover;
    float oldSlowHover;

    float fromSlowHover;
    float toSlowHover;

    float slowHoverTimer;

    float slowHover;
    float lastSlowHover;

    private void Awake()
    {

        pixelManager = GetComponent<PixelManager>();
        
    }

    private void LateUpdate()
    {

        if (oldSlowHover != newSlowHover)
        {
            fromSlowHover = slowHover;
            toSlowHover = newSlowHover;

            slowHoverTimer = 0;

            oldSlowHover = newSlowHover;
        }

        if (slowHoverTimer < 1) slowHoverTimer += Time.deltaTime * 10;
        if (slowHoverTimer > 1) slowHoverTimer = 1;

        slowHover = Mathf.Lerp(fromSlowHover, toSlowHover, slowHoverTimer);

        if(slowHover > 0)
        {

            if (!cursorBehaviour.skipColorManip)
            {

                cursorBehaviour.skipColorManip = true;
                UpdateFromToCursorColor();
            }

            if (previewFilling != lastPreviewFilling)
            {

                UpdateFromToCursorColor();
                lastPreviewFilling = previewFilling;

            }

            if(filling != lastFilling)
            {
                UpdateFromToCursorColor();
                lastFilling = filling;
            }

            if (colorTimer < 1) colorTimer += Time.deltaTime * 5;
            if (colorTimer > 1) colorTimer = 1;

            if (oldColorTimer != colorTimer)
            {

                colorLerp = Mathf.SmoothStep(0, 1, colorTimer);
                cursorBehaviour.image.color = Color.Lerp(fromColor, toColor, colorLerp);

                oldColorTimer = colorTimer;

            }

        }
        else
        {

            cursorBehaviour.skipColorManip = false;

        }

        stringBuilder.Append(pixelManager.skinValue);
        stringBuilder.Append(requiredFormat);
        stringBuilder.Append(pixelManager.skinTolerance);

        requiredField.text = stringBuilder.ToString();
        stringBuilder.Clear();

    }

    void UpdateFromToCursorColor()
    {

        if (previewFilling)
        {
            fromColor = cursorBehaviour.image.color;
            toColor = Color.black;
        }
        else
        {
            fromColor = cursorBehaviour.image.color;
            toColor = Color.white;
        }

        colorTimer = 0;

    }

    private void OnDisable()
    {

        CursorBehaviour.SetColor(Color.white, Color.gray);

    }

}
