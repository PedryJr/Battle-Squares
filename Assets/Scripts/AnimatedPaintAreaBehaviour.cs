using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public sealed class AnimatedPaintAreaBehaviour : PaintAreaBehaviour
{
    [SerializeField]
    private SkinFrameBehaviour skinFrameBehaviour;

    /*
        [SerializeField]
        PixelManager pixelManager;*/

    [SerializeField]
    public List<SkinFrameBehaviour> skinFrames;

    [SerializeField]
    private Transform frameContainer;

    [SerializeField]
    public PixelManager pixelManager;

    [SerializeField]
    public RectTransform selector;
    [SerializeField]
    public TMP_Text indicatorInSelector;

    private PlayerSynchronizer playerSynchronizer;

    public int editingIndex = 0;
    public int lastEditingIndex = 0;

    public int copy;
    public int lastCopy;

    public int paste;
    public int lastPaste;

    float indicatorTimer = 1;
    Color indicatorTravelColor = new Color(0.0F, 0.0F, 0.0F, 0.0F);
    Color indicatorHighlightColor = new Color(1.0F, 1.0F, 1.0F, 1.0F);

    private Inputs input;

    private void Start()
    {
        input = new Inputs();
        input.Editor.Copy.performed += (context) => { copy++; };
        input.Editor.Copy.canceled += (context) => { copy--; };
        input.Editor.Paste.performed += (context) => { paste++; };
        input.Editor.Paste.canceled += (context) => { paste--; };
        input.Editor.CopyC.performed += (context) => { copy++; };
        input.Editor.CopyC.canceled += (context) => { copy--; };
        input.Editor.PasteV.performed += (context) => { paste++; };
        input.Editor.PasteV.canceled += (context) => { paste--; };
        input.Enable();

        skinFrames = new List<SkinFrameBehaviour>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        for (int i = 0; i < playerSynchronizer.skinData.frames; i++)
        {
            SkinFrameBehaviour newframe = Instantiate(skinFrameBehaviour, frameContainer);
            skinFrames.Add(newframe);
            skinFrames[i].frameIndex = i;
            newframe.frameData = playerSynchronizer.skinData.skinFrames[i].frame;
            skinFrames[i].UPDATEPREVIEW();
        }
    }

    private void Update()
    {
        if (editingIndex != lastEditingIndex)
        {
            indicatorTimer = 0;
            pixelManager.frameIndex = editingIndex;

            if (editingIndex == 0) pixelManager.skinTolerance = 45;
            else pixelManager.skinTolerance = 24;

            lastEditingIndex = editingIndex;
        }
        
        indicatorTimer += Time.deltaTime * 5F;
        indicatorTimer = Mathf.Clamp01(indicatorTimer);

        skinFrames[editingIndex].UPDATEPREVIEW();

        Vector3 targetPos = math.transform(skinFrames[editingIndex].transform.localToWorldMatrix, skinFrames[editingIndex].body.rectTransform.localPosition);
        targetPos.x = Mathf.Lerp(targetPos.x - 0.1F, targetPos.x, math.smoothstep(0F, 1F, indicatorTimer * 1.3F));

        indicatorInSelector.color = Color.Lerp(indicatorTravelColor, indicatorHighlightColor, indicatorTimer);
        selector.position = Vector3.Lerp(selector.position, targetPos, Time.deltaTime * 100F * indicatorTimer);
    }

    private void LateUpdate()
    {
        if (copy != lastCopy)
        {
            if (copy == 2)
            {
                string copyCode = MyExtentions.BoolArrayToString(skinFrames[editingIndex].frameData);
                GUIUtility.systemCopyBuffer = copyCode;
                /*
                                GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(copiedFrame, Formatting.Indented);*/
            }

            lastCopy = copy;
        }

        if (paste != lastPaste)
        {
            if (paste == 2)
            {
                bool[] pastedFrame = MyExtentions.StringToBoolArray(GUIUtility.systemCopyBuffer);
                if (pastedFrame != null) for (int i = 0; i < pixelManager.colored.Length; i++) pixelManager.colored[i] = pastedFrame[i];

                /*                bool[] pastedFrame = MyExtentions.ByteArrayToBoolArray(JsonConvert.DeserializeObject<byte[]>(GUIUtility.systemCopyBuffer), 116);

                                if (pastedFrame != null) for (int i = 0; i < pixelManager.colored.Length; i++) pixelManager.colored[i] = pastedFrame[i];*/
            }

            lastPaste = paste;
        }
    }

    public void DELETEFRAME(SkinFrameBehaviour skinFrameToRemove)
    {
        int indexToRemove = skinFrameToRemove.frameIndex;

        if (editingIndex == skinFrameToRemove.frameIndex)
        {
            if (editingIndex == skinFrames.Count - 1) editingIndex = skinFrames.Count - 2;
        }

        skinFrames.Remove(skinFrameToRemove);
        Destroy(skinFrameToRemove.gameObject);

        float remeberFrameRate = playerSynchronizer.skinData.frameRate;
        bool rememberAnimate = playerSynchronizer.skinData.animate;
        bool[] rememberValid = new bool[playerSynchronizer.skinData.skinFrames.Length];
        List<bool[]> rememberFrames = new List<bool[]>();

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames.Length; i++)
        {
            rememberValid[i] = playerSynchronizer.skinData.skinFrames[i].valid;

            if (i != indexToRemove) rememberFrames.Add(playerSynchronizer.skinData.skinFrames[i].frame);
        }

        playerSynchronizer.skinData = new SkinData();
        playerSynchronizer.skinData.skinFrames = new SkinData.SkinFrame[skinFrames.Count];
        playerSynchronizer.skinData.frameRate = remeberFrameRate;
        playerSynchronizer.skinData.animate = rememberAnimate;
        playerSynchronizer.skinData.frames = skinFrames.Count;

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames.Length; i++)
        {
            playerSynchronizer.skinData.skinFrames[i].valid = rememberValid[i];
            playerSynchronizer.skinData.skinFrames[i].frame = rememberFrames[i];
        }

        for (int i = 0; i < skinFrames.Count; i++)
        {
            skinFrames[i].frameIndex = i;
        }
        skinFrames[editingIndex].SELECT();
    }

    public void CREATEFRAME()
    {
        bool[] newSkinData = new bool[116];
        for (int i = 0; i < newSkinData.Length; i++)
        {
            newSkinData[i] = true;
        }

        SkinFrameBehaviour skinFrameToAdd = Instantiate(skinFrameBehaviour, frameContainer);
        skinFrames.Add(skinFrameToAdd);
        int newIndex = skinFrames.Count - 1;
        skinFrameToAdd.frameIndex = newIndex;
        skinFrameToAdd.frameData = newSkinData;

        float remeberFrameRate = playerSynchronizer.skinData.frameRate;
        bool rememberAnimate = playerSynchronizer.skinData.animate;
        bool[] rememberValid = new bool[playerSynchronizer.skinData.skinFrames.Length];

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames.Length; i++)
        {
            rememberValid[i] = playerSynchronizer.skinData.skinFrames[i].valid;
        }

        playerSynchronizer.skinData = new SkinData();
        playerSynchronizer.skinData.skinFrames = new SkinData.SkinFrame[skinFrames.Count];
        playerSynchronizer.skinData.frames = skinFrames.Count;
        playerSynchronizer.skinData.frameRate = remeberFrameRate;
        playerSynchronizer.skinData.animate = rememberAnimate;

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames.Length; i++)
        {
            playerSynchronizer.skinData.skinFrames[i].frame = skinFrames[i].frameData;

            if (i == newIndex) playerSynchronizer.skinData.skinFrames[i].frame = newSkinData;
            else playerSynchronizer.skinData.skinFrames[i].valid = rememberValid[i];
        }

        skinFrames[newIndex].frameIndex = newIndex;
        skinFrameToAdd.UPDATEPREVIEW();
    }

    public void MOVEFRAME(int posChange, int sourceIndex)
    {
        int targetIndex = sourceIndex + posChange;

        if (targetIndex < 0 || targetIndex >= skinFrames.Count) return;

        bool[] tempFrameData = skinFrames[sourceIndex].frameData;
        skinFrames[sourceIndex].frameData = skinFrames[targetIndex].frameData;
        playerSynchronizer.skinData.skinFrames[sourceIndex].frame = skinFrames[sourceIndex].frameData;

        skinFrames[targetIndex].frameData = tempFrameData;
        playerSynchronizer.skinData.skinFrames[targetIndex].frame = skinFrames[targetIndex].frameData;

        if (targetIndex == editingIndex)
        {
            for (int i = 0; i < pixelManager.colored.Length; i++) pixelManager.colored[i] = skinFrames[targetIndex].frameData[i];
        }

        if (sourceIndex == editingIndex)
        {
            for (int i = 0; i < pixelManager.colored.Length; i++) pixelManager.colored[i] = skinFrames[sourceIndex].frameData[i];
        }

        skinFrames[sourceIndex].UPDATEPREVIEW();
        skinFrames[targetIndex].UPDATEPREVIEW();
    }

    private void OnDestroy()
    {
        input.Disable();
        input.Dispose();
    }
}