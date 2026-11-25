using System;
using NUnit.Framework;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class RetryPolicyTests
    {
        private SdkConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new SdkConfig { maxRetries = 2, retryBaseDelaySeconds = 1f, maxRetryDelaySeconds = 30 };
        }

        private TimeSpan? Next(string method, int retries, int status, bool transport = false, int? retryAfter = null, bool stream = false)
        {
            return RetryPolicy.NextDelay(method, retries, status, transport, retryAfter, stream, _config);
        }

        [TestCase("GET")]
        [TestCase("PUT")]
        [TestCase("PATCH")]
        [TestCase("DELETE")]
        public void IdempotentMethod_RetriesTransportFailure(string method)
        {
            Assert.IsNotNull(Next(method, 0, 0, transport: true));
        }

        [Test]
        public void Post_NeverRetriesTransportFailure()
        {
            Assert.IsNull(Next("POST", 0, 0, transport: true));
        }

        [TestCase(502)]
        [TestCase(503)]
        [TestCase(504)]
        public void GatewayErrors_RetriedForGetOnly(int status)
        {
            Assert.IsNotNull(Next("GET", 0, status));
            Assert.IsNull(Next("POST", 0, status));
        }

        [TestCase(500)]
        [TestCase(400)]
        [TestCase(404)]
        [TestCase(422)]
        public void OtherErrors_AreNotRetried(int status)
        {
            Assert.IsNull(Next("GET", 0, status));
        }

        [Test]
        public void TooManyRequests_IsRetriedForPost()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(4), Next("POST", 0, 429, retryAfter: 4));
        }

        [Test]
        public void TooManyRequests_WithoutRetryAfter_UsesBackoff()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(1), Next("POST", 0, 429));
        }

        [Test]
        public void TooManyRequests_WithRetryAfterAboveCap_IsNotRetried()
        {
            Assert.IsNull(Next("GET", 0, 429, retryAfter: 31));
        }

        [Test]
        public void Stream_RetriesOnlyTooManyRequests()
        {
            Assert.IsNull(Next("POST", 0, 503, stream: true));
            Assert.IsNull(Next("POST", 0, 0, transport: true, stream: true));
            Assert.IsNotNull(Next("POST", 0, 429, retryAfter: 1, stream: true));
        }

        [Test]
        public void RetriesAreBounded()
        {
            Assert.IsNotNull(Next("GET", 1, 503));
            Assert.IsNull(Next("GET", 2, 503));
        }

        [Test]
        public void Backoff_DoublesAndIsCapped()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(1), RetryPolicy.Backoff(0, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(2), RetryPolicy.Backoff(1, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(4), RetryPolicy.Backoff(2, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(30), RetryPolicy.Backoff(10, _config));
        }

        [Test]
        public void ZeroMaxRetries_DisablesRetrying()
        {
            _config.maxRetries = 0;

            Assert.IsNull(Next("GET", 0, 503));
            Assert.IsNull(Next("POST", 0, 429, retryAfter: 1));
        }
    }
}
