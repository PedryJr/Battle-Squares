using UnityEngine;

public sealed class GameSpeedBehaviour : MonoBehaviour
{


    public void ChangeGameSpeed(float value)
    {

        Time.timeScale = value;

    }

}
