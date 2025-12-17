using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class EncryptedDouble
{
    private readonly string key;
    private static readonly string FolderPath = Path.Combine(Application.persistentDataPath, "enc_data");

    // Must be 32 bytes (key) and 16 bytes (IV)
    private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
    private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("ABCDEF0123456789");

    public EncryptedDouble(string savedKey, double defaultValue = 0)
    {
        key = savedKey;

        Directory.CreateDirectory(FolderPath);

        if (!FileExists(key))
            Value = defaultValue;
    }

    // ---------------------------
    // File path for this key
    // ---------------------------
    private static string GetPath(string key)
        => Path.Combine(FolderPath, key + ".dat");

    private static bool FileExists(string key)
        => File.Exists(GetPath(key));

    private static string FileRead(string key)
        => File.Exists(GetPath(key)) ? File.ReadAllText(GetPath(key)) : "";

    private static void FileWrite(string key, string value)
        => File.WriteAllText(GetPath(key), value);

    // ------------------------------------
    //            PUBLIC API
    // ------------------------------------

    public double Value
    {
        get
        {
            try
            {
                string encrypted = FileRead(key);
                if (string.IsNullOrEmpty(encrypted))
                    return 0;

                string decrypted = Decrypt(encrypted);
                return double.Parse(decrypted);
            }
            catch
            {
                return 0;
            }
        }

        set
        {
            string encrypted = Encrypt(value.ToString("G17"));
            FileWrite(key, encrypted);
        }
    }

    // ------------------------------------
    //            ENCRYPTION
    // ------------------------------------

    private static string Encrypt(string plain)
    {
        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.IV = AES_IV;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
            sw.Write(plain);

        return Convert.ToBase64String(ms.ToArray());
    }

    private static string Decrypt(string cipher)
    {
        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.IV = AES_IV;

        using var ms = new MemoryStream(Convert.FromBase64String(cipher));
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
