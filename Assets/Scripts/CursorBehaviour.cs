using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
[BurstCompile]
public sealed class CursorBehaviour : MonoBehaviour
{

    [SerializeField]
    Sprite[] anim;
    [SerializeField]
    float[] scales;

    public SpriteRenderer image;

    bool isAppFocused = false;

    [SerializeField]
    float scale;

    [SerializeField]
    bool forceEnable;

    Vector3 targetScale = Vector3.one;
    Vector2 cursorPos;
    public static Color brightColor = Color.white;
    public static Color darkColor = Color.gray;
    Color cursorColor = Color.gray;

    Inputs inputs;
    PosessCursorInput posessionInputs;
    PlayerController controller;
    InputUser inputUser;

    float lastScreenSize;
    float animTimer;

    static bool isEnabled = true;
    bool lastEnable = true;

    public bool click;

    int animIndex;
    int lastAnimeIndex;

    float fadeTimer;
    float fadeLerp;

    bool isPosessed;

    public void TogglePosessCursor(InputUser user, PlayerController controller)
    {
        if (alwaysPosess) return;
        if (isPosessed)
        {
            transform.localScale = Vector3.one * scale;
            if (this.controller != controller) return;
            controller.EnableController();
            inputUser.UnpairDevices();
            isPosessed = false;
        }
        else
        {
            transform.localScale = Vector3.one * scale;
            this.controller = controller;
            controller.DisableController();
            InputUser.PerformPairingWithDevice(user.pairedDevices[0], inputUser, InputUserPairingOptions.None);
            isPosessed = true;
        }
    }
    PosessCursorInput lmaoInputs;



    float h, s, v;
    [BurstCompile]
    private void Awake()
    {

        Application.focusChanged += Application_focusChanged;
        isAppFocused = Application.isFocused;

        Cursor.visible = false;
        image = GetComponent<SpriteRenderer>();
        image.sprite = anim[0];

        inputs = new Inputs();
/*        
        posessionInputs.PosessionActions.MoveAround.performed += MoveAround_performed;
        posessionInputs = new PosessCursorInput();
        posessionInputs.PosessionActions.MoveAround.canceled += MoveAround_canceled;

        posessionInputs.PosessionActions.Click.performed += Click_canceled;
        posessionInputs.PosessionActions.Click.canceled += Click_performed;*/


/*        lmaoInputs = new PosessCursorInput();
        lmaoInputs.PosessionActions.No.performed += (_) =>
        {
            VLog.Log("AtTempting click");
            MouseClickDown(true, false);
        };
        lmaoInputs.PosessionActions.No.canceled += (_) =>
        {
            VLog.Log("AtTempting click");
            MouseClickDown(true, true);
        };
        lmaoInputs.Enable();

        posessionInputs.Enable();*/

/*        if (!alwaysPosess)
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
            inputUser.AssociateActionsWithUser(posessionInputs);
        }*/

        inputs.Cursor.DoLocation.performed += (context) =>
        {
            InactivityBehaviour.inactivityTimer = InactivityBehaviour.MAX;
            cursorPos = context.ReadValue<Vector2>();
        };

        inputs.Cursor.DoClick.performed += (context) => 
        {
            InactivityBehaviour.inactivityTimer = InactivityBehaviour.MAX;
            click = true; 
        };
        inputs.Cursor.DoClick.canceled += (context) => 
        {
            InactivityBehaviour.inactivityTimer = InactivityBehaviour.MAX;
            click = false; 
        };

        ApplyImage(anim[0], scales[0]);
        animTimer = 0f;
        fadeTimer = 5f;
        fadeLerp = 1;

        SceneManager.sceneLoaded += MenuSceneLoaded;

    }

    [SerializeField] float sensitivity = 100f;
    [SerializeField] bool alwaysPosess = true;

    private void Application_focusChanged(bool obj)
    {
        isAppFocused = obj;
    }



/*    private void MoveAround_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) => moveDirection = Vector2.zero;
    private void MoveAround_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) => moveDirection = obj.ReadValue<Vector2>();*/

/*    private void Click_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (alwaysPosess) MouseClickDown(true, false);
        else if (isPosessed && isAppFocused) MouseClickDown(true, false);
    }
    private void Click_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (alwaysPosess) MouseClickDown(true, true);
        else if (isPosessed && isAppFocused) MouseClickDown(true, true);
    }*/


    private void LateUpdate()
    {

        /*        bool invPosessCondition = !isPosessed || !isAppFocused;
                if (alwaysPosess || !invPosessCondition) PosessCursor();*/
    }

    void PosessCursor()
    {
/*        Vector2 posessionMovement = moveDirection * Time.deltaTime * ((Display.main.systemWidth + Display.main.systemHeight) / 2f);
        MoveMouse(posessionMovement.x, -posessionMovement.y, sensitivity);

        Vector3 debugPosStart = transform.position;
        debugPosStart.z = Camera.main.transform.position.z + 3f;

        Debug.DrawLine(debugPosStart, debugPosStart + (Vector3)moveDirection, Color.green, Time.deltaTime);*/
    }


    Vector2 moveDirection = Vector2.zero;

    [BurstCompile]
    private void MenuSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        if(arg0.name == "MenuScene")
        {

            brightColor = Color.white;
            darkColor = Color.gray;
            cursorColor = Color.gray;

        }

    }
    [BurstCompile]
    private void Start()
    {

        inputs.Enable();
        if(forceEnable) SetEnabled(true);

    }

    public bool skipColorManip;

    [BurstCompile]
    private void Update()
    {

        if(lastEnable != isEnabled)
        {
            lastEnable = isEnabled;
            if (isEnabled) image.enabled = true;
            else image.enabled = false;
        }

        if (!isEnabled) return;

        if (click)
        {
            if (animTimer < 1) animTimer += Time.deltaTime * 20;
            if (animTimer > 1) animTimer = 1;
        }
        else
        {
            if (animTimer > 0) animTimer -= Time.deltaTime * 20;
            if (animTimer < 0) animTimer = 0;
        }

        animIndex = (int) math.floor(animTimer * (anim.Length - 1));
        if (animIndex != lastAnimeIndex)
        {
            lastAnimeIndex = animIndex;
            ApplyImage(anim[animIndex], scales[animIndex]);
        }

        if(PlayerController.uiRegs <= 0)
        {
            if(fadeTimer > 0) fadeTimer -= Time.deltaTime * 6f;
            if(fadeTimer < 0) fadeTimer = 0;
            fadeLerp = math.smoothstep(0, 1, fadeTimer);
        }
        else
        {
            if (fadeTimer < 1) fadeTimer += Time.deltaTime * 6f;
            if (fadeTimer > 1) fadeTimer = 1;
            fadeLerp = math.smoothstep(0, 1, fadeTimer);
        }

        transform.position = Camera.main.ScreenToWorldPoint(cursorPos) + new Vector3(0, 0, 1);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15);
        if(!skipColorManip) image.color = Color.Lerp(darkColor, brightColor, fadeLerp);

    }
    [BurstCompile]
    void ApplyImage(Sprite newSprite, float scale)
    {
        image.sprite = newSprite;/*
        transform.localScale = new Vector3(scale, scale, scale) * this.scale;*/
    }


    public static void SetEnabled(bool enable)
    {
        isEnabled = enable;
        Cursor.lockState = CursorLockMode.None;
    }

    public static void SetColor(Color brightColor, Color darkColor)
    {
        CursorBehaviour.brightColor = brightColor;
        CursorBehaviour.darkColor = darkColor;
    }

    private void OnDisable()
    {
        
        inputs.Disable();

    }

    private void OnEnable()
    {
        
        inputs.Enable();

    }

    private void OnDestroy()
    {

/*        posessionInputs.PosessionActions.MoveAround.performed -= MoveAround_performed;
        posessionInputs.PosessionActions.MoveAround.canceled -= MoveAround_canceled;

        posessionInputs.PosessionActions.Click.performed -= Click_canceled;
        posessionInputs.PosessionActions.Click.canceled -= Click_performed;*/

        inputs.Dispose();

    }

}
