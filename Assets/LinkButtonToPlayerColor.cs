using System.Runtime.CompilerServices;
using UnityEngine;

public class LinkButtonToPlayerColor : MonoBehaviour
{

    ButtonHoverAnimation button;
    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        button = GetComponent<ButtonHoverAnimation>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        if (playerSynchronizer.localSquare) button.onHoveredColor = playerSynchronizer.localSquare.PlayerColor.HighlightedWeaponColor;
    }
}