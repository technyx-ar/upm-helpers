using System;
using NUnit.Framework;
using Technyx.Sdk.Config;

namespace Technyx.Sdk.Tests.Editor
{
    [TestFixture]
    public class ConfigLoaderTests
    {
        [SetUp]
        public void SetUp()
        {
            SdkConfigLoader.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SdkConfigLoader.Reset();
        }

        [Test]
        public void Load_ReturnsAValidConfig()
        {
            var config = SdkConfigLoader.Load();

            Assert.IsNotNull(config);
            Assert.IsFalse(string.IsNullOrEmpty(config.apiBaseUrl));
            Assert.DoesNotThrow(() => config.Validate());
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
        public void Load_ReturnsTheSameInstanceUntilReset()
        {
            Assert.AreSame(SdkConfigLoader.Load(), SdkConfigLoader.Load());
        }

        [Test]
        public void Override_BeforeInitialization_ReplacesConfig()
        {
            SdkConfigLoader.Override(new SdkConfig { apiBaseUrl = "https://custom.example.com" });

            Assert.AreEqual("https://custom.example.com", SdkConfigLoader.Load().apiBaseUrl);
        }

        [Test]
        public void Override_InvalidConfig_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => SdkConfigLoader.Override(new SdkConfig { apiBaseUrl = "nope" }));
        }

        [Test]
        public void Override_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SdkConfigLoader.Override(null));
        }

        [Test]
        public void Reset_ClearsTheOverride()
        {
            SdkConfigLoader.Override(new SdkConfig { apiBaseUrl = "https://override.example.com" });
            SdkConfigLoader.Reset();

            Assert.AreNotEqual("https://override.example.com", SdkConfigLoader.Load().apiBaseUrl);
        }
    }
}
