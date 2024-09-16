using TMPro;
using UnityEngine;

public sealed class GameModeDisplayBehaviour : MonoBehaviour
{

    TMP_Text textDisplay;

    private void Awake()
    {
        
        textDisplay = GetComponent<TMP_Text>();

        ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
        if(scoreManager) textDisplay.text = scoreManager.gameMode.ToString();

    }

    public void DisplayGameMode(ScoreManager.Mode mode)
    {

        textDisplay.text = mode.ToString();

    }

}
