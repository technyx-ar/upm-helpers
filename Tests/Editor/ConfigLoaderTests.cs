using NUnit.Framework;
using Technyx.One.Config;

namespace Technyx.One.Tests.Editor
{
    [TestFixture]
    public class ConfigLoaderTests
    {
        [TearDown]
        public void TearDown()
        {
            OneConfigLoader.Reset();
        }

        [Test]
        public void Load_ReturnsConfig()
        {
            var config = OneConfigLoader.Load();
            Assert.IsNotNull(config);
            Assert.IsFalse(string.IsNullOrEmpty(config.apiBaseUrl));
        }

        [Test]
        public void Load_DefaultValues_AreReasonable()
        {
            var config = OneConfigLoader.Load();
            Assert.Greater(config.tokenRefreshMarginSeconds, 0);
            Assert.Greater(config.requestTimeoutSeconds, 0);
            Assert.IsFalse(string.IsNullOrEmpty(config.encryptionSalt));
        }

        [Test]
        public void Override_ReplacesConfig()
        {
            var custom = new OneConfig { apiBaseUrl = "https://custom.example.com" };
            OneConfigLoader.Override(custom);

            var loaded = OneConfigLoader.Load();
            Assert.AreEqual("https://custom.example.com", loaded.apiBaseUrl);
        }

        [Test]
        public void Reset_ClearsCache()
        {
            var custom = new OneConfig { apiBaseUrl = "https://override.example.com" };
            OneConfigLoader.Override(custom);
            OneConfigLoader.Reset();

            var loaded = OneConfigLoader.Load();
            // After reset, reloads from Resources (default), not the override
            Assert.AreNotEqual("https://override.example.com", loaded.apiBaseUrl);
        }
    }
}
