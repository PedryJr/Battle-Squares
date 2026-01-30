using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceDescriptor : MonoBehaviour
{

    [SerializeField] TMP_Text name;
    [SerializeField] TMP_Text id;
    [SerializeField] TMP_Text fac;

    InputDevice inputDevice;

    public void InitializeDescriptor(InputDevice device)
    {
        name.text = device.name;
        id.text = device.deviceId.ToString();
        inputDevice = device;

    }
}
