using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class GameModeButtonBehaviour : MonoBehaviour
{

    [SerializeField]
    ScoreManager.Mode mode;

    ScoreManager scoreManager;

    private void Start()
    {
        
        scoreManager = FindAnyObjectByType<ScoreManager>();

    }

    public void SELECT()
    {

        scoreManager.UpdateModeAsHost(mode);

    }

}
