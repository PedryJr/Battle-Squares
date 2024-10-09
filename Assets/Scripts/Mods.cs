using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Mods : MonoBehaviour
{

    public static float[] at = new float[]
    {
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        0,
        20,
        1,
        1,
        1,
        0.25f,
        0.7f,
    };

    public static float playerGravity = 1; //0
    public static float playerSpeed = 1; //1
    public static float jumpForce = 1; //2

    public static float ballisticSpeed = 1; //3
    public static float ballisticDagame = 1; //4
    public static float ballisticGravity = 1; //5   
    public static float meleeDamage = 1; //6
    public static float aoeDamage = 1; //7

    public static float playerAcceleration = 1; //8
    public static float normalizedMovement = 0; //9
    public static float playerHealth = 20; //10

    public static float damageOverTime = 1; //11

    public static float knockBack = 1; //12
    public static float recoil = 1; //13

    public static float bounce = 0.22f; //14
    public static float friction = 0.7f; //15

    static string modsFilePath;


    public static void SaveMods()
    {

        modsFilePath = Path.Combine(Application.persistentDataPath, "mods.json");

        ModsData data = new ModsData
        {
            at = at,
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(modsFilePath, json);
    }

    public static void LoadMods()
    {
        if (File.Exists(modsFilePath))
        {
            string json = File.ReadAllText(modsFilePath);
            ModsData data = JsonUtility.FromJson<ModsData>(json);

            at = data.at;

        }

    }

}

public class ModsData
{

    public float[] at = new float[]
    {
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        0,
        20,
        1,
        1,
        1,
        0.25f,
        0.7f,
    };

}

