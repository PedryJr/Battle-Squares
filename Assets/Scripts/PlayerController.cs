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
    
    Inputs inputs;
    InputUser user;

    InputDevice[] internalAssignedDevices;
    void UpdateDeviceList() => inputs.devices = internalAssignedDevices;

    private List<int> devicesInUse;

    public PlayerController SetTargetController(PlayerBehaviour playerBehaviour)
    {
        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.playerController = this;
        return this;
    }

    public void PairDevice(InputDevice device)
    {
        if (device == null) return;

        List<InputDevice> devices = new List<InputDevice>(internalAssignedDevices);
        devices.Add(device);
        internalAssignedDevices = devices.ToArray();

        UpdateDeviceList();

        /*        inputs.Disable();
                user = InputUser.PerformPairingWithDevice(device, user, InputUserPairingOptions.None);
                user.AssociateActionsWithUser(inputs);
                inputs.Enable();

                if (!devicesInUse.Contains(device.deviceId)) devicesInUse.Add(device.deviceId);*/
    }

    public void UnpairDevice(InputDevice device)
    {
        if (device == null) return;

        List<InputDevice> devices = new List<InputDevice>(internalAssignedDevices);
        devices.Remove(device);
        internalAssignedDevices = devices.ToArray();

        UpdateDeviceList();
/*        inputs.Disable();

        List<InputDevice> alreadyPairedDevices = new List<InputDevice>();
        foreach (InputDevice alreadyPaired in InputSystem.devices) if(devicesInUse.Contains(alreadyPaired.deviceId)) alreadyPairedDevices.Add(alreadyPaired);
        devicesInUse.Clear();
        
        user = InputUser.CreateUserWithoutPairedDevices();

        foreach (var item in alreadyPairedDevices)
        {
            if (item.deviceId == device.deviceId) continue;
            user = InputUser.PerformPairingWithDevice(item, user, InputUserPairingOptions.None);
            if (!devicesInUse.Contains(item.deviceId)) devicesInUse.Add(item.deviceId);
        }
        user.AssociateActionsWithUser(inputs);
        inputs.Enable();*/

    }

    public bool IsUsingDevice(int deviceId)
    {
        return devicesInUse.Contains(deviceId);
    }

    public bool IsUsingDevice(InputDevice device)
    {
        return device != null && devicesInUse.Contains(device.deviceId);
    }

    public List<int> GetPairedDeviceIds()
    {
        return new List<int>(devicesInUse);
    }

    public bool HasAnyDevices()
    {
        return devicesInUse.Count > 0;
    }

    public void ClearAllDevices()
    {

        inputs.Disable();

        devicesInUse.Clear();
        user = InputUser.CreateUserWithoutPairedDevices();
        user.AssociateActionsWithUser(inputs);

        inputs.Enable();
    }

    public void AssociateUserWithDevices(InputDevice[] userDevices)
    {
        inputs.Disable();
        user = InputUser.CreateUserWithoutPairedDevices();
        devicesInUse.Clear();

        foreach (InputDevice device in userDevices)
        {
            user = InputUser.PerformPairingWithDevice(device, user, InputUserPairingOptions.None);
            devicesInUse.Add(device.deviceId);
        }

        user.AssociateActionsWithUser(inputs);
        inputs.Enable();
    }


    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        controllerManager = FindAnyObjectByType<PlayerControllerManager>();
        devicesInUse = new List<int>();
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

        internalAssignedDevices = new InputDevice[0];
/*
        user = InputUser.CreateUserWithoutPairedDevices();
        user.AssociateActionsWithUser(inputs);*/

        inputs.Enable();

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        if (arg1.name == "LobbyScene") EnableController();
    }

    private void OnDestroy()
    {
        controllerManager.DespawnController(this);
    }
}

public sealed partial class PlayerController
{
    PlayerControllerManager controllerManager;
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

        float mod = Mods.NormalizeMovement;
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