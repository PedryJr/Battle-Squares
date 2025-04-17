using UnityEngine;
using UnityEngine.UI;

public sealed class SkinPreviewBehaviour : MonoBehaviour
{

    [SerializeField]
    AnimatedPaintAreaBehaviour areaBehaviour;

    [SerializeField]
    PlayerSynchronizer playerSynchronizer;

    float animationTimer;
    int animationIndex;
    int lastAnimationIndex = -1;

    [SerializeField]
    Image nozzlePreview;

    [SerializeField]
    Image bodyPreview;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Update()
    {

        ApplyPlayerAnimation();

    }

    public void PREVIEW()
    {

        animationTimer = 1;

    }

    void ApplyPlayerAnimation()
    {

        if (animationTimer > 0) animationTimer -= Time.deltaTime * (playerSynchronizer.skinData.frameRate / areaBehaviour.skinFrames.Count);
        if (animationTimer < 0) animationTimer = 0;
        if (animationTimer == 0)
        {
            animationIndex = 0;
        }
        else
        {
            animationIndex = Mathf.FloorToInt((1 - animationTimer) * areaBehaviour.skinFrames.Count);
        }

        if (animationIndex != lastAnimationIndex)
        {
            bodyPreview.sprite = areaBehaviour.skinFrames[animationIndex].body.sprite;
            nozzlePreview.sprite = areaBehaviour.skinFrames[animationIndex].nozzle.sprite;
            lastAnimationIndex = animationIndex;
        }

    }

}
