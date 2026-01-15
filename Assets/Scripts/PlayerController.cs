using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using static UnityEngine.InputSystem.InputAction;

public sealed partial class PlayerController : MonoBehaviour
{
    [SerializeField]
    public List<int> displayConsumedIDS;

    Rigidbody2D controllerTarget;
    public PlayerBehaviour playerBehaviour;

    Inputs inputs;
    InputUser inputUser;
    private int currentDeviceId = -1;

    // Static lock object to prevent race conditions
    private static readonly object pairingLock = new object();
    public static List<int> consumedDeviceIDs = new List<int>();

    // Track which PlayerController owns which device ID
    private static Dictionary<int, PlayerController> deviceOwnership = new Dictionary<int, PlayerController>();

    // Track when each controller claimed their device (for first-come-first-served)
    private static Dictionary<int, float> deviceClaimTime = new Dictionary<int, float>();

    private void OnDestroy()
    {
        // Clean up when player is destroyed
        lock (pairingLock)
        {
            if (currentDeviceId != -1)
            {
                consumedDeviceIDs.Remove(currentDeviceId);
                deviceOwnership.Remove(currentDeviceId);
                deviceClaimTime.Remove(currentDeviceId);
                currentDeviceId = -1;
            }
        }

        if (inputUser.valid)
        {
            inputUser.UnpairDevices();
        }
    }

    private void FixedUpdate()
    {
        EnsurePlayerControllerIsBound();
    }

    void EnsurePlayerControllerIsBound()
    {
        displayConsumedIDS = consumedDeviceIDs;

        // RUNTIME CHECK: Detect if another controller is using our device ID
        if (currentDeviceId != -1)
        {
            lock (pairingLock)
            {
                // Check if someone else claims ownership of our device
                if (deviceOwnership.TryGetValue(currentDeviceId, out PlayerController owner))
                {
                    if (owner != this && owner != null)
                    {
                        // Conflict detected! Check who claimed it first
                        float ourClaimTime = deviceClaimTime.ContainsKey(currentDeviceId) ? deviceClaimTime[currentDeviceId] : float.MaxValue;

                        // Find if the other owner still exists and has a claim time
                        bool ownerStillValid = false;
                        float ownerClaimTime = float.MaxValue;

                        foreach (var kvp in deviceClaimTime)
                        {
                            if (deviceOwnership.TryGetValue(kvp.Key, out PlayerController pc) && pc == owner)
                            {
                                ownerStillValid = true;
                                ownerClaimTime = kvp.Value;
                                break;
                            }
                        }

                        // If the other owner claimed it first, we need to give it up
                        if (ownerStillValid && ownerClaimTime < ourClaimTime)
                        {
                            Debug.LogWarning($"Player {gameObject.name} detected conflict - another controller owns device {currentDeviceId}. Releasing and finding new device.");

                            // Release this device
                            consumedDeviceIDs.Remove(currentDeviceId);
                            deviceClaimTime.Remove(currentDeviceId);
                            currentDeviceId = -1;

                            if (inputUser.valid)
                            {
                                inputUser.UnpairDevices();
                            }

                            // Try to pair a new device
                            PairNextAvailableDevice();
                            return;
                        }
                        else
                        {
                            // We claimed it first or the other owner is invalid, update ownership to us
                            deviceOwnership[currentDeviceId] = this;
                        }
                    }
                }
                else
                {
                    // No one owns this device, claim it
                    deviceOwnership[currentDeviceId] = this;
                    if (!deviceClaimTime.ContainsKey(currentDeviceId))
                    {
                        deviceClaimTime[currentDeviceId] = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        // RUNTIME CHECK: Verify we actually have the device paired
        if (inputUser.valid && inputUser.pairedDevices.Count > 0)
        {
            lock (pairingLock)
            {
                foreach (var device in inputUser.pairedDevices)
                {
                    // Check if this device belongs to someone else
                    if (deviceOwnership.TryGetValue(device.deviceId, out PlayerController owner))
                    {
                        if (owner != this && owner != null)
                        {
                            Debug.LogWarning($"Player {gameObject.name} has controller {device.deviceId} paired but it belongs to {owner.gameObject.name}. Unpairing.");
                            inputUser.UnpairDevices();
                            if (currentDeviceId != -1 && currentDeviceId != device.deviceId)
                            {
                                consumedDeviceIDs.Remove(currentDeviceId);
                                deviceClaimTime.Remove(currentDeviceId);
                                currentDeviceId = -1;
                            }
                            break;
                        }
                    }

                    // If this device isn't our expected device, unpair
                    if (device.deviceId != currentDeviceId)
                    {
                        Debug.LogWarning($"Player {gameObject.name} has wrong controller {device.deviceId}, expected {currentDeviceId}. Unpairing all.");
                        inputUser.UnpairDevices();
                        break;
                    }
                }
            }
        }

        // RUNTIME CHECK: Verify our current device is still actually paired to us
        bool currentDeviceStillPaired = false;
        if (currentDeviceId != -1 && inputUser.valid)
        {
            foreach (var device in inputUser.pairedDevices)
            {
                if (device.deviceId == currentDeviceId)
                {
                    currentDeviceStillPaired = true;
                    break;
                }
            }
        }

        // If our device isn't actually paired, clear it
        if (!currentDeviceStillPaired && currentDeviceId != -1)
        {
            lock (pairingLock)
            {
                consumedDeviceIDs.Remove(currentDeviceId);
                deviceOwnership.Remove(currentDeviceId);
                deviceClaimTime.Remove(currentDeviceId);
                currentDeviceId = -1;
            }
        }

        // RUNTIME CHECK: Verify device still exists in the system
        if (currentDeviceId != -1)
        {
            bool deviceStillExists = false;
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad.deviceId == currentDeviceId)
                {
                    deviceStillExists = true;
                    break;
                }
            }

            if (!deviceStillExists)
            {
                Debug.LogWarning($"Player {gameObject.name} device {currentDeviceId} no longer exists. Releasing.");
                lock (pairingLock)
                {
                    consumedDeviceIDs.Remove(currentDeviceId);
                    deviceOwnership.Remove(currentDeviceId);
                    deviceClaimTime.Remove(currentDeviceId);
                    currentDeviceId = -1;

                    if (inputUser.valid)
                    {
                        inputUser.UnpairDevices();
                    }
                }
            }
        }

        // Check if we need to pair a device
        if (!inputUser.valid || inputUser.pairedDevices.Count == 0)
        {
            PairNextAvailableDevice();
        }
    }

    void PairNextAvailableDevice()
    {
        lock (pairingLock)
        {
            // Clean up any stale ownership entries
            List<int> staleDevices = new List<int>();
            foreach (var kvp in deviceOwnership)
            {
                if (kvp.Value == null || kvp.Value == this)
                {
                    staleDevices.Add(kvp.Key);
                }
            }
            foreach (int deviceId in staleDevices)
            {
                if (deviceOwnership[deviceId] == null)
                {
                    deviceOwnership.Remove(deviceId);
                    deviceClaimTime.Remove(deviceId);
                }
            }

            // Get available gamepads - must not be in consumedDeviceIDs and not owned by anyone else
            var availableGamepads = Gamepad.all
                .Where(g => !consumedDeviceIDs.Contains(g.deviceId) ||
                           (deviceOwnership.TryGetValue(g.deviceId, out PlayerController owner) &&
                            (owner == null || owner == this)))
                .ToList();

            if (availableGamepads.Count == 0)
            {
                return;
            }

            Gamepad targetGamepad = availableGamepads[0];

            // Unpair existing devices
            if (inputUser.valid && inputUser.pairedDevices.Count > 0)
            {
                inputUser.UnpairDevices();
            }

            // Remove old device ID if we had one
            if (currentDeviceId != -1)
            {
                consumedDeviceIDs.Remove(currentDeviceId);
                deviceOwnership.Remove(currentDeviceId);
                deviceClaimTime.Remove(currentDeviceId);
                currentDeviceId = -1;
            }

            // Reserve this device ID immediately BEFORE pairing
            int deviceIdToPair = targetGamepad.deviceId;
            consumedDeviceIDs.Add(deviceIdToPair);
            deviceOwnership[deviceIdToPair] = this;
            deviceClaimTime[deviceIdToPair] = Time.realtimeSinceStartup;

            // Create user if needed
            if (!inputUser.valid)
            {
                inputUser = InputUser.CreateUserWithoutPairedDevices();
            }

            // Perform the pairing
            InputUser.PerformPairingWithDevice(
                targetGamepad,
                inputUser,
                InputUserPairingOptions.UnpairCurrentDevicesFromUser
            );

            // Verify the pairing succeeded
            bool pairingSucceeded = false;
            if (inputUser.valid)
            {
                foreach (var device in inputUser.pairedDevices)
                {
                    if (device.deviceId == deviceIdToPair)
                    {
                        pairingSucceeded = true;
                        break;
                    }
                }
            }

            if (pairingSucceeded)
            {
                currentDeviceId = deviceIdToPair;
                Debug.Log($"Paired controller {currentDeviceId} to player {gameObject.name}");
            }
            else
            {
                // Pairing failed, remove from consumed list
                consumedDeviceIDs.Remove(deviceIdToPair);
                deviceOwnership.Remove(deviceIdToPair);
                deviceClaimTime.Remove(deviceIdToPair);
                Debug.LogWarning($"Failed to pair controller {deviceIdToPair} to player {gameObject.name}");
            }
        }
    }

    public void SetTargetController(PlayerBehaviour playerBehaviour)
    {
        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.isLocalPlayer = true;

        if (inputUser.valid)
        {
            inputUser.AssociateActionsWithUser(inputs);
            inputs.SquareController.Enable();
        }

        if (!inputUser.valid || inputUser.pairedDevices.Count == 0)
        {
            PairNextAvailableDevice();
        }
    }

    public Vector2 upInputDirection;
    public Vector2 downInputDirection;
    public Vector2 leftInputDirection;
    public Vector2 rightInputDirection;

    delegate void InputAction();

    public Vector2 projectileDirection = Vector2.up;
    public Vector2 finalDirection = Vector2.up;
    public Vector2 aimingDirection = Vector2.up;
    public Vector2 aimingDirectionSimple;

    List<InputAction> cancelInputs;

    public bool inputJump = false;

    public bool shootPrimary = false;
    public bool shootSecondary = false;

    public static float uiRegs = 0;

    public static bool showCursor = true;

    float regs;

    void AfterActionsRegistered()
    {
        inputUser = InputUser.CreateUserWithoutPairedDevices();
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);

        inputs = new Inputs();

        cancelInputs = new List<InputAction>()
        {
            () => { upInputDirection = new Vector2(0, 0); },
            () => { downInputDirection = new Vector2(0, 0); },
            () => { leftInputDirection = new Vector2(0, 0); },
            () => { rightInputDirection = new Vector2(0, 0); },
            () => { aimingDirection = new Vector2(0, 0); },
            () => { shootPrimary = false; },
            () => { shootSecondary = false; }
        };

        inputs.SquareController.Up.performed += (context) =>
        { InputHandler(ref context, () => { upInputDirection = new Vector2(0, 1f); }); };
        inputs.SquareController.Up.canceled += (context) =>
        { InputHandler(ref context, () => { upInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Down.performed += (context) =>
        { InputHandler(ref context, () => { downInputDirection = new Vector2(0, -1f); }); };
        inputs.SquareController.Down.canceled += (context) =>
        { InputHandler(ref context, () => { downInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Left.performed += (context) =>
        { InputHandler(ref context, () => { leftInputDirection = new Vector2(-1, 0); }); };
        inputs.SquareController.Left.canceled += (context) =>
        { InputHandler(ref context, () => { leftInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Right.performed += (context) =>
        { InputHandler(ref context, () => { rightInputDirection = new Vector2(1, 0); }); };
        inputs.SquareController.Right.canceled += (context) =>
        { InputHandler(ref context, () => { rightInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Jump.performed += (context) =>
        { InputHandler(ref context, () => { if (playerBehaviour.hasJump) { inputJump = true; playerBehaviour.hasJump = false; } }); };

        inputs.SquareController.PrimaryConst.performed += (context) =>
        { InputHandler(ref context, () => { shootPrimary = true; }); };
        inputs.SquareController.SecondaryConst.performed += (context) =>
        { InputHandler(ref context, () => { shootSecondary = true; }); };

        inputs.SquareController.PrimaryConst.canceled += (context) =>
        { InputHandler(ref context, () => { shootPrimary = false; }); };
        inputs.SquareController.SecondaryConst.canceled += (context) =>
        { InputHandler(ref context, () => { shootSecondary = false; }); };

        inputs.SquareController.Primary.performed += (context) =>
        { InputHandler(ref context, () => { if (uiRegs != 0) return; shootPrimary = true; }); };
        inputs.SquareController.Secondary.performed += (context) =>
        { InputHandler(ref context, () => { if (uiRegs != 0) return; shootSecondary = true; }); };

        inputs.SquareController.Primary.canceled += (context) =>
        { InputHandler(ref context, () => { shootPrimary = false; }); };
        inputs.SquareController.Secondary.canceled += (context) =>
        { InputHandler(ref context, () => { shootSecondary = false; }); };
        AfterActionsRegistered();
    }

    void InputHandler(ref CallbackContext context, InputAction action)
    {
        InactivityBehaviour.inactivityTimer = InactivityBehaviour.MAX;
        if (!playerBehaviour) return;
        if (playerBehaviour.isDead) CancellAllInputs();
        else action();
        SetFinalInputDirection();
    }

    public void CancellAllInputs() { foreach (InputAction action in cancelInputs) action(); }
    private void Update() => regs = uiRegs;

    void SetFinalInputDirection()
    {
        if (controllerTarget == null) return;

        finalDirection =
            Vector2.Lerp(upInputDirection * 0.4f, upInputDirection, Mods.at[9]) +
            Vector2.Lerp(downInputDirection * 0.3f, downInputDirection, Mods.at[9]) +
            leftInputDirection +
            rightInputDirection;

        aimingDirection =
            upInputDirection +
            downInputDirection +
            leftInputDirection +
            rightInputDirection;

        if (!((downInputDirection + upInputDirection) == Vector2.zero
            && (leftInputDirection + rightInputDirection) == Vector2.zero))
        {
            projectileDirection =
                upInputDirection +
                downInputDirection +
                leftInputDirection +
                rightInputDirection;
        }
    }

    public Vector2 GetDirection() => aimingDirection != Vector2.zero ? aimingDirection : finalDirection;
    public void EnableController() => inputs.SquareController.Enable();
    public void DisableController() => inputs.SquareController.Disable();
}