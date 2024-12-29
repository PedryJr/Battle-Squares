using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinFrameBehaviour : MonoBehaviour
{

    public bool[] frameData;
    public int frameIndex;
    public int lastFrameIndex = -1;

    bool lastClick;

    string baseText = "Base";
    string deletableText = "Delete";

    [SerializeField] public Image nozzle;
    [SerializeField] public Image body;

    [SerializeField] ButtonHoverAnimation deleteButton;
    [SerializeField] Image deleteButtonImage;
    [SerializeField] TMP_Text deleteButtonText;
    [SerializeField] Sprite baseFrameDeleteSprite;
    [SerializeField] Sprite normalFrameDeleteSprite;

    PlayerSynchronizer playerSynchronizer;
    CursorBehaviour cursorBehaviour;
    AnimatedPaintAreaBehaviour animatedPaintAreaBehaviour;

    UIAudio uIAudio;

    Texture2D nozzleTexture;
    Texture2D bodyTexture;

    private void Awake()
    {

        uIAudio = Resources.Load<UIAudio>("UIAudio");

        nozzleTexture = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        nozzleTexture.filterMode = FilterMode.Point;
        nozzle.sprite = Sprite.Create(nozzleTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, -1), 4);
        bodyTexture = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        bodyTexture.filterMode = FilterMode.Point;
        body.sprite = Sprite.Create(bodyTexture, new Rect(0, 0, 10, 10), new Vector2(0.5f, 1), 10);

        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();
        cursorBehaviour = FindFirstObjectByType<CursorBehaviour>();
        animatedPaintAreaBehaviour = GetComponentInParent<AnimatedPaintAreaBehaviour>();

    }

    private void OnEnable()
    {

        UPDATEPREVIEW();

    }

    private void Update()
    {

        if(frameIndex != lastFrameIndex)
        {

            if(frameIndex == 0)
            {

                deleteButton.RemoveTriggers();
                deleteButton.enabled = false;
                deleteButtonImage.sprite = baseFrameDeleteSprite;
                deleteButtonText.text = baseText;

            }
            else
            {

                deleteButton.enabled = true;
                deleteButtonImage.sprite = normalFrameDeleteSprite;
                deleteButtonText.text = deletableText;

            }

            playerSynchronizer.skinData.skinFrames[frameIndex].frame = frameData;
            lastFrameIndex = frameIndex;

        }

    }

    public void UPDATEPREVIEW()
    {

        bool[] frame = playerSynchronizer.skinData.skinFrames[frameIndex].frame;
        frameData = frame;

        Span<bool> bodySkin = stackalloc bool[100];
        Span<bool> nozzleSkin = stackalloc bool[16];

        for (int i = 0; i < frame.Length; i++)
        {

            if(i < 100)
            {
                bodySkin[i] = frame[i];
            }
            else
            {
                nozzleSkin[i - 100] = frame[i];
            }

        }

        CreateTextureFromBoolArray10BY10(bodySkin);
        CreateTextureFromBoolArray4BY4(nozzleSkin);

    }

    public void SELECT()
    {
/*
        uIAudio.PlayClick(1f);*/
        for (int i = 0; i < animatedPaintAreaBehaviour.pixelManager.colored.Length; i++)
        {
            animatedPaintAreaBehaviour.pixelManager.colored[i] = frameData[i];
        }

        animatedPaintAreaBehaviour.editingIndex = frameIndex;

    }

    public void DELETE()
    {
/*
        uIAudio.PlayClick(0.8f);*/
        animatedPaintAreaBehaviour.DELETEFRAME(this);

    }

    public void MOVEUP()
    {
/*
        uIAudio.PlayClick(1.2f);*/
        animatedPaintAreaBehaviour.MOVEFRAME(-1, frameIndex);

    }

    public void MOVEDOWN()
    {
/*
        uIAudio.PlayClick(1.2f);*/
        animatedPaintAreaBehaviour.MOVEFRAME(1, frameIndex);

    }

    public void CreateTextureFromBoolArray10BY10(Span<bool> boolArray)
    {

        bool[] rotatedArray = new bool[100];

        for (int i = 0; i < 100; i++)
        {
            rotatedArray[i] = boolArray[99 - i];
        }

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                int index = i * 10 + j;

                Color color = rotatedArray[index] ? Color.white : Color.clear;
                bodyTexture.SetPixel(j, i, color);
            }
        }

        bodyTexture.Apply();
    }

    public void CreateTextureFromBoolArray4BY4(Span<bool> boolArray)
    {

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int index = i * 4 + j;

                Color color = boolArray[index] ? Color.white : Color.clear;
                nozzleTexture.SetPixel(j, i, color);
            }
        }

        nozzleTexture.Apply();
    }

}
