using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
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
    PlayerSynchronizer playerSynchronizer;
    [SerializeField]
    private bool isAI = false;

    public PlayerController SetTargetController(PlayerBehaviour playerBehaviour)
    {
        
        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.playerController = this;
        zeroInput = false;
        if (playerBehaviour.isAI)
        {
            isAI = true;

            inputs.Disable();
            inputs.Dispose();
            inputs = new Inputs();
            mlAgent = gameObject.GetComponent<PlayerMLAgent>();
            mlAgent.playerController = this;
            playerBehaviour.playerMLAgent = mlAgent; ;
            mlAgent.enabled = true;
            mlAgent.InitializeExtern();
            return null;
        }
        Destroy(GetComponent<PlayerMLAgent>());
        return this;
    }

    public void PairDevice(InputDevice device)
    {
        if (device == null) return;

        List<InputDevice> devices = new List<InputDevice>(internalAssignedDevices);
        devices.Add(device);
        internalAssignedDevices = devices.ToArray();

        UpdateDeviceList();
    }

    public void UnpairDevice(InputDevice device)
    {
        if (device == null) return;

        List<InputDevice> devices = new List<InputDevice>(internalAssignedDevices);
        devices.Remove(device);
        internalAssignedDevices = devices.ToArray();

        UpdateDeviceList();

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


    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
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

        inputs.Enable();
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

    float aiUpdateTimer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        regs = uiRegs;
        if (isAI)
        {
            aiUpdateTimer += Time.deltaTime * AI_UPDATE_RATE;
            if(aiUpdateTimer > 1)
            {
                AIUpdate();
            }
        }

    }

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
        playerBehaviour.aimDirection = (up + down + left + right).normalized;
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

partial class PlayerController
{
    const float AI_UPDATE_RATE = 10f;

    private PlayerMLAgent mlAgent;

    void AIUpdate()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleUp(bool on)
    {
        upInputDirection = on ? Vector2.up : Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleDown(bool on)
    {
        downInputDirection = on ? Vector2.down : Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleLeft(bool on)
    {
        leftInputDirection = on ? Vector2.left : Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleRight(bool on)
    {
        rightInputDirection = on ? Vector2.right : Vector2.zero;
        SetFinalInputDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool OnJumpPerformed(bool on)
    {
        if (!on) return false;
        if (playerBehaviour.hasJump)
        {
            inputJump = true;
            playerBehaviour.hasJump = false;
            SetFinalInputDirection();
            return true;
        }
        SetFinalInputDirection();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPrimaryPerformed(bool on) => shootPrimary = on;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnSecondaryPerformed(bool on) => shootSecondary = on;
}