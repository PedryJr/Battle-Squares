using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class ChatContainer : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Image userIcon;

    [SerializeField]
    private TextMeshProUGUI contextContainer;

    private ulong sentId;
    private PlayerSynchronizer playerSynchronizer;

    public async void Initialize(string context, SteamId steamId, ulong sentId)
    {
        Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);
        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        userIcon.sprite = Sprite.Create(spriteTexture, spriteRect, spritePivot);
        contextContainer.text = context;
        this.sentId = sentId;
    }

    private void Update()
    {
        List<PlayerData> playerIdentities = playerSynchronizer.playerIdentities;

        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {
            if (sentId == playerIdentities[i].id)
            {
                contextContainer.color = UnityEngine.Color.Lerp(
                    playerIdentities[i].square.playerColor,
                    playerIdentities[i].square.playerDarkerColor,
                    0.5f);

                return;
            }
        }
    }

    private struct Data
    {
        public ulong id;
        public UnityEngine.Color dark;
        public UnityEngine.Color bright;
    }
}