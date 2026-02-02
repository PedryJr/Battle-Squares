using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

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

    public static float PlayerGravity
    {
        get => at[0];
        set => at[0] = value;
    }

    public static float PlayerSpeed
    {
        get => at[1];
        set => at[1] = value;
    }

    public static float JumpForce
    {
        get => at[2];
        set => at[2] = value;
    }

    public static float ProjectileSpeed
    {
        get => at[3];
        set => at[3] = value;
    }

    public static float BaseDamage
    {
        get => at[4];
        set => at[4] = value;
    }

    public static float ProjectileGravity
    {
        get => at[5];
        set => at[5] = value;
    }

    public static float MeleeDamage
    {
        get => at[6];
        set => at[6] = value;
    }

    public static float AoeDamage
    {
        get => at[7];
        set => at[7] = value;
    }

    public static float PlayerAcceleration
    {
        get => at[8];
        set => at[8] = value;
    }

    public static float NormalizeMovement
    {
        get => at[9];
        set => at[9] = value;
    }

    public static float PlayerHealth
    {
        get => at[10];
        set => at[10] = value;
    }

    public static float DamageOverTime
    {
        get => at[11];
        set => at[11] = value;
    }

    public static float Knockback
    {
        get => at[12];
        set => at[12] = value;
    }

    public static float Recoil
    {
        get => at[13];
        set => at[13] = value;
    }

    public static float Bounce
    {
        get => at[14];
        set => at[14] = value;
    }

    public static float Friction
    {
        get => at[15];
        set => at[15] = value;
    }


    static string modsFilePath;

    public static void SaveMods()
    {
        modsFilePath = Path.Combine(SaveManager.saveFolderPath, "mods.json");

        ModsData data = new ModsData
        {
            at = at,
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(modsFilePath, json);
    }

    public static void LoadMods()
    {
        modsFilePath = Path.Combine(SaveManager.saveFolderPath, "mods.json");

        if (File.Exists(modsFilePath))
        {
            string json = File.ReadAllText(modsFilePath);
            ModsData data = JsonUtility.FromJson<ModsData>(json);

            if (data?.at != null && data.at.Length == at.Length)
            {
                at = data.at;
            }
            else if (data?.at != null)
            {
                // If saved array length differs, copy as many values as possible and keep defaults for the rest.
                int min = Mathf.Min(at.Length, data.at.Length);
                for (int i = 0; i < min; i++)
                {
                    at[i] = data.at[i];
                }
            }
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

