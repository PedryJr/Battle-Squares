using System;
using UnityEngine;

public sealed class TabModeObject : MonoBehaviour
{

    public Action tabModeEnabled = () => { };
    public Action tabModeDisabled = () => { };

    public void EnableTabMode() => tabModeEnabled();
    public void DisableDabMode() => tabModeDisabled();

}
