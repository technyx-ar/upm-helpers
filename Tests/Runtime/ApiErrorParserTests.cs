using NUnit.Framework;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class ApiErrorParserTests
    {
        [Test]
        public void ErrorObject_KeepsBackendCodeMessageAndDetails()
        {
            var body = "{\"error\":{\"code\":\"AI_BUDGET_EXCEEDED\",\"message\":\"Budget exceeded.\","
                + "\"details\":{\"budget_cents\":500,\"used_cents\":512,\"resets_at\":\"2026-12-01T00:00:00Z\"}}}";

            var error = ApiErrorParser.Parse(403, body, null);

            Assert.AreEqual("AI_BUDGET_EXCEEDED", error.Code);
            Assert.AreEqual("Budget exceeded.", error.Message);
            Assert.AreEqual(500, (int)error.Details["budget_cents"]);
            Assert.AreEqual("2026-12-01T00:00:00Z", (string)error.Details["resets_at"]);
        }

        [Test]
        public void RateLimit_ReadsRetryAfterFromBody()
        {
            var body = "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"Slow down.\",\"retry_after\":12}}";

            var error = ApiErrorParser.Parse(429, body, null);

            Assert.AreEqual("AI_RATE_LIMIT", error.Code);
            Assert.AreEqual(12, error.RetryAfterSeconds);
        }

        [Test]
        public void RateLimit_FallsBackToRetryAfterHeader()
        {
            var error = ApiErrorParser.Parse(429, "{\"message\":\"Too Many Attempts.\"}", "7");

            Assert.AreEqual(ApiErrorCodes.RateLimited, error.Code);
            Assert.AreEqual(7, error.RetryAfterSeconds);
        }

        [Test]
        public void BodyRetryAfter_WinsOverHeader()
        {
            var body = "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"m\",\"retry_after\":3}}";

            Assert.AreEqual(3, ApiErrorParser.Parse(429, body, "30").RetryAfterSeconds);
        }

        [TestCase("Wed, 21 Oct 2026 07:28:00 GMT")]
        [TestCase("-5")]
        [TestCase("")]
        public void RetryAfterHeader_NotDeltaSeconds_IsIgnored(string header)
        {
            Assert.IsNull(ApiErrorParser.ParseRetryAfter(header));
        }

        [Test]
        public void ValidationError_ExposesFieldMessages()
        {
            var body = "{\"message\":\"The given data was invalid.\",\"errors\":{\"name\":[\"The name field is required.\"],"
                + "\"settings.inactivity_timeout\":[\"Too low.\",\"Must be an integer.\"]}}";

            var error = ApiErrorParser.Parse(422, body, null);

            Assert.AreEqual(ApiErrorCodes.ValidationError, error.Code);
            Assert.AreEqual("The given data was invalid.", error.Message);
            Assert.IsTrue(error.HasValidationErrors);
            Assert.AreEqual("The name field is required.", error.GetFieldErrors("name")[0]);
            Assert.AreEqual(2, error.GetFieldErrors("settings.inactivity_timeout").Count);
            Assert.AreEqual(0, error.GetFieldErrors("unknown").Count);
        }

        [Test]
        public void SuccessFalseShape_UsesMessage()
        {
            var error = ApiErrorParser.Parse(404, "{\"success\":false,\"message\":\"Photo not found.\"}", null);

            Assert.AreEqual(ApiErrorCodes.NotFound, error.Code);
            Assert.AreEqual("Photo not found.", error.Message);
        }

        [TestCase(400, "BAD_REQUEST")]
        [TestCase(401, "UNAUTHENTICATED")]
        [TestCase(403, "FORBIDDEN")]
        [TestCase(404, "NOT_FOUND")]
        [TestCase(409, "CONFLICT")]
        [TestCase(413, "PAYLOAD_TOO_LARGE")]
        [TestCase(422, "VALIDATION_ERROR")]
        [TestCase(429, "RATE_LIMITED")]
        [TestCase(500, "SERVER_ERROR")]
        [TestCase(503, "SERVER_ERROR")]
        [TestCase(418, "UNKNOWN")]
        public void NonJsonBody_GetsStatusDerivedCode(int status, string expected)
        {
            var error = ApiErrorParser.Parse(status, "<html>nginx</html>", null);

            Assert.AreEqual(expected, error.Code);
            Assert.That(error.Message, Does.Contain(status.ToString()));
        }

        [Test]
        public void EmptyBody_GetsStatusDerivedCode()
        {
            var error = ApiErrorParser.Parse(502, "", null);

            Assert.AreEqual(ApiErrorCodes.ServerError, error.Code);
        }

        [Test]
        public void JsonArrayBody_IsNotMistakenForAnErrorObject()
        {
            var error = ApiErrorParser.Parse(500, "[1,2]", null);

            Assert.AreEqual(ApiErrorCodes.ServerError, error.Code);
        }

        [Test]
        public void ErrorObjectWithoutCode_KeepsStatusDerivedCode()
        {
            var error = ApiErrorParser.Parse(403, "{\"error\":{\"message\":\"No organization.\"}}", null);

            Assert.AreEqual(ApiErrorCodes.Forbidden, error.Code);
            Assert.AreEqual("No organization.", error.Message);
        }

        [Test]
        public void ErrorAsPlainString_IsUsedAsMessage()
        {
            var error = ApiErrorParser.Parse(400, "{\"error\":\"Bad thing\"}", null);

            Assert.AreEqual("Bad thing", error.Message);
        }
    }
}
