using System;
using NUnit.Framework;
using Technyx.One.Auth;

namespace Technyx.One.Tests
{
    [TestFixture]
    public class TokenStorageTests
    {
        private TokenStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new TokenStorage("test-salt");
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
            var result = _storage.Load();
            Assert.IsNull(result);
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
        public void Save_OverwritesPrevious()
        {
            _storage.Save("first-token", DateTime.UtcNow.AddHours(1));
            _storage.Save("second-token", DateTime.UtcNow.AddHours(2));

            var loaded = _storage.Load();
            Assert.AreEqual("second-token", loaded.Token);
        }

        [Test]
        public void Clear_RemovesStoredToken()
        {
            _storage.Save("some-token", DateTime.UtcNow.AddHours(1));
            _storage.Clear();

            var loaded = _storage.Load();
            Assert.IsNull(loaded);
        }

        [Test]
        public void Load_ExpiredToken_ReturnsNull()
        {
            _storage.Save("expired-token", DateTime.UtcNow.AddSeconds(-1));

            var loaded = _storage.Load();
            Assert.IsNull(loaded);
        }

        [Test]
        public void DifferentSalt_CannotDecrypt()
        {
            _storage.Save("secret-token", DateTime.UtcNow.AddHours(1));

            var otherStorage = new TokenStorage("different-salt");
            var loaded = otherStorage.Load();

            // Should fail to decrypt and return null (clears corrupt data)
            Assert.IsNull(loaded);
        }
    }
}
