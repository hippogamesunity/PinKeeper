using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Common
{
    /// <summary>
    /// AES (Advanced Encryption Standard) implementation with 128-bit key (default)
    /// - 128-bit AES is approved  by NIST, but not the 256-bit AES
    /// - 256-bit AES is slower than the 128-bit AES (by about 40%)
    /// - Use it for secure data protection
    /// - Do NOT use it for data protection in RAM (in most common scenarios)
    /// </summary>
    public static class AES
    {
        public static int KeyLength = 128;

        public static string SaltKey
        {
            get
            {
                return PlayerPrefs.HasKey("SK") ? PlayerPrefs.GetString("SK") : "ShMG8hLyZ7k~Ge5@";
            }
            set
            {
                PlayerPrefs.SetString("SaltKey", value);
                PlayerPrefs.Save();
            }
        }

        public static string VIKey
        {
            get
            {
                return PlayerPrefs.HasKey("VI") ? PlayerPrefs.GetString("VI") : "~6YUi0Sv5@|{aOZO";
            }
            set
            {
                PlayerPrefs.SetString("SaltKey", value);
                PlayerPrefs.Save();
            }
        }

        public static void Initialize()
        {
            SaltKey = Md5.Encode(Convert.ToString(CRandom.GetRandom(999999))).Substring(0, 16);
            VIKey = Md5.Encode(Convert.ToString(CRandom.GetRandom(999999))).Substring(0, 16);
        }

        public static string Encrypt(byte[] value, string password)
        {
            var keyBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(KeyLength / 8);
            var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(value, 0, value.Length);
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Close();
                    memoryStream.Close();

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string Encrypt(string value, string password)
        {
            return Encrypt(Encoding.UTF8.GetBytes(value), password);
        }

        public static ProtectedValue Encrypt(ProtectedValue value, ProtectedValue password)
        {
            return new ProtectedValue(Encrypt(Encoding.UTF8.GetBytes(value.String), password.String));
        }

        public static string Decrypt(string value, string password)
        {
            Debug.Log($"Decrypt={value.Substring(0, 10)}, password={password}");

            var cipherTextBytes = Convert.FromBase64String(value);
            var keyBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(KeyLength / 8);
            var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None };
            var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

            using (var memoryStream = new MemoryStream(cipherTextBytes))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    var plainTextBytes = new byte[cipherTextBytes.Length];
                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

                    memoryStream.Close();
                    cryptoStream.Close();

                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
                }
            }
        }

        public static ProtectedValue Decrypt(ProtectedValue value, ProtectedValue password)
        {
            return new ProtectedValue(Decrypt(value.String, password.String));
        }
    }
}