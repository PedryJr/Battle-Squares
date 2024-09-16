using Steamworks;
using Steamworks.Data;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class LocalSteamData : MonoBehaviour
{

    public byte[] pfpData;
    public uint imageWidth;
    public uint imageHeight;

    public YRows[] yRows;

    public Sprite pfp;

    public void Init(ulong steamId)
    {

        GetImageData(steamId);

    }

    public async void GetImageData(ulong steamId)
    {

        Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);
        if (image == null) return;

        pfpData = image.Value.Data;
        imageWidth = image.Value.Width;
        imageHeight = image.Value.Height;

        pfp = MyExtentions.CreateSpriteFromData(pfpData, imageWidth, imageHeight);

        yRows = new YRows[imageHeight];
        for (int y = 0; y < yRows.Length; y++)
        {

            yRows[y].xRow = new XRows[imageWidth];

            for (int x = 0; x < yRows[y].xRow.Length; x++)
            {

                yRows[y].xRow[x].pixel = new PixelData
                {

                    rgb = new byte[3]
                    {
                        (byte) Mathf.RoundToInt(Mathf.Clamp(pfp.texture.GetPixel(x, y).r * 256f, 0f, 256f)),
                        (byte) Mathf.RoundToInt(Mathf.Clamp(pfp.texture.GetPixel(x, y).g * 256f, 0f, 256f)),
                        (byte) Mathf.RoundToInt(Mathf.Clamp(pfp.texture.GetPixel(x, y).b * 256f, 0f, 256f))
                    },
                    x = x,
                    y = y

                };

            }

        }


    }

}

[Serializable]
public struct YRows
{

    public XRows[] xRow;

}
[Serializable]
public struct XRows
{

    public PixelData pixel;

}

[Serializable]
public struct PixelData
{

    public byte[] rgb;
    public int x;
    public int y;

}
