using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class PlayerControllerManager : MonoBehaviour
{
    [SerializeField]
    private string[] scenesThatListenForDevices;

    // Track all registered player controllers
    private List<PlayerController> playerControllers = new List<PlayerController>();

    // Track all available devices and their connection order
    private List<InputDevice> availableDevices = new List<InputDevice>();
    private Dictionary<InputDevice, int> deviceConnectionOrder = new Dictionary<InputDevice, int>();
    private int deviceConnectionCounter = 0;

    // Track device-to-player assignments
    private Dictionary<InputDevice, PlayerController> deviceAssignments = new Dictionary<InputDevice, PlayerController>();

    // Cache for keyboard and mouse
    private Keyboard keyboard;
    private Mouse mouse;

    private void Awake()
    {
        // Register for input system events
        InputSystem.onDeviceChange += OnDeviceChange;
        SceneManager.activeSceneChanged += OnSceneChanged;

        // Initialize with existing devices
        DiscoverInitialDevices();
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene previousScene, Scene newScene)
    {
        // Check if the new scene should listen for devices
        if (scenesThatListenForDevices != null && scenesThatListenForDevices.Length > 0)
        {
            bool shouldListen = System.Array.Exists(scenesThatListenForDevices,
                sceneName => sceneName == newScene.name);

            if (shouldListen)
            {
                // Rebind all controllers in this scene
                RebindAllControllers();
            }
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                HandleDeviceAdded(device);
                break;

            case InputDeviceChange.Reconnected:
                HandleDeviceReconnected(device);
                break;

            case InputDeviceChange.Removed:
                HandleDeviceRemoved(device);
                break;

            case InputDeviceChange.Disconnected:
                HandleDeviceDisconnected(device);
                break;
        }
    }

    private void HandleDeviceAdded(InputDevice device)
    {
        // Add device to tracking
        if (!availableDevices.Contains(device))
        {
            availableDevices.Add(device);
            deviceConnectionOrder[device] = deviceConnectionCounter++;

            // Cache keyboard and mouse references
            if (device is Keyboard)
                keyboard = device as Keyboard;
            if (device is Mouse)
                mouse = device as Mouse;

            Debug.Log($"Device added: {device.name} (Order: {deviceConnectionOrder[device]})");
        }

        // Trigger rebinding for all controllers
        RebindAllControllers();
    }

    private void HandleDeviceReconnected(InputDevice device)
    {
        // Treat reconnection as a fresh connection with new priority
        if (!availableDevices.Contains(device))
        {
            availableDevices.Add(device);
        }

        // Update connection order to make it most recent
        deviceConnectionOrder[device] = deviceConnectionCounter++;

        Debug.Log($"Device reconnected: {device.name} (New Order: {deviceConnectionOrder[device]})");

        // Trigger rebinding
        RebindAllControllers();
    }

    private void HandleDeviceRemoved(InputDevice device)
    {
        // Remove from available devices
        availableDevices.Remove(device);
        deviceConnectionOrder.Remove(device);

        // Clear any assignments
        if (deviceAssignments.ContainsKey(device))
        {
            PlayerController controller = deviceAssignments[device];
            deviceAssignments.Remove(device);
            controller?.UnpairDevice(device);
        }

        Debug.Log($"Device removed: {device.name}");

        // Trigger rebinding for remaining controllers
        RebindAllControllers();
    }

    private void HandleDeviceDisconnected(InputDevice device)
    {
        // Keep device in tracking but mark it as unavailable
        Debug.Log($"Device disconnected: {device.name}");

        // Optionally unpair immediately or wait for reconnection
        // For now, we'll unpair to free up the slot
        if (deviceAssignments.ContainsKey(device))
        {
            PlayerController controller = deviceAssignments[device];
            deviceAssignments.Remove(device);
            controller?.UnpairDevice(device);
        }

        availableDevices.Remove(device);
        RebindAllControllers();
    }

    private void DiscoverInitialDevices()
    {
        // Find all currently connected devices
        foreach (var device in InputSystem.devices)
        {
            if (!availableDevices.Contains(device))
            {
                availableDevices.Add(device);
                deviceConnectionOrder[device] = deviceConnectionCounter++;

                // Cache keyboard and mouse
                if (device is Keyboard)
                    keyboard = device as Keyboard;
                if (device is Mouse)
                    mouse = device as Mouse;
            }
        }

        Debug.Log($"Discovered {availableDevices.Count} initial devices");
    }

    public bool IsDeviceValidForRegistrationEXTERN(InputDevice device)
    {
        // Check if the device is already assigned to a player
        bool isDeviceFree = !deviceAssignments.ContainsKey(device);

        if (playerControllers.Count > 0)
        {
            // Allow player 1 to "steal" a device (except keyboard/mouse)
            if (deviceAssignments.TryGetValue(device, out PlayerController controller))
            {
                if (playerControllers.IndexOf(controller) == 0 && device is not Keyboard && device is not Mouse)
                {
                    isDeviceFree = true;
                }
            }
        }

        // Keyboard and mouse are always reserved for player 1
        if ((device is Keyboard || device is Mouse) && playerControllers.Count > 0)
        {
            isDeviceFree = false;
        }

        return isDeviceFree;
    }

    internal void SpawnController(PlayerController newController)
    {
        // Register the new controller
        if (!playerControllers.Contains(newController))
        {
            playerControllers.Add(newController);
            Debug.Log($"Controller spawned. Total controllers: {playerControllers.Count}");
        }

        // Trigger rebinding for all controllers
        RebindAllControllers();
    }

    internal void DespawnController(PlayerController oldController)
    {
        if (oldController == null)
        {
            Debug.LogWarning("Attempted to despawn null controller");
            return;
        }

        if (!playerControllers.Contains(oldController))
        {
            Debug.LogWarning($"Controller not found in active controllers list");
            return;
        }

        Debug.Log($"Despawning controller. Current count: {playerControllers.Count}");

        // Unpair all devices from this controller before removing it
        var devicesToUnpair = deviceAssignments
            .Where(kvp => kvp.Value == oldController)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var device in devicesToUnpair)
        {
            oldController.UnpairDevice(device);
            deviceAssignments.Remove(device);
            Debug.Log($"Unpaired {device.name} from despawning controller");
        }

        // Remove the controller from tracking
        playerControllers.Remove(oldController);
        Debug.Log($"Controller despawned. Remaining controllers: {playerControllers.Count}");

        // Rebind remaining controllers to redistribute devices
        RebindAllControllers();
    }

    public void UnregisterController(PlayerController controller)
    {
        // Remove controller from tracking
        playerControllers.Remove(controller);

        // Remove any device assignments for this controller
        var devicesToRemove = deviceAssignments.Where(kvp => kvp.Value == controller)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var device in devicesToRemove)
        {
            deviceAssignments.Remove(device);
        }

        Debug.Log($"Controller unregistered. Remaining controllers: {playerControllers.Count}");

        // Rebind remaining controllers
        RebindAllControllers();
    }

    private void RebindAllControllers()
    {
        // Clear all current assignments
        var previousAssignments = new Dictionary<InputDevice, PlayerController>(deviceAssignments);
        deviceAssignments.Clear();

        // Unpair all devices from all controllers
        foreach (var controller in playerControllers)
        {
            foreach (var device in previousAssignments.Keys)
            {
                controller.UnpairDevice(device);
            }
        }

        if (playerControllers.Count == 0)
        {
            return;
        }

        // Sort controllers by their registration order (assuming first in list is player 1)
        var sortedControllers = new List<PlayerController>(playerControllers);

        // Get gamepads sorted by connection order (most recent first)
        var gamepads = availableDevices
            .Where(d => d is Gamepad)
            .OrderByDescending(d => deviceConnectionOrder.ContainsKey(d) ? deviceConnectionOrder[d] : -1)
            .ToList();

        // MAPPING RULES:
        // 1. Player 1 always gets keyboard + mouse
        // 2. Other players get gamepads in reverse chronological order (most recent first)

        // Assign Player 1 (if exists)
        if (sortedControllers.Count > 0)
        {
            PlayerController player1 = sortedControllers[0];

            // Player 1 gets keyboard and mouse
            if (keyboard != null)
            {
                player1.PairDevice(keyboard);
                deviceAssignments[keyboard] = player1;
            }

            if (mouse != null)
            {
                player1.PairDevice(mouse);
                deviceAssignments[mouse] = player1;
            }

            Debug.Log($"Player 1 assigned: Keyboard + Mouse");
        }

        // Assign remaining players to gamepads
        int gamepadIndex = 0;
        for (int i = 1; i < sortedControllers.Count && gamepadIndex < gamepads.Count; i++)
        {
            PlayerController player = sortedControllers[i];
            InputDevice gamepad = gamepads[gamepadIndex];

            player.PairDevice(gamepad);
            deviceAssignments[gamepad] = player;

            Debug.Log($"Player {i + 1} assigned: {gamepad.name} (Connection order: {deviceConnectionOrder[gamepad]})");

            gamepadIndex++;
        }

        // Special case: If there's only 1 player and gamepads are available,
        // player 1 can also use the most recent gamepad
        if (sortedControllers.Count == 1 && gamepads.Count > 0)
        {
            PlayerController player1 = sortedControllers[0];
            InputDevice mostRecentGamepad = gamepads[0];

            if (!deviceAssignments.ContainsKey(mostRecentGamepad))
            {
                player1.PairDevice(mostRecentGamepad);
                deviceAssignments[mostRecentGamepad] = player1;

                Debug.Log($"Player 1 also assigned: {mostRecentGamepad.name} (most recent gamepad)");
            }
        }
    }

    // Public method to manually trigger rebinding (useful for debugging)
    public void ForceRebind()
    {
        RebindAllControllers();
    }

    // Get current device assignments (useful for debugging)
    public Dictionary<PlayerController, List<InputDevice>> GetCurrentAssignments()
    {
        var assignments = new Dictionary<PlayerController, List<InputDevice>>();

        foreach (var kvp in deviceAssignments)
        {
            if (!assignments.ContainsKey(kvp.Value))
            {
                assignments[kvp.Value] = new List<InputDevice>();
            }
            assignments[kvp.Value].Add(kvp.Key);
        }

        return assignments;
    }
}