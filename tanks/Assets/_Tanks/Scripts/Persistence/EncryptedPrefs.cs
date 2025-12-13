using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Tanks.Complete.Persistence
{
    /// <summary>
    /// PlayerPrefs wrapper that encrypts string values before saving.
    /// This keeps persisted auth data readable only by the app.
    /// </summary>
    public static class EncryptedPrefs
    {
        // Hard-coded key/IV are acceptable for this demo project. Obfuscate for production.
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("tanks-2025b-encrypt-key-32bytes!");
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("tanks-2025-iv-16");

        public static void SetString(string key, string value)
        {
            if (value == null)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            var cipher = Encrypt(value);
            PlayerPrefs.SetString(key, cipher);
        }

        public static string GetString(string key, string defaultValue = null)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }

            var cipher = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(cipher))
            {
                return defaultValue;
            }

            try
            {
                return Decrypt(cipher);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to decrypt PlayerPrefs key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        public static void DeleteAll() => PlayerPrefs.DeleteAll();

        public static void Save() => PlayerPrefs.Save();

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs, Encoding.UTF8))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Decrypt(string cipherText)
        {
            var bytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(bytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
