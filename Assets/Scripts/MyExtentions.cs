using System;
using System.Collections;
using UnityEngine;
using K4os.Compression.LZ4;
using System.IO;
using Steamworks;
using Unity.VisualScripting;
using Steamworks.Data;
using System.Threading.Tasks;
using System.IO.Compression;

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
        // Scale rotation from 0-360 range to 0-65535 range
        int scaledRotation = (int)((rotation / 360.0f) * 65535);

        // Extract high and low bytes
        byte highByte = (byte)((scaledRotation >> 8) & 0xFF);
        byte lowByte = (byte)(scaledRotation & 0xFF);

        // Pack into 2 bytes
        byte[] result = new byte[2];
        result[0] = highByte;
        result[1] = lowByte;

        return result;
    }

    // Function to decode rotation angle
    public static float DecodeRotation(byte[] bytes)
    {
        if (bytes.Length != 2)
            throw new ArgumentException("Input must be exactly 2 bytes.");

        // Extract high and low bytes
        int highByte = bytes[0] & 0xFF;
        int lowByte = bytes[1] & 0xFF;

        // Reconstruct the scaled rotation
        int scaledRotation = (highByte << 8) | lowByte;

        // Scale back to the original 0-360 range
        float rotation = (scaledRotation / 65535.0f) * 360.0f;

        return rotation;
    }

    public static byte[] EncodeFloat(float value)
    {
        // Define the range
        float minValue = -1000.0f;
        float maxValue = 1000.0f;
        int range = (int)(maxValue - minValue); // 2000

        // Scale the value to fit into the range of 0 to 16777215
        int scaledValue = (int)(((value - minValue) / range) * 16777215);

        // Ensure the scaledValue is within the valid range
        scaledValue = Math.Max(0, Math.Min(16777215, scaledValue));

        // Extract the three bytes
        byte byte1 = (byte)((scaledValue >> 16) & 0xFF);
        byte byte2 = (byte)((scaledValue >> 8) & 0xFF);
        byte byte3 = (byte)(scaledValue & 0xFF);

        // Pack into 3 bytes
        return new byte[] { byte1, byte2, byte3 };
    }

    // Function to decode float
    public static float DecodeFloat(byte[] bytes)
    {
        if (bytes.Length != 3)
            throw new ArgumentException("Input must be exactly 3 bytes.");

        // Reconstruct the scaled value from the three bytes
        int scaledValue = (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];

        // Define the range
        float minValue = -1000.0f;
        float maxValue = 1000.0f;
        int range = (int)(maxValue - minValue); // 2000

        // Convert the scaled value back to the original float range
        float value = minValue + ((scaledValue / 16777215.0f) * range);

        return value;
    }

    public static byte[] EncodeNozzlePosition(float x, float y)
    {

        x = Mathf.Clamp(x, -1, 1);
        y = Mathf.Clamp(y, -1, 1);

        // Scale position from -1 to 1 range to 0 to 255 range
        byte scaledX = (byte)((x + 1.0f) * 127.5f);
        byte scaledY = (byte)((y + 1.0f) * 127.5f);

        // Pack x and y into 2 bytes
        return new byte[] { scaledX, scaledY };
    }

    // Function to decode position
    public static (float, float) DecodeNozzlePosition(byte[] bytes)
    {
        if (bytes.Length != 2)
            throw new ArgumentException("Input must be exactly 2 bytes.");

        // Extract x and y bytes
        byte scaledX = bytes[0];
        byte scaledY = bytes[1];

        // Scale back to the -1 to 1 range
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
            return 1 - (float) Math.Pow(-2 * x + 2, 3) / 2;
        }
    }

    public static float EaseInExpo(float x)
    {
        return x == 0 ? 0 : (float) Math.Pow(2, 10 * x - 10);
    }

    public static float EaseInQuad(float x)
    {
        return x * x;
    }

    public static float EaseOutQuad(float x)
    {
        return 1 - (1 - x) * (1 - x);
    }

    public static async Task<Sprite> GetImageData(SteamId steamId)
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

        return (Sprite.Create(spriteTexture, spriteRect, spritePivot));

    }

}
