using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ListInputDevices : MonoBehaviour
{
    [SerializeField] InputDeviceDescriptor prefabReference;
    [SerializeField] RectTransform contentParent;

    private readonly Dictionary<int, InputDeviceDescriptor> descriptors = new Dictionary<int, InputDeviceDescriptor>();

    private void OnEnable()
    {
        foreach (var device in InputSystem.devices) AddDevice(device);
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        foreach (var descriptor in descriptors.Values) Destroy(descriptor.gameObject);
        descriptors.Clear();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                AddDevice(device);
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                RemoveDevice(device);
                break;
        }
    }

    private void AddDevice(InputDevice device)
    {
        if (descriptors.ContainsKey(device.deviceId)) return;

        var instance = Instantiate(prefabReference, contentParent);
        instance.InitializeDescriptor(device);

        descriptors.Add(device.deviceId, instance);
    }

    private void RemoveDevice(InputDevice device)
    {
        if (!descriptors.TryGetValue(device.deviceId, out var descriptor))
            return;

        Destroy(descriptor.gameObject);
        descriptors.Remove(device.deviceId);
    }
}
