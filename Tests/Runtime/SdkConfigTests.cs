using System;
using NUnit.Framework;
using Technyx.Sdk.Config;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class SdkConfigTests
    {
        [Test]
        public void Defaults_AreValid()
        {
            Assert.DoesNotThrow(() => new SdkConfig().Validate());
        }

        [Test]
        public void Defaults_PointAtTheProductionApi()
        {
            Assert.AreEqual("https://api.prod.technyx.ar/api/v1", new SdkConfig().apiBaseUrl);
        }

        [TestCase("")]
        [TestCase("not a url")]
        [TestCase("api.technyx.ar/api/v1")]
        [TestCase("ftp://api.technyx.ar/api/v1")]
        public void InvalidBaseUrl_IsRejected(string url)
        {
            var config = new SdkConfig { apiBaseUrl = url };

            var error = Assert.Throws<InvalidOperationException>(() => config.Validate());
            StringAssert.Contains("apiBaseUrl", error.Message);
        }

        [TestCase("http://localhost:8123/api/v1")]
        [TestCase("https://api.prod.technyx.ar/api/v1")]
        public void HttpAndHttpsUrls_AreAccepted(string url)
        {
            Assert.DoesNotThrow(() => new SdkConfig { apiBaseUrl = url }.Validate());
        }

        [Test]
        public void EmptySalt_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { encryptionSalt = "" }.Validate());
        }

        [Test]
        public void NonPositiveTimeouts_AreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { requestTimeoutSeconds = 0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { streamTimeoutSeconds = 0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { uploadTimeoutSeconds = -1 }.Validate());
        }

        [Test]
        public void NegativeRetryValues_AreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { maxRetries = -1 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { retryBaseDelaySeconds = -1f }.Validate());
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { maxRetryDelaySeconds = -1 }.Validate());
        }

        [Test]
        public void ZeroRetries_AreAllowed()
        {
            Assert.DoesNotThrow(() => new SdkConfig { maxRetries = 0 }.Validate());
        }

        [Test]
        public void RefreshRetrySettings_MustBePositive()
        {
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { refreshRetryBaseSeconds = 0f }.Validate());
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { refreshRetryMaxSeconds = 0 }.Validate());
        }

        [Test]
        public void Heartbeat_IsOffByDefaultWithAFiveMinuteInterval()
        {
            var config = new SdkConfig();

            Assert.IsFalse(config.autoHeartbeat);
            Assert.AreEqual(300, config.heartbeatIntervalSeconds);
        }

        [TestCase(59, false)]
        [TestCase(60, true)]
        [TestCase(3600, true)]
        [TestCase(3601, false)]
        public void HeartbeatInterval_FollowsTheBackendRange(int seconds, bool valid)
        {
            var config = new SdkConfig { heartbeatIntervalSeconds = seconds };

            if (valid)
                Assert.DoesNotThrow(() => config.Validate());
            else
                StringAssert.Contains("heartbeatIntervalSeconds", Assert.Throws<InvalidOperationException>(() => config.Validate()).Message);
        }

        [Test]
        public void NegativeRefreshMargin_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new SdkConfig { tokenRefreshMarginSeconds = -1 }.Validate());
        }

        [Test]
        public void AllProblems_AreReportedTogether()
        {
            var config = new SdkConfig { apiBaseUrl = "nope", requestTimeoutSeconds = 0 };

            var error = Assert.Throws<InvalidOperationException>(() => config.Validate());

            StringAssert.Contains("apiBaseUrl", error.Message);
            StringAssert.Contains("requestTimeoutSeconds", error.Message);
        }

        [Test]
        public void JsonWithoutNewFields_KeepsTheirDefaults()
        {
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<SdkConfig>(
                "{\"apiBaseUrl\":\"https://x.example/api/v1\",\"tokenRefreshMarginSeconds\":300,\"encryptionSalt\":\"s\",\"requestTimeoutSeconds\":30}");

            Assert.AreEqual(2, config.maxRetries);
            Assert.AreEqual(120, config.uploadTimeoutSeconds);
            Assert.DoesNotThrow(() => config.Validate());
        }
    }
}
