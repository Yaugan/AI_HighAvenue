using UnityEngine;
using System;
using System.Security.Cryptography;


public static class AESKeyStore
{
    private const string KeyPref = "AES_KEY";
    private const string IVPref = "AES_IV";

    public static byte[] GetKey()
    {
        if (!PlayerPrefs.HasKey(KeyPref))
            GenerateAndSaveKey();

        return Convert.FromBase64String(PlayerPrefs.GetString(KeyPref));
    }

    public static byte[] GetIV()
    {
        if (!PlayerPrefs.HasKey(IVPref))
            GenerateAndSaveKey();

        return Convert.FromBase64String(PlayerPrefs.GetString(IVPref));
    }

    public static void GenerateAndSaveKey()
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            PlayerPrefs.SetString(KeyPref, Convert.ToBase64String(aes.Key));
            PlayerPrefs.SetString(IVPref, Convert.ToBase64String(aes.IV));
            PlayerPrefs.Save();

            Debug.Log("🔑 New AES key and IV generated and stored.");
        }
    }

    public static void ResetKeys()
    {
        PlayerPrefs.DeleteKey(KeyPref);
        PlayerPrefs.DeleteKey(IVPref);
        PlayerPrefs.Save();
        Debug.Log("🔁 AES keys reset.");
    }
}
