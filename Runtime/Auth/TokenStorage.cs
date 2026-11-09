using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Technyx.Sdk.Auth
{
    // The key is derived from the device identifier and the configured salt, and both are readable on
    // the device. The MAC detects a damaged or edited value; it does not protect the token from
    // someone who controls the device or the app.
    public class TokenStorage
    {
        private const byte FormatVersion = 2;
        private const int IvLength = 16;
        private const int MacLength = 32;
        private const string TokenPurpose = "token";
        private const string ExpiryPurpose = "expiry";

        private readonly string _tokenKey;
        private readonly string _expiryKey;
        private readonly byte[] _encryptionKey;
        private readonly byte[] _macKey;

        public TokenStorage(string salt, string keyPrefix = "_tnx")
        {
            _tokenKey = keyPrefix + "_tk";
            _expiryKey = keyPrefix + "_te";

            byte[] master;
            using (var sha = SHA256.Create())
                master = sha.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier + salt));

            _encryptionKey = Derive(master, "enc");
            _macKey = Derive(master, "mac");
        }

        public void Save(string token, DateTime expiresAt)
        {
            PlayerPrefs.SetString(_tokenKey, Protect(TokenPurpose, token));
            PlayerPrefs.SetString(_expiryKey, Protect(ExpiryPurpose, ToUtc(expiresAt).ToString("O", CultureInfo.InvariantCulture)));
            PlayerPrefs.Save();
        }

        public TokenData Load()
        {
            var storedToken = PlayerPrefs.GetString(_tokenKey, null);
            var storedExpiry = PlayerPrefs.GetString(_expiryKey, null);

            if (string.IsNullOrEmpty(storedToken) || string.IsNullOrEmpty(storedExpiry))
                return null;

            string token;
            DateTime expiry;
            try
            {
                token = Unprotect(TokenPurpose, storedToken);
                expiry = DateTime.Parse(Unprotect(ExpiryPurpose, storedExpiry), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            catch (Exception e) when (e is CryptographicException || e is FormatException || e is ArgumentException)
            {
                Debug.LogWarning("[Technyx.Sdk] The stored session could not be verified and was discarded; a new login is required. " + e.Message);
                Clear();
                return null;
            }

            if (expiry <= DateTime.UtcNow)
            {
                Clear();
                return null;
            }

            return new TokenData { Token = token, ExpiresAt = expiry };
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_tokenKey);
            PlayerPrefs.DeleteKey(_expiryKey);
            PlayerPrefs.Save();
        }

        private static DateTime ToUtc(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc: return value;
                case DateTimeKind.Local: return value.ToUniversalTime();
                default: return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
        }

        private static byte[] Derive(byte[] master, string label)
        {
            using (var hmac = new HMACSHA256(master))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(label));
        }

        private string Protect(string purpose, string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipher;
                using (var encryptor = aes.CreateEncryptor())
                    cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                var payload = new byte[1 + IvLength + cipher.Length + MacLength];
                payload[0] = FormatVersion;
                Buffer.BlockCopy(aes.IV, 0, payload, 1, IvLength);
                Buffer.BlockCopy(cipher, 0, payload, 1 + IvLength, cipher.Length);

                var mac = ComputeMac(purpose, payload, payload.Length - MacLength);
                Buffer.BlockCopy(mac, 0, payload, payload.Length - MacLength, MacLength);

                return Convert.ToBase64String(payload);
            }
        }

        private string Unprotect(string purpose, string stored)
        {
            var payload = Convert.FromBase64String(stored);

            if (payload.Length < 1 + IvLength + 16 + MacLength || payload[0] != FormatVersion)
                throw new CryptographicException("Unknown stored session format.");

            var macOffset = payload.Length - MacLength;
            var expected = ComputeMac(purpose, payload, macOffset);
            var actual = new byte[MacLength];
            Buffer.BlockCopy(payload, macOffset, actual, 0, MacLength);

            if (!FixedTimeEquals(expected, actual))
                throw new CryptographicException("The stored session failed its integrity check.");

            var iv = new byte[IvLength];
            Buffer.BlockCopy(payload, 1, iv, 0, IvLength);

            var cipherLength = macOffset - 1 - IvLength;
            var cipher = new byte[cipherLength];
            Buffer.BlockCopy(payload, 1 + IvLength, cipher, 0, cipherLength);

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                    return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipher, 0, cipher.Length));
            }
        }

        private byte[] ComputeMac(string purpose, byte[] data, int count)
        {
            var label = Encoding.UTF8.GetBytes(purpose + "\n");

            using (var hmac = new HMACSHA256(_macKey))
            {
                hmac.TransformBlock(label, 0, label.Length, null, 0);
                hmac.TransformFinalBlock(data, 0, count);
                return hmac.Hash;
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];

            return difference == 0;
        }
    }
}
