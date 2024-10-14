using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public sealed class PlayerController : MonoBehaviour
{

    Rigidbody2D controllerTarget;
    public PlayerBehaviour playerBehaviour;

    Inputs inputs;

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
    [BurstCompile]
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

        inputs.SquareController.Up.performed
            += (context) => { InputHandler(() => { upInputDirection = new Vector2(0, 1f); }); };
        inputs.SquareController.Up.canceled
            += (context) => { InputHandler(() => { upInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Down.performed
            += (context) => { InputHandler(() => { downInputDirection = new Vector2(0, -1f); }); };
        inputs.SquareController.Down.canceled
            += (context) => { InputHandler(() => { downInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Left.performed
            += (context) => { InputHandler(() => { leftInputDirection = new Vector2(-1, 0); }); };
        inputs.SquareController.Left.canceled
            += (context) => { InputHandler(() => { leftInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Right.performed
            += (context) => { InputHandler(() => { rightInputDirection = new Vector2(1, 0); }); };
        inputs.SquareController.Right.canceled
            += (context) => { InputHandler(() => { rightInputDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Jump.performed
            += (context) => { InputHandler(() => { if (playerBehaviour.hasJump) { inputJump = true; playerBehaviour.hasJump = false; } }); };

        inputs.SquareController.PrimaryConst.performed += (context) =>
        {
            InputHandler(() => {
                shootPrimary = true;
            });
        };
        inputs.SquareController.SecondaryConst.performed += (context) =>
        {
            InputHandler(() => {
                shootSecondary = true;
            });
        };

        inputs.SquareController.PrimaryConst.canceled += (context) =>
        {
            InputHandler(() => {
                shootPrimary = false;
            });
        };
        inputs.SquareController.SecondaryConst.canceled += (context) =>
        {
            InputHandler(() => {
                shootSecondary = false;
            });
        };

        inputs.SquareController.Primary.performed += (context) =>
        {
            InputHandler(() => {
                if (uiRegs != 0) return;
            shootPrimary = true;
        });
        };
        inputs.SquareController.Secondary.performed += (context) =>
        {
            InputHandler(() => {
                if (uiRegs != 0) return;
            shootSecondary = true;
        });
        };

        inputs.SquareController.Primary.canceled += (context) =>
        {
            InputHandler(() => {
                shootPrimary = false;
        });
        };
        inputs.SquareController.Secondary.canceled += (context) =>
        {
            InputHandler(() => {
                shootSecondary = false;
        });
        };

        inputs.SquareController.Enable();

    }

    [BurstCompile]
    void InputHandler(InputAction action)
    {

        if (!playerBehaviour) return;

        if (playerBehaviour.isDead)
        {
            CancellAllInputs();
        }
        else
        {
            action();
        }

        SetFinalInputDirection();

    }
    [BurstCompile]
    public void CancellAllInputs()
    {

        foreach (InputAction action in cancelInputs) action();

    }
    [BurstCompile]
    private void Update()
    {

        regs = uiRegs;

    }

    [BurstCompile]
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
    [BurstCompile]
    public void SetTargetController(PlayerBehaviour playerBehaviour)
    {

        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.isLocalPlayer = true;

    }
    [BurstCompile]
    public Vector2 GetDirection()
    {

        if (aimingDirection != Vector2.zero) return aimingDirection;
        else return finalDirection;

    }
    [BurstCompile]
    public void EnableController()
    {
        inputs.SquareController.Enable();
    }
    [BurstCompile]
    public void DisableController()
    {
        inputs.SquareController.Disable();
    }

}
