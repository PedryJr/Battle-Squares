using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerColorSelector : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    Image sliderKnob;

    Color selectedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    bool animating = true;

    float timer = 0;

    public Slider slider;

    private void Start()
    {
        
        slider = GetComponent<Slider>();
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
        timer = 0;

    }

    private void Update()
    {

        if(animating) Animation();

    }

    void Animation()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        timer += Time.deltaTime;

        if(timer >= 1)
        {
            timer = 1;
            animating = false;
        }

        UpdateHueKnob(Mathf.Lerp(0, playerSynchronizer.localSquare.PlayerColor.ReadColorHue, timer));

    }

    public void UpdateHueKnobPlayer(float hue)
    {

        if (animating) return;

        selectedColor = playerSynchronizer.UpdatePlayerColor(hue);

        Color baseColor = selectedColor;
        Vector3 baseNormalized = new Vector3(baseColor.r, baseColor.g, baseColor.b).normalized;
        Color normalColor = new Color(baseNormalized.x, baseNormalized.y, baseNormalized.z);

        Color cursorColor = normalColor * 0.6f;
        Color cursorDarkerColor = normalColor * 0.38f;

        CursorBehaviour.SetColor(
            playerSynchronizer.localSquare.PlayerColor.CursorDefaultColor,
            playerSynchronizer.localSquare.PlayerColor.CursorHoverColor);

        selectedColor.r *= 0.8f;
        selectedColor.g *= 0.8f;
        selectedColor.b *= 0.8f;
        selectedColor.a = 1;

        sliderKnob.color = selectedColor;

    }

    public void UpdateHueKnob(float hue)
    {

        float h, s, v;

        selectedColor = playerSynchronizer.localSquare.PlayerColor.PrimaryColor;

        Color baseColor = selectedColor;
        Vector3 baseNormalized = new Vector3(baseColor.r, baseColor.g, baseColor.b).normalized;
        Color normalColor = new Color(baseNormalized.x, baseNormalized.y, baseNormalized.z);

        CursorBehaviour.SetColor(
            playerSynchronizer.localSquare.PlayerColor.CursorDefaultColor,
            playerSynchronizer.localSquare.PlayerColor.CursorHoverColor);

        selectedColor.r *= 0.8f;
        selectedColor.g *= 0.8f;
        selectedColor.b *= 0.8f;
        selectedColor.a = 1;

        Color.RGBToHSV(selectedColor, out h, out s, out v);

        float playerH = playerSynchronizer.localSquare.PlayerColor.ReadColorHue;

        if (playerH == 0)
        {
            h = MyExtentions.EaseInOutCubic(hue / (playerH + 0.0001f)) * (playerH + 0.0001f);
        }
        else
        {
            h = MyExtentions.EaseInOutCubic(hue / playerH) * playerH;
        }

        if(playerH == hue)
        {
            h = hue;
        }

        selectedColor = Color.HSVToRGB(h, s, v);

        sliderKnob.color = selectedColor;
        slider.value = h;

    }

}
