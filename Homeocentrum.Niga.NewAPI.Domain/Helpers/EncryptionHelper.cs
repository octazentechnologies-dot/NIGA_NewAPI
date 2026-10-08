using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// AES encrypt/decrypt helper (compatible with legacy Homeocentrum activation links).
    /// </summary>
    public static class EncryptionHelper
    {
        private const string EncryptionKey = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly byte[] Salt =
        {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        };

        public static string Encrypt(string encryptString)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(encryptString);
            using (Aes encryptor = Aes.Create())
            {
                (encryptor.Key, encryptor.IV) = DeriveKeyAndIv();
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encryptString = Convert.ToBase64String(ms.ToArray());
                }
            }
            return encryptString;
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    (encryptor.Key, encryptor.IV) = DeriveKeyAndIv();
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception)
            {
                // Keep legacy behaviour: return original text on decrypt failure.
            }
            return cipherText;
        }

        // Existing activation links need the old Rfc2898DeriveBytes defaults: SHA1, 1000 iterations, key then IV.
        private static (byte[] Key, byte[] IV) DeriveKeyAndIv()
        {
            var bytes = Rfc2898DeriveBytes.Pbkdf2(EncryptionKey, Salt, 1000, HashAlgorithmName.SHA1, 48);
            return (bytes[..32], bytes[32..]);
        }
    }
}
