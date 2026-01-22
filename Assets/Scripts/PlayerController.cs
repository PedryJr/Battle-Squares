using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using static UnityEngine.InputSystem.InputAction;

public sealed partial class PlayerController : MonoBehaviour
{
    [SerializeField]
    public List<int> displayConsumedIDS;

    Inputs inputs;
    InputUser inputUser;
    private int currentDeviceId = -1;

    private bool needsRepairing = false;
    private float lastPairingAttempt = 0f;
    private const float PAIRING_COOLDOWN = 0.1f;

    private static Dictionary<int, PlayerController> deviceOwnership = new Dictionary<int, PlayerController>();
    public static HashSet<int> consumedDeviceIDs = new HashSet<int>();
    [SerializeField] private int displayCurrentDevice = -1;

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        ReleaseCurrentDevice();
    }

    private void FixedUpdate()
    {
        ValidateAndMaintainBinding();
    }

    private void LateUpdate()
    {
        if (!playerBehaviour) Destroy(gameObject);
        displayCurrentDevice = currentDeviceId;
        displayConsumedIDS = consumedDeviceIDs.ToList();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not Gamepad gamepad) return;

        switch (change)
        {
            case InputDeviceChange.Added:
                if (currentDeviceId == -1 || needsRepairing)
                {
                    needsRepairing = true;
                }
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                if (gamepad.deviceId == currentDeviceId)
                {
                    Debug.LogWarning($"Player {gameObject.name} lost device {currentDeviceId}");
                    ReleaseCurrentDevice();
                    needsRepairing = true;
                }
                break;

            case InputDeviceChange.Reconnected:
                if (currentDeviceId == -1 && needsRepairing)
                {
                    needsRepairing = true;
                }
                break;
        }
    }

    void ValidateAndMaintainBinding()
    {
        if (currentDeviceId != -1)
        {
            bool deviceExists = Gamepad.all.Any(g => g.deviceId == currentDeviceId);

            if (!deviceExists)
            {
                Debug.LogWarning($"Player {gameObject.name} device {currentDeviceId} no longer exists");
                ReleaseCurrentDevice();
                needsRepairing = true;
            }
        }

        if (currentDeviceId != -1)
        {
            if (deviceOwnership.TryGetValue(currentDeviceId, out PlayerController owner))
            {
                if (owner != this)
                {
                    Debug.LogError($"Player {gameObject.name} device {currentDeviceId} is owned by {owner?.gameObject.name}. Releasing.");
                    ReleaseCurrentDevice();
                    needsRepairing = true;
                }
            }
            else
            {
                deviceOwnership[currentDeviceId] = this;
                if (!consumedDeviceIDs.Contains(currentDeviceId))
                {
                    consumedDeviceIDs.Add(currentDeviceId);
                }
            }
        }

        if (currentDeviceId != -1 && inputUser.valid)
        {
            bool correctlyPaired = false;
            bool hasWrongDevice = false;

            foreach (var device in inputUser.pairedDevices)
            {
                if (device.deviceId == currentDeviceId)
                {
                    correctlyPaired = true;
                }
                else
                {
                    hasWrongDevice = true;
                    Debug.LogWarning($"Player {gameObject.name} has wrong device {device.deviceId} paired");
                }
            }

            if (hasWrongDevice || !correctlyPaired)
            {
                if (inputUser.valid)
                {
                    inputUser.UnpairDevices();
                }
                needsRepairing = true;
            }
        }

        List<int> staleDevices = new List<int>();
        foreach (var kvp in deviceOwnership)
        {
            if (kvp.Value == null)
            {
                staleDevices.Add(kvp.Key);
            }
        }
        foreach (int deviceId in staleDevices)
        {
            deviceOwnership.Remove(deviceId);
            consumedDeviceIDs.Remove(deviceId);
            Debug.Log($"Cleaned up stale device {deviceId}");
        }

        if ((currentDeviceId == -1 || needsRepairing) &&
            Time.realtimeSinceStartup - lastPairingAttempt > PAIRING_COOLDOWN)
        {
            lastPairingAttempt = Time.realtimeSinceStartup;
            AttemptPairing();
        }
    }

    void AttemptPairing()
    {
        var allGamepads = Gamepad.all.ToList();

        if (allGamepads.Count == 0)
        {
            Debug.Log($"Player {gameObject.name} waiting for gamepad...");
            return;
        }

        Gamepad targetGamepad = null;

        foreach (var gamepad in allGamepads)
        {
            int deviceId = gamepad.deviceId;

            if (!consumedDeviceIDs.Contains(deviceId))
            {
                targetGamepad = gamepad;
                break;
            }
            else if (currentDeviceId == -1 &&
                     deviceOwnership.TryGetValue(deviceId, out PlayerController owner) &&
                     owner == this)
            {
                targetGamepad = gamepad;
                break;
            }
        }

        if (targetGamepad == null)
        {
            Debug.LogWarning($"Player {gameObject.name} found no available gamepads. Total: {allGamepads.Count}, Consumed: {consumedDeviceIDs.Count}");
            return;
        }

        int deviceIdToPair = targetGamepad.deviceId;

        if (deviceOwnership.TryGetValue(deviceIdToPair, out PlayerController currentOwner))
        {
            if (currentOwner != null && currentOwner != this && currentOwner.currentDeviceId == deviceIdToPair)
            {
                Debug.LogWarning($"Player {gameObject.name} cannot claim device {deviceIdToPair} - actively used by {currentOwner.gameObject.name}");
                return;
            }
        }

        if (currentDeviceId != -1 && currentDeviceId != deviceIdToPair)
        {
            ReleaseCurrentDevice();
        }

        if (inputUser.valid && inputUser.pairedDevices.Count > 0)
        {
            inputUser.UnpairDevices();
        }

        consumedDeviceIDs.Add(deviceIdToPair);
        deviceOwnership[deviceIdToPair] = this;

        if (!inputUser.valid)
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
            inputUser.AssociateActionsWithUser(inputs);
        }

        try
        {
            InputUser.PerformPairingWithDevice(
                targetGamepad,
                inputUser,
                InputUserPairingOptions.UnpairCurrentDevicesFromUser
            );

            bool pairingSucceeded = inputUser.valid && inputUser.pairedDevices.Any(d => d.deviceId == deviceIdToPair);

            if (pairingSucceeded)
            {
                currentDeviceId = deviceIdToPair;
                needsRepairing = false;

                inputs.SquareController.Enable();
            }
            else
            {
                consumedDeviceIDs.Remove(deviceIdToPair);
                deviceOwnership.Remove(deviceIdToPair);
                Debug.LogError($"Failed to pair device {deviceIdToPair} to player {gameObject.name}");
            }
        }
        catch (System.Exception e)
        {
            consumedDeviceIDs.Remove(deviceIdToPair);
            deviceOwnership.Remove(deviceIdToPair);
            Debug.LogError($"Exception pairing device {deviceIdToPair}: {e.Message}");
        }
    }

    void ReleaseCurrentDevice()
    {
        if (currentDeviceId != -1)
        {
            consumedDeviceIDs.Remove(currentDeviceId);
            deviceOwnership.Remove(currentDeviceId);
            currentDeviceId = -1;
        }

        if (inputUser.valid) inputUser.UnpairDevices();
    }

    public void SetTargetController(PlayerBehaviour playerBehaviour)
    {
        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.playerController = this;

        if (!inputUser.valid)
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
        }

        inputUser.AssociateActionsWithUser(inputs);

        needsRepairing = true;
        AttemptPairing();
    }

    [ContextMenu("Force Repair")]
    public void ForceRepair()
    {
        needsRepairing = true;
    }

    void AfterActionsRegistered()
    {
        inputUser = InputUser.CreateUserWithoutPairedDevices();
    }

    private void Awake()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        DontDestroyOnLoad(this.gameObject);

        inputs = new Inputs();

        inputs.SquareController.Up.performed += OnUpPerformed;
        inputs.SquareController.Up.canceled += OnUpCanceled;

        inputs.SquareController.Down.performed += OnDownPerformed;
        inputs.SquareController.Down.canceled += OnDownCanceled;

        inputs.SquareController.Left.performed += OnLeftPerformed;
        inputs.SquareController.Left.canceled += OnLeftCanceled;

        inputs.SquareController.Right.performed += OnRightPerformed;
        inputs.SquareController.Right.canceled += OnRightCanceled;

        inputs.SquareController.Jump.performed += OnJumpPerformed;

        inputs.SquareController.PrimaryConst.performed += OnPrimaryConstPerformed;
        inputs.SquareController.PrimaryConst.canceled += OnPrimaryConstCanceled;

        inputs.SquareController.SecondaryConst.performed += OnSecondaryConstPerformed;
        inputs.SquareController.SecondaryConst.canceled += OnSecondaryConstCanceled;

        inputs.SquareController.Primary.performed += OnPrimaryPerformed;
        inputs.SquareController.Primary.canceled += OnPrimaryCanceled;

        inputs.SquareController.Secondary.performed += OnSecondaryPerformed;
        inputs.SquareController.Secondary.canceled += OnSecondaryCanceled;

        inputs.SquareController.ToggleMousePosession.performed += ToggleMousePosession_performed;

        AfterActionsRegistered();
    }

    private void ToggleMousePosession_performed(CallbackContext obj)
    {
        CursorBehaviour cb = FindAnyObjectByType<CursorBehaviour>();
        if (cb) cb.TogglePosessCursor(inputUser, this);
    }
}

public sealed partial class PlayerController
{
    Rigidbody2D controllerTarget;
    public PlayerBehaviour playerBehaviour;

    public Vector2 upInputDirection;
    public Vector2 downInputDirection;
    public Vector2 leftInputDirection;
    public Vector2 rightInputDirection;

    public Vector2 projectileDirection = Vector2.up;
    public Vector2 finalDirection = Vector2.up;
    public Vector2 aimingDirection = Vector2.up;
    public Vector2 aimingDirectionSimple;

    public bool inputJump = false;
    public bool shootPrimary = false;
    public bool shootSecondary = false;

    public static float uiRegs = 0;
    public static bool showCursor = true;

    float regs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnUpPerformed(CallbackContext context)
    {
        if (!ValidateDeadzone(context.ReadValue<float>())) return;
        if (!ValidateInput()) return;
        upInputDirection = new Vector2(0, 1f);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnUpCanceled(CallbackContext context)
    {
        upInputDirection = Vector2.zero;
        SetFinalInputDirection();
    }

    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDownPerformed(CallbackContext context)
    {
        if (!ValidateDeadzone(context.ReadValue<float>())) return;
        if (!ValidateInput()) return;
        downInputDirection = new Vector2(0, -1f);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDownCanceled(CallbackContext context)
    {
        downInputDirection = Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnLeftPerformed(CallbackContext context)
    {
        if (!ValidateDeadzone(context.ReadValue<float>())) return;
        if (!ValidateInput()) return;
        leftInputDirection = new Vector2(-1, 0);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnLeftCanceled(CallbackContext context)
    {
        leftInputDirection = Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnRightPerformed(CallbackContext context)
    {
        if (!ValidateDeadzone(context.ReadValue<float>())) return;
        if (!ValidateInput()) return;
        rightInputDirection = new Vector2(1, 0);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnRightCanceled(CallbackContext context)
    {
        rightInputDirection = Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnJumpPerformed(CallbackContext context)
    {
        if (useCachedInput) return;
        if (!ValidateInput()) return;
        if (playerBehaviour.hasJump)
        {
            inputJump = true;
            playerBehaviour.hasJump = false;
        }
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPrimaryConstPerformed(CallbackContext context)
    {
        if (useCachedInput) return;
        if (!ValidateInput()) return;
        shootPrimary = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPrimaryConstCanceled(CallbackContext context)
    {
        shootPrimary = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSecondaryConstPerformed(CallbackContext context)
    {
        if (useCachedInput) return;
        if (!ValidateInput()) return;
        shootSecondary = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSecondaryConstCanceled(CallbackContext context)
    {
        shootSecondary = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPrimaryPerformed(CallbackContext context)
    {
        if (useCachedInput) return;
        if (!ValidateInput() || uiRegs != 0) return;
        shootPrimary = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnPrimaryCanceled(CallbackContext context)
    {
        shootPrimary = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSecondaryPerformed(CallbackContext context)
    {
        if (useCachedInput) return;
        if (!ValidateInput() || uiRegs != 0) return;
        shootSecondary = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSecondaryCanceled(CallbackContext context)
    {
        shootSecondary = false;
    }

    //Testing a 0.5 deadzone for now (should make corner aiming easier)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ValidateDeadzone(float v)
    {
        return v > 0.5f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ValidateInput()
    {
        InactivityBehaviour.inactivityTimer = InactivityBehaviour.MAX;
        if (!playerBehaviour) return false;
        if (playerBehaviour.isDead)
        {
            CancelAllInputs();
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CancelAllInputs()
    {
        upInputDirection = Vector2.zero;
        downInputDirection = Vector2.zero;
        leftInputDirection = Vector2.zero;
        rightInputDirection = Vector2.zero;
        aimingDirection = Vector2.zero;
        shootPrimary = false;
        shootSecondary = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update() => regs = uiRegs;

    void SetFinalInputDirection()
    {
        if (controllerTarget == null) return;

        Vector2 usedInput = useCachedInput ? 
            cachedInputUp +
            cachedInputDown +
            cachedInputLeft + 
            cachedInputRight :
            upInputDirection +
            downInputDirection +
            leftInputDirection +
            rightInputDirection;

        finalDirection =
            Vector2.Lerp((useCachedInput ? cachedInputUp : upInputDirection) * 0.4f, useCachedInput ? cachedInputUp : upInputDirection, Mods.at[9]) +
            Vector2.Lerp((useCachedInput ? cachedInputDown : downInputDirection) * 0.3f, useCachedInput ? cachedInputDown : downInputDirection, Mods.at[9]) +
                                      (useCachedInput ? cachedInputLeft : leftInputDirection) +
                                      (useCachedInput ? cachedInputRight : rightInputDirection);

        aimingDirection = usedInput;

        if (!((downInputDirection + upInputDirection) == Vector2.zero && (leftInputDirection + rightInputDirection) == Vector2.zero)) projectileDirection = usedInput;
    }

    [SerializeField]
    bool useCachedInput = false;
    Vector2 cachedInputLeft;
    Vector2 cachedInputRight;
    Vector2 cachedInputUp;
    Vector2 cachedInputDown;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetDirection() => aimingDirection != Vector2.zero ? aimingDirection : finalDirection;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnableController()
    {
        useCachedInput = false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableController()
    {
        cachedInputDown = downInputDirection;
        cachedInputLeft = leftInputDirection;
        cachedInputRight = rightInputDirection;
        cachedInputDown = upInputDirection;
        useCachedInput = true;
    }
}