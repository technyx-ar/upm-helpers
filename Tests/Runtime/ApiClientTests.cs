using System;
using Newtonsoft.Json;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class ApiClientTests
    {
        [TestCase("auth/login")]
        [TestCase("/auth/login")]
        [TestCase("auth/refresh")]
        [TestCase("Auth/Refresh")]
        [TestCase("auth/logout")]
        public void ShouldRefreshOn401_AuthEndpoints_ReturnsFalse(string path)
        {
            Assert.IsFalse(ApiClient.ShouldRefreshOn401(path, hasToken: true));
        }

        [TestCase("devices")]
        [TestCase("/devices/abc/heartbeat")]
        [TestCase("auth/user")]
        public void ShouldRefreshOn401_ProtectedEndpointWithToken_ReturnsTrue(string path)
        {
            Assert.IsTrue(ApiClient.ShouldRefreshOn401(path, hasToken: true));
        }

        [Test]
        public void ShouldRefreshOn401_WithoutToken_ReturnsFalse()
        {
            Assert.IsFalse(ApiClient.ShouldRefreshOn401("devices", hasToken: false));
        }

        [Test]
        public void DeserializeData_UnwrapsDataEnvelope()
        {
            var json = "{\"data\":{\"token\":\"abc\",\"token_type\":\"Bearer\",\"expires_at\":\"2026-11-10T10:00:00+00:00\"}}";

            var result = ApiClient.DeserializeData<RefreshResponse>(json);

            Assert.AreEqual("abc", result.Token);
            Assert.AreEqual("Bearer", result.TokenType);
        }

        [Test]
        public void DeserializeData_WithoutEnvelope_ReadsRootObject()
        {
            var result = ApiClient.DeserializeData<RefreshResponse>("{\"token\":\"root\"}");

            Assert.AreEqual("root", result.Token);
        }

        [Test]
        public void DeserializeData_KeepsIsoTimestampStringsVerbatim()
        {
            var result = ApiClient.DeserializeData<DeviceData>("{\"data\":{\"id\":\"01D\",\"created_at\":\"2026-12-01T10:30:00.000000Z\"}}");

            Assert.AreEqual("2026-12-01T10:30:00.000000Z", result.CreatedAt);
        }

        [Test]
        public void DeserializeData_NonPaginatedType_IgnoresMetaAtTheRoot()
        {
            var result = ApiClient.DeserializeData<RefreshResponse>("{\"data\":{\"token\":\"inner\"},\"meta\":{\"x\":1}}");

            Assert.AreEqual("inner", result.Token);
        }

        [Test]
        public void DeserializeData_InvalidJson_Throws()
        {
            Assert.Throws<JsonReaderException>(() => ApiClient.DeserializeData<RefreshResponse>("<html>502</html>"));
        }

        [Test]
        public void TryParseExpiry_Iso8601WithOffset_ReturnsUtc()
        {
            var ok = AuthService.TryParseExpiry("2026-11-10T12:00:00+02:00", out var utc);

            Assert.IsTrue(ok);
            Assert.AreEqual(new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc), utc);
        }

        [Test]
        public void TryParseExpiry_WithoutOffset_IsTreatedAsUtc()
        {
            var ok = AuthService.TryParseExpiry("2026-11-10T12:00:00", out var utc);

            Assert.IsTrue(ok);
            Assert.AreEqual(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc), utc);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("not a date")]
        public void TryParseExpiry_Invalid_ReturnsFalse(string value)
        {
            Assert.IsFalse(AuthService.TryParseExpiry(value, out _));
        }
    }
}
