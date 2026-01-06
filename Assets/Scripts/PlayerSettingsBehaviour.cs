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

    Color fromImageColor;
    Color fromImageDarkerColor;
    Color fromTextColor;
    Color[] fromNormColors;


    bool lastVisible;
    bool lastMuteState;
    float visibilityTimer;

    private void Awake()
    {

        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        imagesWithNormColors = new Color[imagesWithNorm.Length];
        for (int i = 0; i < imagesWithNorm.Length; i++) imagesWithNormColors[i] = imagesWithNorm[i].color;
        fromNormColors = new Color[imagesWithNorm.Length];

    }

    private void LateUpdate()
    {
        UpdateVisibility(selectedPlayer != null);
        if (selectedPlayer)
        {
            if (selectedPlayer.voiceMute != lastMuteState)
            {
                lastMuteState = selectedPlayer.voiceMute;
                ApplyMuteIcons(lastMuteState);
            }
        }
    }


    void UpdateVisibility(bool visibility)
    {
        if (visibility != lastVisible)
        {
            visibilityTimer = Mathf.Clamp01(visibilityTimer);
            CaptureCurrentColors();

            if (visibility)
            {
                foreach (GameObject go in settings)
                    if (!go.activeSelf)
                        go.SetActive(true);
            }
        }

        float dir = visibility ? 1f : -1f;
        visibilityTimer = Mathf.Clamp01(
            visibilityTimer + dir * (Time.deltaTime / timeToShow)
        );

        ApplyVisibilityLerp(MyExtentions.EaseInExpo(visibilityTimer), visibility);

        if (!visibility && visibilityTimer <= 0f)
        {
            foreach (GameObject go in settings)
                if (go.activeSelf)
                    go.SetActive(false);
        }

        lastVisible = visibility;
    }


    void CaptureCurrentColors()
    {
        fromImageColor = imagesWithHue.Length > 0 ? imagesWithHue[0].color : Color.clear;
        fromImageDarkerColor = imagesWithNorm.Length > 0 ? imagesWithNorm[0].color : Color.clear;
        fromTextColor = icons.Length > 0 ? icons[0].color : Color.clear;

        for (int i = 0; i < imagesWithNorm.Length; i++)
            fromNormColors[i] = imagesWithNorm[i].color;
    }



    void ApplyVisibilityLerp(float lerp, bool visibility)
    {
        Color targetColor = visibility && selectedPlayer
            ? selectedPlayer.PlayerColor.UiButtonColorHighlighted
            : Color.clear;

        Color targetDarkerColor = visibility && selectedPlayer
            ? selectedPlayer.PlayerColor.UiButtonColorNormal
            : Color.clear;

        Color targetTextColor = visibility ? Color.white : Color.clear;

        Color finalImageColor = Color.Lerp(fromImageColor, targetColor, lerp);
        Color finalImageDarkerColor = Color.Lerp(fromImageDarkerColor, targetDarkerColor, lerp);
        Color finalTextColor = Color.Lerp(fromTextColor, targetTextColor, lerp);

        foreach (Image img in imagesWithHue) img.color = finalImageColor;

        foreach (TMP_Text ico in icons) ico.color = finalTextColor;

        for (int i = 0; i < imagesWithNorm.Length; i++) imagesWithNorm[i].color = Color.Lerp(fromNormColors[i], visibility ? imagesWithNormColors[i] : Color.clear, lerp);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onHoveredColor = finalImageColor;
            buttons[i].offHoveredColor = finalImageDarkerColor;
        }
    }

    void ApplyMuteIcons(bool muted)
    {
        unMutedLogo.SetActive(!muted);
        mutedLogo.SetActive(muted);
    }

    public void SHOW(PlayerBehaviour selectedPlayer)
    {
        if (this.selectedPlayer == selectedPlayer)
        {
            this.selectedPlayer = null;
            return;
        }

        if (selectedPlayer == playerSynchronizer.localSquare)
            selectedPlayer.voiceMute = MySettings.Muted;

        this.selectedPlayer = selectedPlayer;
        volumeSlider.value = selectedPlayer.voiceVolume;

        // Cache mute state once
        lastMuteState = selectedPlayer.voiceMute;
        ApplyMuteIcons(lastMuteState);
    }


    public void TOGGLEMUTE()
    {
        selectedPlayer.voiceMute = !selectedPlayer.voiceMute;

        if (selectedPlayer == playerSynchronizer.localSquare)
            MySettings.SetMuted(selectedPlayer.voiceMute);

        lastMuteState = selectedPlayer.voiceMute;
        ApplyMuteIcons(lastMuteState);
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
