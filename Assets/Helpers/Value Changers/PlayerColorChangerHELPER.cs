using UnityEngine;

public class PlayerColorChangerHELPER : MonoBehaviour
{

    public enum CType { S, V }
    public CType type;

    PlayerSynchronizer playerSynchronizer;
    private void Awake() => playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    public void CHANGECOLOR(float value)
    {

        if (type == CType.S) Saturation(value);
        if (type == CType.V) Valuate(value);
    }

    void Saturation(float sat)
    {
        if (!playerSynchronizer.localSquare) return;
        playerSynchronizer.localSquare.s = sat;
    }

    void Valuate(float sat)
    {
        if (!playerSynchronizer.localSquare) return;
        playerSynchronizer.localSquare.v = sat;
    }


}