using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerSynchronizer;

public sealed class ScoreBoard : MonoBehaviour
{

    [SerializeField]
    TMP_Text text;

    [SerializeField]
    Image image;

    public PlayerData playerData;


    public void SetScore(PlayerData player)
    {

        playerData = player;

        text.text = player.square.score.ToString();

        image.color = player.square.PlayerColor.PrimaryColor;

    }

}
