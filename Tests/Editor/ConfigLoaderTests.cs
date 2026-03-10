using NUnit.Framework;
using Technyx.Sdk.Config;

namespace Technyx.Sdk.Tests.Editor
{
    [TestFixture]
    public class ConfigLoaderTests
    {
        [TearDown]
        public void TearDown()
        {
            SdkConfigLoader.Reset();
        }

        [Test]
        public void Load_ReturnsConfig()
        {
            var config = SdkConfigLoader.Load();
            Assert.IsNotNull(config);
            Assert.IsFalse(string.IsNullOrEmpty(config.apiBaseUrl));
        }

        [Test]
        public void Load_DefaultValues_AreReasonable()
        {
            var config = SdkConfigLoader.Load();
            Assert.Greater(config.tokenRefreshMarginSeconds, 0);
            Assert.Greater(config.requestTimeoutSeconds, 0);
            Assert.IsFalse(string.IsNullOrEmpty(config.encryptionSalt));
        }

        [Test]
        public void Override_ReplacesConfig()
        {
            var custom = new SdkConfig { apiBaseUrl = "https://custom.example.com" };
            SdkConfigLoader.Override(custom);

            var loaded = SdkConfigLoader.Load();
            Assert.AreEqual("https://custom.example.com", loaded.apiBaseUrl);
        }

        [Test]
        public void Reset_ClearsCache()
        {
            var custom = new SdkConfig { apiBaseUrl = "https://override.example.com" };
            SdkConfigLoader.Override(custom);
            SdkConfigLoader.Reset();

            var loaded = SdkConfigLoader.Load();
            // After reset, reloads from Resources (default), not the override
            Assert.AreNotEqual("https://override.example.com", loaded.apiBaseUrl);
        }
    }
}
