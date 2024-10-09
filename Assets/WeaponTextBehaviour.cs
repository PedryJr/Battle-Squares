using TMPro;
using UnityEngine;

public class WeaponTextBehaviour : MonoBehaviour
{

    TMP_Text equippedClassesField;

    PlayerSynchronizer playerSynchronizer; 

    private void Start()
    {

        equippedClassesField = GetComponent<TMP_Text>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Update()
    {

        if (!playerSynchronizer) return;

        string weapon1, weapon2;
        weapon1 = playerSynchronizer.localSquare.nozzleBehaviour.primary.ToString();
        weapon2 = playerSynchronizer.localSquare.nozzleBehaviour.secondary.ToString();

        weapon1 = weapon1.Substring(0, 1).ToUpper() + weapon1.Substring(1, weapon1.Length - 1);
        weapon2 = weapon2.Substring(0, 1).ToUpper() + weapon2.Substring(1, weapon2.Length - 1);

        equippedClassesField.text = weapon1 + " - " + weapon2;

    }

}
