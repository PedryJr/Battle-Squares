using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerSettingsBehaviour : MonoBehaviour
{

    [SerializeField]
    float timeToShow;

    [SerializeField]
    ButtonHoverAnimation[] buttons;

    [SerializeField]
    GameObject[] settings;

    [SerializeField]
    Image[] imagesWithHue;

    [SerializeField]
    TMP_Text[] icons;

    [SerializeField]
    Image[] imagesWithNorm;
    Color[] imagesWithNormColors;

    [SerializeField]
    GameObject unMutedLogo, mutedLogo;
    [SerializeField]
    Slider volumeSlider;

    PlayerSynchronizer playerSynchronizer;
    PlayerBehaviour selectedPlayer;

    Color lastPlayerColor;

    bool lastVisible;

    float visibilityTimer;

    private void Awake()
    {

        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        imagesWithNormColors = new Color[imagesWithNorm.Length];
        for (int i = 0; i < imagesWithNorm.Length; i++) imagesWithNormColors[i] = imagesWithNorm[i].color;

    }

    private void LateUpdate()
    {

        UpdateVisibility(selectedPlayer ? true : false);

    }

    void UpdateVisibility(bool visibility)
    {

        if (visibility != lastVisible) visibilityTimer = 0;

        foreach (GameObject gameObject in settings)
        {

            if(gameObject.activeSelf != visibility) gameObject.SetActive(visibility);

        }

        if (visibility) ShowElementsWithHue();

        lastVisible = visibility;

    }

    void ShowElementsWithHue()
    {

        float h, s, v, lerp;
        Color targetColor, targetDarkerColor, finalImageColor, finalImageDarkerColor, finalTextColor, initButtonColor;

        targetColor = selectedPlayer.playerColor;
        targetDarkerColor = selectedPlayer.playerDarkerColor;
        visibilityTimer = Mathf.Clamp01(visibilityTimer + (Time.deltaTime / timeToShow));

        targetColor *= 0.8f;
        targetColor.a = 1;

        targetDarkerColor *= 0.8f;
        targetDarkerColor.a = 1;

        Color.RGBToHSV(targetColor, out h, out s, out v);
        targetColor = Color.HSVToRGB(h, s, v);

        lerp = MyExtentions.EaseInExpo(visibilityTimer);

        finalImageColor = Color.Lerp(Color.clear, targetColor, lerp);
        finalImageDarkerColor = Color.Lerp(Color.clear, targetDarkerColor, lerp);
        finalTextColor = Color.Lerp(Color.clear, Color.white, lerp);

        foreach (Image img in imagesWithHue) img.color = finalImageColor;
        foreach (TMP_Text ico in icons) ico.color = finalTextColor;
        for (int i = 0; i < imagesWithNorm.Length; i++)
        {

            imagesWithNorm[i].color = Color.Lerp(Color.clear, imagesWithNormColors[i], lerp);

        }
        for (int i = 0; i < buttons.Length; i++)
        {

            buttons[i].onHoveredColor = finalImageColor;
            buttons[i].offHoveredColor = finalImageDarkerColor;
            initButtonColor = buttons[i].isHovering ? finalImageColor : finalImageDarkerColor;


            if (visibilityTimer >= 1)
            {

                if(lastPlayerColor != selectedPlayer.playerColor)
                {

                    buttons[i].toColor = initButtonColor;

                }

            }
            else
            {

                buttons[i].image.color = initButtonColor;
                buttons[i].currentColor = initButtonColor;
                buttons[i].toColor = initButtonColor;

            }

        }

        if (selectedPlayer.voiceMute)
        {
            if (unMutedLogo.activeSelf != false) unMutedLogo.SetActive(false);
            if (mutedLogo.activeSelf != true) mutedLogo.SetActive(true);
        }
        else
        {
            if (unMutedLogo.activeSelf != true) unMutedLogo.SetActive(true);
            if (mutedLogo.activeSelf != false) mutedLogo.SetActive(false);
        }

        lastPlayerColor = selectedPlayer.playerColor;

    }

    public void SHOW(PlayerBehaviour selectedPlayer) 
    {

        if (this.selectedPlayer == selectedPlayer) { this.selectedPlayer = null; return; }

        if (selectedPlayer == playerSynchronizer.localSquare) selectedPlayer.voiceMute = MySettings.muted;

        this.selectedPlayer = selectedPlayer;
        visibilityTimer = 0;
        volumeSlider.value = (selectedPlayer.voiceVolume);

    }

    public void TOGGLEMUTE()
    {


        selectedPlayer.voiceMute = !selectedPlayer.voiceMute;

        if (selectedPlayer == playerSynchronizer.localSquare)
        {
            MySettings.muted = selectedPlayer.voiceMute;
            MySettings.SaveSettings();
        }

    }

    public void VOLUME(float volume)
    {

        selectedPlayer.voiceVolume = volume;

    }

    public void KICK()
    {

        playerSynchronizer.KickPlayerClientRpc((byte)selectedPlayer.id);

    }

    public async void PROFILE()
    {

        await selectedPlayer.friend.RequestInfoAsync();

        SteamFriends.OpenUserOverlay(selectedPlayer.friend.Id, "steamid");

    }

}
