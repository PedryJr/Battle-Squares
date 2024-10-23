using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedPaintAreaBehaviour : PaintAreaBehaviour
{

    [SerializeField]
    SkinFrameBehaviour skinFrameBehaviour;
/*
    [SerializeField]
    PixelManager pixelManager;*/

    [SerializeField]
    public List<SkinFrameBehaviour> skinFrames;

    [SerializeField]
    Transform frameContainer;

    [SerializeField]
    public PixelManager pixelManager;

    PlayerSynchronizer playerSynchronizer;

    public int editingIndex = 0;
    public int lastEditingIndex = 0;

    private void Start()
    {
        
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
        
        if(editingIndex != lastEditingIndex)
        {
            pixelManager.frameIndex = editingIndex;
            
            if(editingIndex == 0) pixelManager.skinTolerance = 12;
            else pixelManager.skinTolerance = 45;

            lastEditingIndex = editingIndex;

        }

        skinFrames[editingIndex].UPDATEPREVIEW();

    }

    public void DELETEFRAME(int index)
    {

        if(index == skinFrames.Count - 1) editingIndex = index - 1;

        SkinFrameBehaviour skinFrameToRemove = skinFrames[index];
        skinFrames.RemoveAt(index);
        Destroy(skinFrameToRemove.gameObject);

        playerSynchronizer.skinData = new SkinData();
        playerSynchronizer.skinData.skinFrames = new SkinData.SkinFrame[skinFrames.Count];

        for (int i = 0; i < skinFrames.Count; i++)
        {
            skinFrames[i].frameIndex = i;
        }
        playerSynchronizer.skinData.frames = skinFrames.Count;

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

        playerSynchronizer.skinData = new SkinData();
        playerSynchronizer.skinData.skinFrames = new SkinData.SkinFrame[skinFrames.Count];
        playerSynchronizer.skinData.frames = skinFrames.Count;

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames.Length; i++)
        {
            playerSynchronizer.skinData.skinFrames[i].frame = skinFrames[i].frameData;
        }

        skinFrames[newIndex].frameIndex = newIndex;
        skinFrameToAdd.UPDATEPREVIEW();

    }

    public void MOVEFRAME(int posChange)
    {

    }

}
