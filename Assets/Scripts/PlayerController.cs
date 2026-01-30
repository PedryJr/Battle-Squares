using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using static UnityEngine.InputSystem.InputAction;

public sealed partial class PlayerController : MonoBehaviour
{
    [SerializeField][Range(0f, 1f)] float deadzoneRadius = 0.5f;
    [SerializeField][Range(0f, 1f)] float cornerBias = 0.3f;

    [SerializeField]
    public List<int> displayConsumedIDS;

    PlayerFactorySynchronizer playerFactory;
    Inputs inputs;
    InputUser inputUser;

    private float lastPairingAttempt = 0f;
    private const float PAIRING_COOLDOWN = 0.1f;

    private static Dictionary<int, PlayerController> deviceOwnership = new Dictionary<int, PlayerController>();
    public static HashSet<int> consumedDeviceIDs = new HashSet<int>();

    private static bool firstPlayerExists = false;
    private bool needsRepairing = false;
    private bool isFirstPlayer = false;

    private int currentDeviceId = -1;
    private int releasedGamepadId = -1;

    [SerializeField] private int displayCurrentDevice = -1;

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange; 
        ReleaseCurrentDevice(); 
        if (isFirstPlayer) firstPlayerExists = false;
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

    private void OnStartPressed(CallbackContext context)
    {
        if (!isFirstPlayer)
        {
            inputs.SquareController.Start.performed -= OnStartPressed;
            return;
        }

        if (context.control.device is Gamepad gamepad)
        {
            int pressedDeviceId = gamepad.deviceId;
            if (currentDeviceId == pressedDeviceId)
            {
                Debug.Log($"First player pressed Start on gamepad {pressedDeviceId} - spawning new player and releasing gamepad");
                releasedGamepadId = currentDeviceId;
                ReleaseCurrentDevice();
                playerFactory.CreateNewPlayerFromFirstController();
                inputs.SquareController.Start.performed -= OnStartPressed;
            }
        }
    }

    void ValidateAndMaintainBinding()
    {
        if (isFirstPlayer) ValidateKeyboardMouse();
        else ValidateGamepad();

        List<int> staleDevices = new List<int>();
        foreach (var kvp in deviceOwnership)
        {
            if (kvp.Value == null) staleDevices.Add(kvp.Key);
        }
        foreach (int deviceId in staleDevices)
        {
            deviceOwnership.Remove(deviceId);
            consumedDeviceIDs.Remove(deviceId);
            Debug.Log($"Cleaned up stale device {deviceId}");
        }

        if ((currentDeviceId == -1 || needsRepairing) && Time.realtimeSinceStartup - lastPairingAttempt > PAIRING_COOLDOWN)
        {
            lastPairingAttempt = Time.realtimeSinceStartup;
            AttemptPairing();
        }
    }

    void ValidateKeyboardMouse()
    { 
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        if (!inputUser.valid || !inputUser.pairedDevices.Any(d => d is Keyboard) || !inputUser.pairedDevices.Any(d => d is Mouse)) needsRepairing = true;

        if (currentDeviceId != -1)
        {
            bool deviceExists = Gamepad.all.Any(g => g.deviceId == currentDeviceId);

            if (!deviceExists)
            {
                currentDeviceId = -1;
                needsRepairing = true;
            }
        }
    }

    void ValidateGamepad()
    {
        if (currentDeviceId != -1)
        {
            bool deviceExists = Gamepad.all.Any(g => g.deviceId == currentDeviceId);

            if (!deviceExists)
            {
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
                if (device.deviceId == currentDeviceId) correctlyPaired = true;
                else if (device is Gamepad) hasWrongDevice = true;
            }

            if (hasWrongDevice || !correctlyPaired)
            {
                if (inputUser.valid) inputUser.UnpairDevices();
                needsRepairing = true;
            }
        }
    }

    void AttemptPairing()
    {
        if (isFirstPlayer) PairFirstPlayer();
        else PairGamepad();
    }

    void PairFirstPlayer()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        if (!inputUser.valid)
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
            inputUser.AssociateActionsWithUser(inputs);
        }

        if (inputUser.pairedDevices.Count > 0) inputUser.UnpairDevices();

        try
        {
            InputUser.PerformPairingWithDevice(keyboard, inputUser);
            InputUser.PerformPairingWithDevice(mouse, inputUser);

            var allGamepads = Gamepad.all.ToList();
            Gamepad targetGamepad = null;

            foreach (var gamepad in allGamepads)
            {
                int deviceId = gamepad.deviceId;
                if (deviceId == releasedGamepadId) continue;

                if (!consumedDeviceIDs.Contains(deviceId))
                {
                    targetGamepad = gamepad;
                    break;
                }
            }

            if (targetGamepad != null)
            {
                int deviceIdToPair = targetGamepad.deviceId;
                if (deviceOwnership.TryGetValue(deviceIdToPair, out PlayerController currentOwner))
                {
                    if (currentOwner != null && currentOwner != this && currentOwner.currentDeviceId == deviceIdToPair) targetGamepad = null;
                }

                if (targetGamepad != null)
                {
                    InputUser.PerformPairingWithDevice(targetGamepad, inputUser);

                    bool gamepadPaired = inputUser.pairedDevices.Any(d => d.deviceId == deviceIdToPair);
                    if (gamepadPaired)
                    {
                        currentDeviceId = deviceIdToPair;
                        consumedDeviceIDs.Add(deviceIdToPair);
                        deviceOwnership[deviceIdToPair] = this;
                    }
                }
            }

            needsRepairing = false;
            inputs.SquareController.Enable();
        }
        catch { }
    }

    void PairGamepad()
    {
        var allGamepads = Gamepad.all.ToList();

        if (allGamepads.Count == 0) return;

        Gamepad targetGamepad = null;

        foreach (var gamepad in allGamepads)
        {
            int deviceId = gamepad.deviceId;

            if (!consumedDeviceIDs.Contains(deviceId))
            {
                targetGamepad = gamepad;
                break;
            }
            else if (currentDeviceId == -1 && deviceOwnership.TryGetValue(deviceId, out PlayerController owner) && owner == this)
            {
                targetGamepad = gamepad;
                break;
            }
        }

        if (targetGamepad == null) return;

        int deviceIdToPair = targetGamepad.deviceId;

        if (deviceOwnership.TryGetValue(deviceIdToPair, out PlayerController currentOwner))
        {
            if (currentOwner != null && currentOwner != this && currentOwner.currentDeviceId == deviceIdToPair) return;
        }

        if (currentDeviceId != -1 && currentDeviceId != deviceIdToPair) ReleaseCurrentDevice();

        if (inputUser.valid && inputUser.pairedDevices.Count > 0) inputUser.UnpairDevices();

        consumedDeviceIDs.Add(deviceIdToPair);
        deviceOwnership[deviceIdToPair] = this;

        if (!inputUser.valid)
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
            inputUser.AssociateActionsWithUser(inputs);
        }

        try
        {
            InputUser.PerformPairingWithDevice(targetGamepad, inputUser, InputUserPairingOptions.UnpairCurrentDevicesFromUser); 
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
            }
        }
        catch (Exception e)
        {
            consumedDeviceIDs.Remove(deviceIdToPair);
            deviceOwnership.Remove(deviceIdToPair); 
        }
    }

    void ReleaseCurrentDevice()
    { 
        if (isFirstPlayer)
        {
            if (currentDeviceId != -1)
            {
                consumedDeviceIDs.Remove(currentDeviceId);
                deviceOwnership.Remove(currentDeviceId); 
                if (releasedGamepadId == -1) releasedGamepadId = currentDeviceId; 
                currentDeviceId = -1; 
                if (inputUser.valid)
                {
                    inputUser.UnpairDevices();
                    needsRepairing = true;
                }
            }
        }
        else
        { 
            if (currentDeviceId != -1)
            {
                consumedDeviceIDs.Remove(currentDeviceId);
                deviceOwnership.Remove(currentDeviceId);
                currentDeviceId = -1;
            } 
            if (inputUser.valid) inputUser.UnpairDevices();
        }
    }

    //Factory calls this function to bind a local player that just spawned to a new instance of "PlayerController"
    //Kindof acts like an initializer.
    public void SetTargetController(PlayerBehaviour playerBehaviour)
    {
        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.playerController = this; 
        if (!firstPlayerExists)
        {
            isFirstPlayer = true;
            firstPlayerExists = true; 
        }

        if (!inputUser.valid) inputUser = InputUser.CreateUserWithoutPairedDevices(); 
        inputUser.AssociateActionsWithUser(inputs); 
        needsRepairing = true;
        AttemptPairing();
    }
     

    private void Awake()
    {
        playerFactory = FindAnyObjectByType<PlayerFactorySynchronizer>();
        InputSystem.onDeviceChange += OnDeviceChange;
        DontDestroyOnLoad(this.gameObject);

        inputs = new Inputs();

        inputs.SquareController.Up.performed += HandleUp;
        inputs.SquareController.Up.canceled += HandleUp;
        inputs.SquareController.Down.performed += HandleDown;
        inputs.SquareController.Down.canceled += HandleDown;
        inputs.SquareController.Left.performed += HandleLeft;
        inputs.SquareController.Left.canceled += HandleLeft;
        inputs.SquareController.Right.performed += HandleRight;
        inputs.SquareController.Right.canceled += HandleRight;

        inputs.SquareController.Jump.performed += OnJumpPerformed;

        inputs.SquareController.PrimaryConst.performed += OnPrimaryConstPerformed;
        inputs.SquareController.PrimaryConst.canceled += OnPrimaryConstCanceled; 
        inputs.SquareController.SecondaryConst.performed += OnSecondaryConstPerformed;
        inputs.SquareController.SecondaryConst.canceled += OnSecondaryConstCanceled; 
        inputs.SquareController.Primary.performed += OnPrimaryPerformed;
        inputs.SquareController.Primary.canceled += OnPrimaryCanceled; 
        inputs.SquareController.Secondary.performed += OnSecondaryPerformed;
        inputs.SquareController.Secondary.canceled += OnSecondaryCanceled;
         
        inputs.SquareController.Start.performed += OnStartPressed;
        inputUser = InputUser.CreateUserWithoutPairedDevices();

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        if (arg1.name == "LobbyScene") EnableController();
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
    private void HandleUp(CallbackContext context)
    {
        if (!ValidateInput()) return;
        upInputDirection = new Vector2(0, context.ReadValue<float>());
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleDown(CallbackContext context)
    {
        if (!ValidateInput()) return;
        downInputDirection = new Vector2(0, -context.ReadValue<float>());
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleLeft(CallbackContext context)
    {
        if (!ValidateInput()) return;
        leftInputDirection = new Vector2(-context.ReadValue<float>(), 0);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleRight(CallbackContext context)
    {
        if (!ValidateInput()) return;
        rightInputDirection = new Vector2(context.ReadValue<float>(), 0);
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnJumpPerformed(CallbackContext context)
    {
        if (zeroInput) return;
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
        if (zeroInput) return;
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
        if (zeroInput) return;
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
        if (zeroInput) return;
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
        if (zeroInput) return;
        if (!ValidateInput() || uiRegs != 0) return;
        shootSecondary = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSecondaryCanceled(CallbackContext context)
    {
        shootSecondary = false;
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

        float inputMask = zeroInput ? 0f : 1f;
        Vector2 up = upInputDirection * inputMask;
        Vector2 down = downInputDirection * inputMask;
        Vector2 left = leftInputDirection * inputMask;
        Vector2 right = rightInputDirection * inputMask;

        float mod = Mods.at[9];
        float upScale = Mathf.Lerp(0.4f, 1f, mod);
        float downScale = Mathf.Lerp(0.3f, 1f, mod);
        playerBehaviour.moveDirection = up * upScale + down * downScale + left + right;

        Vector2 accumInput = up + down + left + right;
        float sqrMagnitude = accumInput.sqrMagnitude;
        float deadzoneSqr = deadzoneRadius * deadzoneRadius;

        if (sqrMagnitude <= deadzoneSqr)
        {
            playerBehaviour.aimDirection = Vector2.zero;
            return;
        }

        const float SQRT_HALF = 0.70710678f;
        const float ONE_OVER_PI_OVER_4 = 4f / Mathf.PI;

        float magnitude = Mathf.Sqrt(sqrMagnitude);
        Vector2 normalized = accumInput / magnitude;

        float angle = Mathf.Atan2(normalized.y, normalized.x);
        if (angle < 0f) angle += Mathf.PI * 2f;

        float cornerBiasScaler = Mathf.Lerp(0.6f, 1.4f, cornerBias);
        float straightHalfWidth = (Mathf.PI / 8f) * cornerBiasScaler;
        float diagonalHalfWidth = (Mathf.PI / 8f) * (2f - cornerBiasScaler);

        float sector = angle * ONE_OVER_PI_OVER_4;
        int sectorIndex = (int)sector;
        float sectorFrac = sector - sectorIndex;

        int chosenIndex = -1;

        if (sectorFrac <= 0.5f)
        {
            float halfWidth = (sectorIndex % 2 == 0) ? straightHalfWidth : diagonalHalfWidth;
            float centerAngle = sectorIndex * (Mathf.PI / 4f);
            float delta = angle - centerAngle;
            if (delta < 0) delta = -delta;

            if (delta <= halfWidth)
            {
                chosenIndex = sectorIndex;
            }
            else if (sectorIndex > 0)
            {
                halfWidth = ((sectorIndex - 1) % 2 == 0) ? straightHalfWidth : diagonalHalfWidth;
                centerAngle = (sectorIndex - 1) * (Mathf.PI / 4f);
                delta = angle - centerAngle;
                if (delta < 0) delta = -delta;
                if (delta <= halfWidth) chosenIndex = sectorIndex - 1;
            }
        }
        else
        {
            int nextIndex = (sectorIndex + 1) % 8;
            float halfWidth = (nextIndex % 2 == 0) ? straightHalfWidth : diagonalHalfWidth;
            float centerAngle = nextIndex * (Mathf.PI / 4f);
            float delta = angle - centerAngle;
            if (delta < 0) delta = -delta;

            if (delta <= halfWidth)
            {
                chosenIndex = nextIndex;
            }
            else
            {
                halfWidth = (sectorIndex % 2 == 0) ? straightHalfWidth : diagonalHalfWidth;
                centerAngle = sectorIndex * (Mathf.PI / 4f);
                delta = angle - centerAngle;
                if (delta < 0) delta = -delta;
                if (delta <= halfWidth) chosenIndex = sectorIndex;
            }
        }

        float scaledMagnitude = (magnitude - deadzoneRadius) / (1f - deadzoneRadius);
        switch (chosenIndex)
        {
            case 0: playerBehaviour.aimDirection = new Vector2(scaledMagnitude, 0f); break;
            case 1: playerBehaviour.aimDirection = new Vector2(SQRT_HALF, SQRT_HALF) * scaledMagnitude; break;
            case 2: playerBehaviour.aimDirection = new Vector2(0f, scaledMagnitude); break;
            case 3: playerBehaviour.aimDirection = new Vector2(-SQRT_HALF, SQRT_HALF) * scaledMagnitude; break;
            case 4: playerBehaviour.aimDirection = new Vector2(-scaledMagnitude, 0f); break;
            case 5: playerBehaviour.aimDirection = new Vector2(-SQRT_HALF, -SQRT_HALF) * scaledMagnitude; break;
            case 6: playerBehaviour.aimDirection = new Vector2(0f, -scaledMagnitude); break;
            case 7: playerBehaviour.aimDirection = new Vector2(SQRT_HALF, -SQRT_HALF) * scaledMagnitude; break;
            default: playerBehaviour.aimDirection = Vector2.zero; break;
        }
    }



    [SerializeField]
    bool zeroInput = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnableController()
    {
        zeroInput = false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableController()
    {
        zeroInput = true;
    }
}