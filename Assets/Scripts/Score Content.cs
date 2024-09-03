using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreContent : MonoBehaviour
{
    [SerializeField]
    TMP_Text nameDisplay;
    [SerializeField]
    TMP_Text scoreDisplay;
    [SerializeField]
    Image PFPDisplay;

    public void Init(Sprite image, string name, int score)
    {

        PFPDisplay.sprite = image;
        nameDisplay.text = name;
        scoreDisplay.text = "Score: " + score.ToString();

    }

}
