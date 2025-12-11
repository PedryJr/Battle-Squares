using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ButtonColorSettings", menuName = "ButtonColorSettings")]
public class ButtonHoverAnimationColorSettings : ScriptableObject
{

    [SerializeField]
    [ColorUsage(showAlpha: false, hdr: true)]
    Color OnHoveredColor = new Color(0, 0, 0, 1);
    [SerializeField]
    [ColorUsage(showAlpha: false, hdr: true)]
    Color OffHoveredColor = new Color(0, 0, 0, 1);

    public Color onHoveredColor => OnHoveredColor;
    public Color offHoveredColor => OffHoveredColor;

}
