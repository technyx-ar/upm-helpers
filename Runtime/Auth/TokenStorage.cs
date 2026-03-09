using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Technyx.One.Auth
{
    public class TokenStorage
    {
        private const string TokenKey = "_tnx_tk";
        private const string ExpiryKey = "_tnx_te";

        private readonly byte[] _encryptionKey;

        public TokenStorage(string salt)
        {
            var source = SystemInfo.deviceUniqueIdentifier + salt;
            using var sha = SHA256.Create();
            _encryptionKey = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        }

        public void Save(string token, DateTime expiresAt)
        {
            PlayerPrefs.SetString(TokenKey, Encrypt(token));
            PlayerPrefs.SetString(ExpiryKey, Encrypt(expiresAt.ToString("O")));
            PlayerPrefs.Save();
        }

        public TokenData Load()
        {
            var encryptedToken = PlayerPrefs.GetString(TokenKey, null);
            var encryptedExpiry = PlayerPrefs.GetString(ExpiryKey, null);

            if (string.IsNullOrEmpty(encryptedToken) || string.IsNullOrEmpty(encryptedExpiry))
                return null;

            try
            {
                var token = Decrypt(encryptedToken);
                var expiry = DateTime.Parse(Decrypt(encryptedExpiry));

                if (expiry <= DateTime.UtcNow)
                {
                    Clear();
                    return null;
                }

                return new TokenData { Token = token, ExpiresAt = expiry };
            }
            catch
            {
                Clear();
                return null;
            }
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(ExpiryKey);
            PlayerPrefs.Save();
        }

        private string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        private string Decrypt(string cipherText)
        {
            var fullBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullBytes.Length - iv.Length];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullBytes, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
