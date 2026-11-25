using System;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using UnityEngine;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class TokenStorageTests
    {
        private const string Prefix = "_tnxtest_storage";

        private TokenStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new TokenStorage("test-salt", Prefix);
            _storage.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Clear();
        }

        [Test]
        public void Load_WhenEmpty_ReturnsNull()
        {
            Assert.IsNull(_storage.Load());
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var token = "test-access-token-abc123";
            var expiry = DateTime.UtcNow.AddHours(24);

            _storage.Save(token, expiry);
            var loaded = _storage.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(token, loaded.Token);
            Assert.That(loaded.ExpiresAt, Is.EqualTo(expiry).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Load_ReturnsExpiryAsUtc()
        {
            _storage.Save("utc-token", DateTime.UtcNow.AddHours(24));

            Assert.AreEqual(DateTimeKind.Utc, _storage.Load().ExpiresAt.Kind);
        }

        [Test]
        public void Save_LocalExpiry_IsStoredAsTheSameInstant()
        {
            var local = DateTime.Now.AddHours(5);

            _storage.Save("local-token", local);

            Assert.That(_storage.Load().ExpiresAt, Is.EqualTo(local.ToUniversalTime()).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Save_UnspecifiedExpiry_IsTreatedAsUtc()
        {
            var unspecified = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(5), DateTimeKind.Unspecified);

            _storage.Save("unspecified-token", unspecified);

            Assert.That(_storage.Load().ExpiresAt, Is.EqualTo(unspecified).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Save_OverwritesPrevious()
        {
            _storage.Save("first-token", DateTime.UtcNow.AddHours(1));
            _storage.Save("second-token", DateTime.UtcNow.AddHours(2));

            Assert.AreEqual("second-token", _storage.Load().Token);
        }

        [Test]
        public void Clear_RemovesStoredToken()
        {
            _storage.Save("some-token", DateTime.UtcNow.AddHours(1));
            _storage.Clear();

            Assert.IsNull(_storage.Load());
        }

        [Test]
        public void Load_ExpiredToken_ReturnsNullAndClears()
        {
            _storage.Save("expired-token", DateTime.UtcNow.AddSeconds(-1));

            Assert.IsNull(_storage.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_tk"));
        }

        [Test]
        public void Save_DoesNotStoreThePlainToken()
        {
            _storage.Save("plain-secret-token", DateTime.UtcNow.AddHours(1));

            Assert.That(PlayerPrefs.GetString(Prefix + "_tk"), Does.Not.Contain("plain-secret-token"));
        }

        [Test]
        public void Save_SameToken_ProducesDifferentCiphertext()
        {
            var expiry = DateTime.UtcNow.AddHours(1);

            _storage.Save("same-token", expiry);
            var first = PlayerPrefs.GetString(Prefix + "_tk");
            _storage.Save("same-token", expiry);
            var second = PlayerPrefs.GetString(Prefix + "_tk");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void DifferentSalt_CannotRead_AndTheValueIsDiscarded()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));

            var other = new TokenStorage("different-salt", Prefix);

            Assert.IsNull(other.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_tk"));
        }

        [Test]
        public void Tampered_Ciphertext_IsRejectedAndDiscarded()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));
            var raw = Convert.FromBase64String(PlayerPrefs.GetString(Prefix + "_tk"));
            raw[20] ^= 0x01;
            PlayerPrefs.SetString(Prefix + "_tk", Convert.ToBase64String(raw));

            Assert.IsNull(_storage.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_tk"));
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_te"));
        }

        [Test]
        public void Tampered_Mac_IsRejected()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));
            var raw = Convert.FromBase64String(PlayerPrefs.GetString(Prefix + "_tk"));
            raw[raw.Length - 1] ^= 0x80;
            PlayerPrefs.SetString(Prefix + "_tk", Convert.ToBase64String(raw));

            Assert.IsNull(_storage.Load());
        }

        [Test]
        public void Truncated_Value_IsRejected()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));
            var raw = Convert.FromBase64String(PlayerPrefs.GetString(Prefix + "_tk"));
            Array.Resize(ref raw, 20);
            PlayerPrefs.SetString(Prefix + "_tk", Convert.ToBase64String(raw));

            Assert.IsNull(_storage.Load());
        }

        [Test]
        public void SwappingTokenAndExpiryValues_IsRejected()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));
            var token = PlayerPrefs.GetString(Prefix + "_tk");
            var expiry = PlayerPrefs.GetString(Prefix + "_te");
            PlayerPrefs.SetString(Prefix + "_tk", expiry);
            PlayerPrefs.SetString(Prefix + "_te", token);

            Assert.IsNull(_storage.Load());
        }

        [Test]
        public void NotBase64_IsRejected()
        {
            PlayerPrefs.SetString(Prefix + "_tk", "%%% not base64 %%%");
            PlayerPrefs.SetString(Prefix + "_te", "%%% not base64 %%%");

            Assert.IsNull(_storage.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_tk"));
        }

        [Test]
        public void PreviousUnauthenticatedFormat_IsDiscarded()
        {
            var legacyLike = Convert.ToBase64String(new byte[48]);
            PlayerPrefs.SetString(Prefix + "_tk", legacyLike);
            PlayerPrefs.SetString(Prefix + "_te", legacyLike);

            Assert.IsNull(_storage.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "_tk"));
        }

        [Test]
        public void DifferentPrefixes_DoNotShareState()
        {
            var other = new TokenStorage("test-salt", Prefix + "_other");
            other.Clear();

            _storage.Save("one", DateTime.UtcNow.AddHours(1));

            Assert.IsNull(other.Load());
            Assert.AreEqual("one", _storage.Load().Token);
        }
    }
}
