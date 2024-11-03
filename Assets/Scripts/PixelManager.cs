using UnityEngine;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System;
using Unity.Mathematics;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using System.Linq;
using Unity.Entities.UniversalDelegates;

[BurstCompile]
public sealed class PixelManager : MonoBehaviour
{

    public int skinTolerance;
    public int skinValue;
    public int frameIndex = 0;
    int oldSkinValue;

    PainterExec painterExec;
    CursorBehaviour cursorBehaviour;
    PlayerSynchronizer playerSynchronizer;
    PixelStateAnimator[] animators;
    EditorPixelBehaviour[] pixels;

    public NativeArray<Vector2> currentSize;
    public NativeArray<Vector2> fromSize;
    public NativeArray<Vector2> toSize;
    public NativeArray<bool> isHovering;
    public NativeArray<float> enterHoverTransitionTime;
    public NativeArray<float> exitHoverTransitionTime;
    public NativeArray<float> animationTimer;

    public NativeArray<bool> lastIsHovering;
    public NativeArray<bool> colored;
    public NativeArray<bool> lastColored;
    public NativeArray<bool> click;
    public NativeArray<bool> lastClick;
    public NativeArray<Color> fromColor;
    public NativeArray<Color> toColor;
    public NativeArray<Color> currentColor;
    public NativeArray<float> colorTimer;
    public NativeArray<float> colorLerp;

    public NativeArray<bool> filling;
    public NativeArray<bool> previewFilling;
    public NativeArray<float> newSlowHover;

    PixelColorJob colorJob;
    JobHandle handle;

    [SerializeField]
    int pixelCount;

    [SerializeField]
    public Color filledColor;

    [SerializeField]
    public Color emptyColor;

    [SerializeField]
    public Color hoverFilledColor;

    [SerializeField]
    public Color hoverEmptyColor;

    [BurstCompile]
    private void Awake()
    {

        animators = GetComponentsInChildren<PixelStateAnimator>();
        pixels = GetComponentsInChildren<EditorPixelBehaviour>();
        painterExec = GetComponent<PainterExec>();
        cursorBehaviour = FindAnyObjectByType<CursorBehaviour>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        currentSize = new NativeArray<Vector2>(pixelCount, Allocator.Persistent);
        fromSize = new NativeArray<Vector2>(pixelCount, Allocator.Persistent);
        toSize = new NativeArray<Vector2>(pixelCount, Allocator.Persistent);
        isHovering = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        enterHoverTransitionTime = new NativeArray<float>(pixelCount, Allocator.Persistent);
        exitHoverTransitionTime = new NativeArray<float>(pixelCount, Allocator.Persistent);
        animationTimer = new NativeArray<float>(pixelCount, Allocator.Persistent);

        lastIsHovering = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        colored = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        lastColored = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        click = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        lastClick = new NativeArray<bool>(pixelCount, Allocator.Persistent);
        fromColor = new NativeArray<Color>(pixelCount, Allocator.Persistent);
        toColor = new NativeArray<Color>(pixelCount, Allocator.Persistent);
        currentColor = new NativeArray<Color>(pixelCount, Allocator.Persistent);
        colorTimer = new NativeArray<float>(pixelCount, Allocator.Persistent);
        colorLerp = new NativeArray<float>(pixelCount, Allocator.Persistent);

        filling = new NativeArray<bool>(new bool[1], Allocator.Persistent);
        previewFilling = new NativeArray<bool>(new bool[1], Allocator.Persistent);
        newSlowHover = new NativeArray<float>(new float[1], Allocator.Persistent);

    }

    [BurstCompile]
    private void Update()
    {

        filling[0] = painterExec.filling;
        previewFilling[0] = painterExec.previewFilling;
        newSlowHover[0] = painterExec.newSlowHover;

        colorJob = new PixelColorJob
        {

            deltaTime = Time.deltaTime,
            cursorClick = cursorBehaviour.click,

            filling = filling,
            previewFilling = previewFilling,
            newSlowHover = newSlowHover,

            hoverFilledColor = hoverFilledColor,
            filledColor = filledColor,
            hoverEmptyColor = hoverEmptyColor,
            emptyColor = emptyColor,

            currentSize = currentSize,
            fromSize = fromSize,
            toSize = toSize,
            isHovering = isHovering,
            enterHoverTransitionTime = enterHoverTransitionTime,
            exitHoverTransitionTime = exitHoverTransitionTime,
            animationTimer = animationTimer,

            lastIsHovering = lastIsHovering,
            colored = colored,
            lastColored = lastColored,
            click = click,
            lastClick = lastClick,
            fromColor = fromColor,
            toColor = toColor,
            currentColor = currentColor,
            colorTimer = colorTimer,
            colorLerp = colorLerp,

        };

        handle = colorJob.Schedule(pixelCount, 16);

        handle.Complete();

        skinValue = 0;
        for (int i = 0; i < pixelCount; i++)
        {

            animators[i].rectTransform.sizeDelta = currentSize[i];
            pixels[i].image.color = currentColor[i];
            skinValue += colored[i] && i < 100 ? 1 : 0;

        }

        painterExec.filling = colorJob.filling[0];
        painterExec.previewFilling = colorJob.previewFilling[0];
        painterExec.newSlowHover = colorJob.newSlowHover[0];

    }

    private void LateUpdate()
    {

        if (oldSkinValue != skinValue) playerSynchronizer.skinData.skinFrames[frameIndex].frame = colored.ToArray();
        oldSkinValue = skinValue;

    }

    [BurstCompile]
    private void OnDestroy()
    {

        currentSize.Dispose();
        fromSize.Dispose();
        toSize.Dispose();
        isHovering.Dispose();
        enterHoverTransitionTime.Dispose();
        exitHoverTransitionTime.Dispose();
        animationTimer.Dispose();

        lastIsHovering.Dispose();
        colored.Dispose();
        lastColored.Dispose();
        click.Dispose();
        lastClick.Dispose();
        fromColor.Dispose();
        toColor.Dispose();
        currentColor.Dispose();
        colorTimer.Dispose();
        colorLerp.Dispose();

        filling.Dispose();
        previewFilling.Dispose();
        newSlowHover.Dispose();

    }

}

[BurstCompile]
public struct PixelColorJob : IJobParallelFor
{

    public float deltaTime;
    public bool cursorClick;

    [NativeDisableParallelForRestriction]
    public NativeArray<bool> filling;

    [NativeDisableParallelForRestriction]
    public NativeArray<bool> previewFilling;

    [NativeDisableParallelForRestriction]
    public NativeArray<float> newSlowHover;

    public Color hoverFilledColor;
    public Color filledColor;
    public Color hoverEmptyColor;
    public Color emptyColor;

    public NativeArray<Vector2> currentSize;
    public NativeArray<Vector2> fromSize;
    public NativeArray<Vector2> toSize;
    public NativeArray<bool> isHovering;
    public NativeArray<float> enterHoverTransitionTime;
    public NativeArray<float> exitHoverTransitionTime;
    public NativeArray<float> animationTimer;

    public NativeArray<bool> lastIsHovering;
    public NativeArray<bool> colored;
    public NativeArray<bool> lastColored;
    public NativeArray<bool> lastClick;
    public NativeArray<bool> click;
    public NativeArray<Color> fromColor;
    public NativeArray<Color> toColor;
    public NativeArray<Color> currentColor;
    public NativeArray<float> colorTimer;
    public NativeArray<float> colorLerp;

    [BurstCompile]
    public void Execute(int index)
    {

        float colorTimerMultiplier = 0;

        click[index] = cursorClick;

        if (isHovering[index])
        {

            if (animationTimer[index] < 1) animationTimer[index] += deltaTime / enterHoverTransitionTime[index];
            else animationTimer[index] = 1;
            colorTimerMultiplier = enterHoverTransitionTime[index];

        }
        else
        {
            if (animationTimer[index] < 1) animationTimer[index] += deltaTime / exitHoverTransitionTime[index];
            else animationTimer[index] = 1;
            colorTimerMultiplier = exitHoverTransitionTime[index];
        }

        float lerp = animationTimer[index];
        currentSize[index] = Vector2.LerpUnclamped(fromSize[index], toSize[index], lerp);

        click[index] = cursorClick;

        if (isHovering[index] != lastIsHovering[index])
        {
            animationTimer[index] = 0;
            if (isHovering[index])
            {
                if (click[index])
                {
                    colored[index] = filling[0];
                }
                else
                {
                    previewFilling[0] = colored[index];
                }

                newSlowHover[0] += 1;
            }
            else
            {
                newSlowHover[0] -= 1;
            }

            fromColor[index] = currentColor[index];

            if (colored[index])
            {
                toColor[index] = isHovering[index] ? hoverFilledColor : filledColor;
            }
            else
            {
                toColor[index] = isHovering[index] ? hoverEmptyColor : emptyColor;
            }

            colorTimer[index] = 0;

            lastIsHovering[index] = isHovering[index];
        }

        if (click[index] != lastClick[index])
        {
            if (isHovering[index] && click[index])
            {
                colored[index] = !colored[index];
                filling[0] = colored[index];
            }

            fromColor[index] = currentColor[index];

            if (colored[index])
            {
                toColor[index] = isHovering[index] ? hoverFilledColor : filledColor;
            }
            else
            {
                toColor[index] = isHovering[index] ? hoverEmptyColor : emptyColor;
            }

            colorTimer[index] = 0;

            lastClick[index] = click[index];
        }

        if (colored[index] != lastColored[index])
        {

            fromColor[index] = currentColor[index];

            if (colored[index])
            {
                toColor[index] = isHovering[index] ? hoverFilledColor : filledColor;
            }
            else
            {
                toColor[index] = isHovering[index] ? hoverEmptyColor : emptyColor;
            }

            colorTimer[index] = 0;

            lastColored[index] = colored[index];
        }

        if (colorTimer[index] < 1)
        {
            colorTimer[index] = Mathf.Clamp01(colorTimer[index] + (deltaTime / colorTimerMultiplier));
            colorLerp[index] = Mathf.SmoothStep(0, 1, colorTimer[index]);
            currentColor[index] = Color.Lerp(fromColor[index], toColor[index], colorLerp[index]);
        }

    }

}
