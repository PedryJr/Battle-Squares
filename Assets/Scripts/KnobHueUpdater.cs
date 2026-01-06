using UnityEngine;
using UnityEngine.UI;

public sealed class KnobHueUpdater : MonoBehaviour
{

    Image sliderKnob;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        sliderKnob = GetComponent<Image>();

    }

    private void Update()
    {

        if (!playerSynchronizer.localSquare) return;

        sliderKnob.color = playerSynchronizer.localSquare.PlayerColor.UIKnobColor;

    } 

}
