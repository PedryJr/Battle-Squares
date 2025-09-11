using UnityEngine;

public sealed class LevelPiece : MonoBehaviour
{

    ForceFieldBlocker FFBlocker;

    private void Awake()
    {
        EnsureForcefieldBlocker();
    }

    void EnsureForcefieldBlocker()
    {
        if (!(FFBlocker = GetComponent<ForceFieldBlocker>())) FFBlocker = gameObject.AddComponent<ForceFieldBlocker>();
    }

}
