using TMPro;
using UnityEngine;

public sealed class EditorFrameRateBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text textField;

    PlayerSynchronizer playerSynchronizer;

    float frameRate;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        frameRate = playerSynchronizer.skinData.frameRate;
        textField.text = $"Framerate - {Mathf.RoundToInt(frameRate)}";

    }

    private void Update()
    {
        
        if(frameRate != playerSynchronizer.skinData.frameRate)
        {
            playerSynchronizer.skinData.frameRate = frameRate;
            textField.text = $"Framerate - {Mathf.RoundToInt(frameRate)}";
        }

    }

    public void UPFRAMERATE()
    {
        frameRate = Mathf.RoundToInt(frameRate + 1);
    }

    public void DOWNFRAMERATE()
    {
        frameRate = Mathf.RoundToInt(frameRate - 1);
        if(frameRate < 1) frameRate = 1;
    }

}
