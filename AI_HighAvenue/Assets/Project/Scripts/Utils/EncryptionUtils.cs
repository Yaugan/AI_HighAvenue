using System.IO;
using System.Security.Cryptography;

public static class EncryptionUtils
{
    public static byte[] EncryptStringToBytes(string plainText)
    {
        byte[] key = AESKeyStore.GetKey();
        byte[] iv = AESKeyStore.GetIV();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor();
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
                sw.Close();
                return ms.ToArray();
            }
        }
    }

    public static string DecryptStringFromBytes(byte[] cipherText)
    {
        byte[] key = AESKeyStore.GetKey();
        byte[] iv = AESKeyStore.GetIV();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            using (var ms = new MemoryStream(cipherText))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
