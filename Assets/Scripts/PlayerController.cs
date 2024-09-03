using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{

    Rigidbody2D controllerTarget;
    public PlayerBehaviour playerBehaviour;

    Inputs inputs;

    Vector2 upInputDirection;
    Vector2 downInputDirection;
    Vector2 leftInputDirection;
    Vector2 rightInputDirection;

    delegate void InputAction();

    public Vector2 projectileDirection;
    public Vector2 finalDirection;
    public Vector2 aimingDirection;
    public Vector2 aimingDirectionSimple;

    List<InputAction> cancelInputs;

    public bool inputJump = false;

    public bool shootPrimary = false;
    public bool shootSecondary = false;

    public static float uiRegs = 0;

    float regs;

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

        inputs.SquareController.Aim.performed
            += (context) => { InputHandler(() => { aimingDirection = context.ReadValue<Vector2>(); }); };
        inputs.SquareController.Aim.canceled
            += (context) => { InputHandler(() => { aimingDirection = new Vector2(0, 0); }); };

        inputs.SquareController.Jump.performed
            += (context) => { InputHandler(() => { if (playerBehaviour.hasJump) { inputJump = true; playerBehaviour.hasJump = false; } }); };

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

    void InputHandler(InputAction action)
    {

        if (!playerBehaviour) return;
        if (playerBehaviour.isDead)
        {
            CancellAllInputs();
            return;
        }

        action();

    }

    public void CancellAllInputs()
    {

        foreach (InputAction action in cancelInputs) action();

    }

    private void Update()
    {

        SetFinalInputDirection();

        regs = uiRegs;

    }

    void SetFinalInputDirection()
    {

        if (controllerTarget == null) return;

        finalDirection = 
            (upInputDirection * 0.3f) +
            (downInputDirection * 0.3f) +
            leftInputDirection +
            rightInputDirection;

        if(!(downInputDirection == Vector2.zero
            && upInputDirection == Vector2.zero
            && leftInputDirection == Vector2.zero
            && rightInputDirection == Vector2.zero))
        {
            projectileDirection =
                upInputDirection +
                downInputDirection +
                leftInputDirection +
                rightInputDirection;
        }

        if(aimingDirection == Vector2.zero)
        {
            aimingDirectionSimple = Vector2.zero;
        }
        else
        {
            if(aimingDirection.x > 0.4f && aimingDirection.y > 0.4f)
            {

                aimingDirectionSimple.x = 0.8f;
                aimingDirectionSimple.y = 0.8f;

            }
            else if (aimingDirection.x < -0.4f && aimingDirection.y > 0.4f)
            {

                aimingDirectionSimple.x = -0.8f;
                aimingDirectionSimple.y = 0.8f;

            }
            else if (aimingDirection.x < -0.4f && aimingDirection.y < -0.4f)
            {

                aimingDirectionSimple.x = -0.8f;
                aimingDirectionSimple.y = -0.8f;

            }
            else if (aimingDirection.x > 0.4f && aimingDirection.y < -0.4f)
            {

                aimingDirectionSimple.x = 0.8f;
                aimingDirectionSimple.y = -0.8f;

            }
            else if (aimingDirection.x > -0.4f && Mathf.Abs(aimingDirection.y) < 0.4f)
            {

                aimingDirectionSimple.x = 1f;
                aimingDirectionSimple.y = 0f;

            }
            else if (Mathf.Abs(aimingDirection.x) < 0.4f && aimingDirection.y > -0.4f)
            {

                aimingDirectionSimple.x = 0f;
                aimingDirectionSimple.y = 1f;

            }
            else if (aimingDirection.x < -0.4f && Mathf.Abs(aimingDirection.y) < 0.4f)
            {

                aimingDirectionSimple.x = -1f;
                aimingDirectionSimple.y = 0f;

            }
            else if (Mathf.Abs(aimingDirection.x) < 0.4f && aimingDirection.y < -0.4f)
            {

                aimingDirectionSimple.x = 0f;
                aimingDirectionSimple.y = -1f;

            }
            else
            {
                aimingDirectionSimple = Vector2.zero;
            }

        }

    }

    public void SetTargetController(PlayerBehaviour playerBehaviour)
    {

        this.playerBehaviour = playerBehaviour;
        controllerTarget = this.playerBehaviour.GetComponent<Rigidbody2D>();
        playerBehaviour.isLocalPlayer = true;

    }

    public Vector2 GetDirection()
    {

        if (aimingDirection != Vector2.zero) return aimingDirection;
        else return finalDirection;

    }

}
