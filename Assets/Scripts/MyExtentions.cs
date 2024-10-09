using Steamworks;
using Steamworks.Data;
using System;
using System.IO.Compression;
using System.IO;
using UnityEngine;
using Unity.VisualScripting;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Mathematics;
public static class MyExtentions
{

    public static int scoreCapture;

    public static byte[] EncodePosition(float x, float y)
    {
        int scaledX = (int)(x * 32);
        int scaledY = (int)(y * 32);

        byte x1 = (byte)((scaledX >> 12) & 0xF);
        byte x2 = (byte)((scaledX >> 8) & 0xF);
        byte x3 = (byte)((scaledX >> 4) & 0xF);
        byte x4 = (byte)(scaledX & 0xF);

        byte y1 = (byte)((scaledY >> 12) & 0xF);
        byte y2 = (byte)((scaledY >> 8) & 0xF);
        byte y3 = (byte)((scaledY >> 4) & 0xF);
        byte y4 = (byte)(scaledY & 0xF);

        byte[] result = new byte[4];
        result[0] = (byte)((x1 << 4) | y1);
        result[1] = (byte)((x2 << 4) | y2);
        result[2] = (byte)((x3 << 4) | y3);
        result[3] = (byte)((x4 << 4) | y4);

        return result;
    }

    public static (float, float) DecodePosition(byte[] bytes)
    {
        int x1 = (bytes[0] >> 4) & 0xF;
        int y1 = bytes[0] & 0xF;

        int x2 = (bytes[1] >> 4) & 0xF;
        int y2 = bytes[1] & 0xF;

        int x3 = (bytes[2] >> 4) & 0xF;
        int y3 = bytes[2] & 0xF;

        int x4 = (bytes[3] >> 4) & 0xF;
        int y4 = bytes[3] & 0xF;

        int scaledX = (x1 << 12) | (x2 << 8) | (x3 << 4) | x4;
        int scaledY = (y1 << 12) | (y2 << 8) | (y3 << 4) | y4;

        float x = scaledX / 32.0f;
        float y = scaledY / 32.0f;

        return (x, y);
    }

    public static byte[] EncodeRotation(float rotation)
    {
        int scaledRotation = (int)((rotation / 360.0f) * 65535);

        byte highByte = (byte)((scaledRotation >> 8) & 0xFF);
        byte lowByte = (byte)(scaledRotation & 0xFF);

        byte[] result = new byte[2];
        result[0] = highByte;
        result[1] = lowByte;

        return result;
    }

    public static float DecodeRotation(byte[] bytes)
    {
        if (bytes.Length != 2)
            throw new ArgumentException("Input must be exactly 2 bytes.");

        int highByte = bytes[0] & 0xFF;
        int lowByte = bytes[1] & 0xFF;

        int scaledRotation = (highByte << 8) | lowByte;

        float rotation = (scaledRotation / 65535.0f) * 360.0f;

        return rotation;
    }

    public static byte[] EncodeFloat(float value)
    {
        float minValue = -1000.0f;
        float maxValue = 1000.0f;
        int range = (int)(maxValue - minValue);

        int scaledValue = (int)(((value - minValue) / range) * 16777215);

        scaledValue = math.max(0, math.min(16777215, scaledValue));

        byte byte1 = (byte)((scaledValue >> 16) & 0xFF);
        byte byte2 = (byte)((scaledValue >> 8) & 0xFF);
        byte byte3 = (byte)(scaledValue & 0xFF);

        return new byte[] { byte1, byte2, byte3 };
    }

    public static float DecodeFloat(byte[] bytes)
    {
        if (bytes.Length != 3)
            throw new ArgumentException("Input must be exactly 3 bytes.");

        int scaledValue = (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];

        float minValue = -1000.0f;
        float maxValue = 1000.0f;
        int range = (int)(maxValue - minValue);

        float value = minValue + ((scaledValue / 16777215.0f) * range);

        return value;
    }

    public static byte[] EncodeNozzlePosition(float x, float y)
    {

        x = math.clamp(x, -1, 1);
        y = math.clamp(y, -1, 1);

        byte scaledX = (byte)((x + 1.0f) * 127.5f);
        byte scaledY = (byte)((y + 1.0f) * 127.5f);

        return new byte[] { scaledX, scaledY };
    }

    public static (float, float) DecodeNozzlePosition(byte[] bytes)
    {

        byte scaledX = bytes[0];
        byte scaledY = bytes[1];

        float x = (scaledX / 127.5f) - 1.0f;
        float y = (scaledY / 127.5f) - 1.0f;

        return (x, y);
    }

    public static float EaseInOutCubic(float x)
    {
        if (x < 0.5)
        {
            return 4 * x * x * x;
        }
        else
        {
            return 1 - (float) math.pow(-2 * x + 2, 3) / 2;
        }
    }

    public static float EaseInExpo(float x)
    {
        return x == 0 ? 0 : (float) math.pow(2, 10 * x - 10);
    }

    public static float EaseInQuad(float x)
    {
        return x * x;
    }

    public static float EaseOutQuad(float x)
    {
        return 1 - (1 - x) * (1 - x);
    }

    public static Sprite GetImageData(SteamId steamId)
    {

        Image? image = SteamFriends.GetLargeAvatarAsync(steamId).Result;
        if (image != null) return SteamImgToSprite(image.Value);
        else return null;

    }

    public static async Task<Sprite> GetImageFromSteam(SteamId steamId)
    {

        Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        return Sprite.Create(spriteTexture, spriteRect, spritePivot);

    }

    public static Sprite SteamImgToSprite(Image image)
    {

        byte[] imageData = image.Data;
        Debug.Log(imageData.Length);
        uint imageWidth = image.Width;
        uint imageHeight = image.Height;
        Vector2 imageDimentions = new Vector2(image.Width, image.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        return (Sprite.Create(spriteTexture, spriteRect, spritePivot));

    }

    public static Sprite CreateSpriteFromData(byte[] imageData, uint imageWidth, uint imageHeight)
    {

        Vector2 imageDimentions = new Vector2(imageWidth, imageHeight);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        return (Sprite.Create(spriteTexture, spriteRect, spritePivot));

    }

    public static byte[] CompressByteArray(byte[] data)
    {
        using (var outputStream = new MemoryStream())
        {
            using (var compressionStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                compressionStream.Write(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }
    }

    public static byte[] DecompressByteArray(byte[] compressedData)
    {
        using (var inputStream = new MemoryStream(compressedData))
        using (var outputStream = new MemoryStream())
        using (var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            decompressionStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }

    public static byte[] FlattenByteArray(byte[][] jaggedArray)
    {
        int length = 0;
        foreach (byte[] array in jaggedArray)
        {
            length += array.Length;
        }

        byte[] flatArray = new byte[length];
        int offset = 0;

        foreach (byte[] array in jaggedArray)
        {
            Buffer.BlockCopy(array, 0, flatArray, offset, array.Length);
            offset += array.Length;
        }

        return flatArray;
    }

    public static Vector2 AngleToNormalizedCoordinate(float angle)
    {
        float radians = math.radians(angle);

        float x = math.cos(radians);
        float y = math.sin(radians);

        return new Vector2(x, y).normalized;
    }
    public static string SanitizeMessage(string message)
    {
        // Truncate the message to 120 characters if it's longer
        message = message.Length > 120 ? message.Substring(0, 120) : message;

        // Regular expression to match allowed characters
        message = Regex.Replace(message,
            @"[^\p{L}\p{N}\p{Sc}\p{Sm}\p{Mn}\p{Pc}\p{Pd}\p{Zs}.,<>{}|_+=!?;:'""\-\(\)]",
            string.Empty);

        return message;
    }


    public static float EaseOnHover(float x)
    {

        return 1 - math.pow(1 - x, 5);

    }

    public static float EaseOffHover(float x)
    {

        float f1, f2, sum;
        float a, b;
        a = 4.1f;
        b = -3.8f;

        f1 = (1.70158f + 1) * x * x * x - 1.70158f * x * x;
        f2 = math.exp(-a * x) * math.sin(b * x);

        sum = f1 + f2;

        return sum;

    }

    public static float EaseOnClick(float x)
    {

        return 1 - math.pow(1 - x, 5);

    }

}
