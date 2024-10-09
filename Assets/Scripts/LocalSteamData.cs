using Steamworks;
using Steamworks.Data;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public sealed class LocalSteamData : MonoBehaviour
{

    public byte[] pfpData;
    public uint imageWidth;
    public uint imageHeight;

    public YRows[] yRows;

    public Sprite pfp;

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
