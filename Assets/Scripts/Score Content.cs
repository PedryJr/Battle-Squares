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
    [SerializeField]
    RankAnimationBehaviour[] rankAnimationBehaviour;

    public void Init(Sprite image, string name, int score, double oldMMRVal, double newMMRVal, PlayerBehaviour player)
    {/*
        oldMMr.text = $"OldMMR: {oldMMRVal:F1}";
        newMMr.text = $"NewMMR: {newMMRVal:F1}";*/
        PFPDisplay.sprite = image;
        nameDisplay.text = name;
        scoreDisplay.text = "Score: " + score.ToString();

        int oldRank = 0;
        int newRank = 0;

        for (int i = 0; i < rankAnimationBehaviour.Length; i++)
        {
                rankAnimationBehaviour[i].Init(RankAnimationBehaviour.AnimationState.Hide, player);
                if (oldMMRVal > rankAnimationBehaviour[i].requiredMMR) oldRank = rankAnimationBehaviour[i].rankNR;
                if (newMMRVal > rankAnimationBehaviour[i].requiredMMR) newRank = rankAnimationBehaviour[i].rankNR;
        }

        if(oldRank == newRank)
        {
            for (int i = 0; i < rankAnimationBehaviour.Length; i++)
            {
                if (rankAnimationBehaviour[i].rankNR == oldRank) rankAnimationBehaviour[i].Init(RankAnimationBehaviour.AnimationState.Show, player);
            }
        }
        else
        {
            for (int i = 0; i < rankAnimationBehaviour.Length; i++)
            {
                if (rankAnimationBehaviour[i].rankNR == oldRank)
                {
                    rankAnimationBehaviour[i].Init(RankAnimationBehaviour.AnimationState.Show, player);
                    rankAnimationBehaviour[i].SetAnimationState(RankAnimationBehaviour.AnimationState.Hide);
                }
                if (rankAnimationBehaviour[i].rankNR == newRank)
                {
                    rankAnimationBehaviour[i].Init(RankAnimationBehaviour.AnimationState.Hide, player);
                    rankAnimationBehaviour[i].SetAnimationState(RankAnimationBehaviour.AnimationState.Show);
                }
            }
        }

    }

}
