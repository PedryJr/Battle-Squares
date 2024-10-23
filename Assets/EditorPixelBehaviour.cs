using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[BurstCompile]
public sealed class EditorPixelBehaviour : MonoBehaviour
{

    public int siblingIndex;

    public Image image;

    PixelStateAnimator pixelStateAnimator;
    PainterExec painterExec;
    CursorBehaviour cursorBehaviour;
    Skin activeSkin;
    PixelManager pixelManager;
    PlayerSynchronizer playerSynchronizer;

    bool isHovering;
    bool lastIsHovering;
    bool click;
    bool lastClick;

    bool colored;
    bool lastColored;
    float colorLerp;
    float colorTimer;
    float lastColorTimer;

    [BurstCompile]
    private void Awake()
    {
        image = GetComponent<Image>();
        pixelStateAnimator = GetComponent<PixelStateAnimator>();
        cursorBehaviour = FindAnyObjectByType<CursorBehaviour>();
        painterExec = GetComponentInParent<PainterExec>();
        pixelManager = GetComponentInParent<PixelManager>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        activeSkin = FindAnyObjectByType<Skin>();

        siblingIndex = transform.GetSiblingIndex();

    }

    [BurstCompile]
    private void Start()
    {
        colored = playerSynchronizer.skinData.skinFrames[pixelManager.frameIndex].frame[siblingIndex];

        pixelManager.lastIsHovering[siblingIndex] = lastIsHovering;
        pixelManager.colored[siblingIndex] = colored;
        pixelManager.lastColored[siblingIndex] = lastColored;
        pixelManager.click[siblingIndex] = click;
        pixelManager.lastClick[siblingIndex] = lastClick;
        pixelManager.fromColor[siblingIndex] = colored ? pixelManager.emptyColor : pixelManager.filledColor;
        pixelManager.toColor[siblingIndex] = colored ? pixelManager.filledColor : pixelManager.emptyColor;
        pixelManager.currentColor[siblingIndex] = colored ? pixelManager.filledColor : pixelManager.emptyColor;
        pixelManager.colorTimer[siblingIndex] = colorTimer;
        pixelManager.colorLerp[siblingIndex] = colorLerp;

    }
}