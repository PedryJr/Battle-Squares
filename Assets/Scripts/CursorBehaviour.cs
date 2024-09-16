using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class CursorBehaviour : MonoBehaviour
{
    [SerializeField]
    Sprite[] anim;
    [SerializeField]
    float[] scales;

    Image image;

    [SerializeField]
    float scale;

    Vector2 cursorPos;
    static Color brightColor = Color.white;
    static Color darkColor = Color.gray;
    Color cursorColor = Color.white;

    Inputs inputs;

    float lastScreenSize;
    float animTimer;

    static bool isEnabled = true;
    bool lastEnable = true;

    bool click;

    int animIndex;
    int lastAnimeIndex;

    float fadeTimer;
    float fadeLerp;

    float h, s, v;

    private void Awake()
    {
        Cursor.visible = false;
        DontDestroyOnLoad(transform.parent.parent);
        image = GetComponent<Image>();
        image.sprite = anim[0];

        inputs = new Inputs();
        inputs.Cursor.DoLocation.performed += (context) =>
        {
            cursorPos = context.ReadValue<Vector2>();
        };

        inputs.Cursor.DoClick.performed += (context) => { click = true; };
        inputs.Cursor.DoClick.canceled += (context) => { click = false; };

        ApplyImage(anim[0], scales[0]);
        animTimer = 0f;
        fadeTimer = 5f;
        fadeLerp = 1;

        SceneManager.sceneLoaded += MenuSceneLoaded;

    }

    private void MenuSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        if (arg0.name != "MenuScene") return;

        brightColor = Color.white;
        darkColor = Color.gray;
        cursorColor = Color.white;

    }

    private void Start()
    {

        inputs.Enable();

    }

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

        animIndex = Mathf.FloorToInt(animTimer * (anim.Length - 1));
        if (animIndex != lastAnimeIndex)
        {
            lastAnimeIndex = animIndex;
            ApplyImage(anim[animIndex], scales[animIndex]);
        }

        if(PlayerController.uiRegs <= 0)
        {
            if(fadeTimer > 0) fadeTimer -= Time.deltaTime * 8.5f;
            if(fadeTimer < 0) fadeTimer = 0;
            fadeLerp = Mathf.SmoothStep(0, 1, Mathf.Clamp(fadeTimer, 0, 1/3.2f) * 3.2f);
        }
        else
        {
            if (fadeTimer < 1) fadeTimer += Time.deltaTime * 8.5f;
            if (fadeTimer > 1) fadeTimer = 1;
            fadeLerp = Mathf.SmoothStep(0, 1, fadeTimer);
        }

        transform.position = cursorPos;
        image.color = Color.Lerp(darkColor, brightColor, fadeLerp);

    }

    void ApplyImage(Sprite newSprite, float scale)
    {
        image.sprite = newSprite;
        transform.localScale = new Vector3(scale, scale, scale) * this.scale;
    }

    public static void SetEnabled(bool enable)
    {
        isEnabled = enable;
    }

    public static void SetColor(Color brightColor, Color darkColor)
    {
        CursorBehaviour.brightColor = brightColor;
        CursorBehaviour.darkColor = darkColor;
    }

}
