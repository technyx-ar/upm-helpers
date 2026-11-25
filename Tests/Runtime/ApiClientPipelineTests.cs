using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class ApiClientPipelineTests
    {
        private SdkConfig _config;
        private TokenStorage _storage;
        private FakeHttpTransport _transport;
        private ApiClient _client;

        [SetUp]
        public void SetUp()
        {
            _config = new SdkConfig
            {
                apiBaseUrl = "https://api.test/api/v1",
                encryptionSalt = "test-salt",
                maxRetries = 2,
                retryBaseDelaySeconds = 0f,
                maxRetryDelaySeconds = 5,
            };

            _storage = new TokenStorage(_config.encryptionSalt, "_tnxtest_pipeline");
            _storage.Clear();
            _transport = new FakeHttpTransport();
            _client = new ApiClient(_config, _storage, _transport);
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Clear();
        }

        private void SignIn(string token = "tok-1")
        {
            _storage.Save(token, System.DateTime.UtcNow.AddHours(1));
        }

        [Test]
        public async Task Get_UnwrapsEnvelopeAndSendsBearerToken()
        {
            SignIn();
            _transport.Enqueue(200, "{\"success\":true,\"data\":{\"token\":\"abc\"}}");

            var response = await _client.GetAsync<RefreshResponse>("auth/user");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("abc", response.Data.Token);
            Assert.AreEqual("GET", _transport.Requests[0].Method);
            Assert.AreEqual("https://api.test/api/v1/auth/user", _transport.Requests[0].Url);
            Assert.AreEqual("tok-1", _transport.Requests[0].BearerToken);
            Assert.AreEqual(_config.requestTimeoutSeconds, _transport.Requests[0].TimeoutSeconds);
        }

        [Test]
        public async Task Get_WithoutStoredToken_SendsNoAuthorization()
        {
            _transport.Enqueue(200, "{\"data\":{}}");

            await _client.GetAsync<RefreshResponse>("devices");

            Assert.IsNull(_transport.Requests[0].BearerToken);
        }

        [Test]
        public async Task Post_SendsJsonBodyAndContentType()
        {
            _transport.Enqueue(200, "{\"data\":{\"token\":\"x\"}}");

            await _client.PostAsync<RefreshResponse>("auth/login", new { email = "a@b.c" });

            Assert.AreEqual("application/json", _transport.Requests[0].ContentType);
            Assert.AreEqual("{\"email\":\"a@b.c\"}", Encoding.UTF8.GetString(_transport.Requests[0].Body));
        }

        [Test]
        public async Task Post_WithoutBody_SendsNoContentType()
        {
            _transport.Enqueue(204, "");

            await _client.PostAsync<object>("auth/logout");

            Assert.IsNull(_transport.Requests[0].Body);
            Assert.IsNull(_transport.Requests[0].ContentType);
        }

        [Test]
        public async Task NoContent_IsSuccessWithoutData()
        {
            _transport.Enqueue(204, "");

            var response = await _client.DeleteAsync("devices/abc");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(204, response.StatusCode);
            Assert.IsNull(response.Data);
        }

        [Test]
        public async Task Paginated_InsideEnvelope_IsRead()
        {
            _transport.Enqueue(200, "{\"success\":true,\"data\":{\"data\":[{\"token\":\"a\"}],\"links\":{},\"meta\":{\"current_page\":1,\"last_page\":2,\"per_page\":30,\"total\":31}}}");

            var response = await _client.GetAsync<PaginatedResponse<RefreshResponse>>("x");

            Assert.AreEqual(1, response.Data.Data.Count);
            Assert.IsTrue(response.Data.HasMorePages);
        }

        [Test]
        public async Task Paginated_WithoutEnvelope_IsRead()
        {
            _transport.Enqueue(200, "{\"data\":[{\"token\":\"a\"},{\"token\":\"b\"}],\"links\":{\"first\":\"u\"},\"meta\":{\"current_page\":1,\"last_page\":1,\"per_page\":15,\"total\":2}}");

            var response = await _client.GetAsync<PaginatedResponse<RefreshResponse>>("teams");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(2, response.Data.Data.Count);
            Assert.AreEqual(2, response.Data.Total);
            Assert.IsFalse(response.Data.HasMorePages);
        }

        [Test]
        public async Task SingleResource_WithoutEnvelopeData_StillUnwrapsData()
        {
            _transport.Enqueue(200, "{\"data\":{\"token\":\"t\"}}");

            var response = await _client.GetAsync<RefreshResponse>("teams/1");

            Assert.AreEqual("t", response.Data.Token);
        }

        [Test]
        public async Task InvalidJson_IsAParseErrorNotAnException()
        {
            _transport.Enqueue(200, "<html>oops</html>");

            var response = await _client.GetAsync<RefreshResponse>("x");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
            Assert.AreEqual("<html>oops</html>", response.RawBody);
        }

        [Test]
        public async Task ErrorResponse_IsMappedAndNotRetried()
        {
            _transport.Enqueue(422, "{\"message\":\"Invalid.\",\"errors\":{\"name\":[\"Required.\"]}}");

            var response = await _client.PostAsync<RefreshResponse>("teams", new { });

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(422, response.StatusCode);
            Assert.AreEqual(ApiErrorCodes.ValidationError, response.Error.Code);
            Assert.AreEqual("Required.", response.Error.GetFieldErrors("name")[0]);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Get_OnServiceUnavailable_RetriesAndSucceeds()
        {
            _transport.Enqueue(503, "").Enqueue(503, "").Enqueue(200, "{\"data\":{\"token\":\"ok\"}}");

            var response = await _client.GetAsync<RefreshResponse>("devices/abc");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(3, _transport.Requests.Count);
        }

        [Test]
        public async Task Get_RetriesAreBounded()
        {
            _transport.Enqueue(503, "").Enqueue(503, "").Enqueue(503, "").Enqueue(200, "{}");

            var response = await _client.GetAsync<RefreshResponse>("devices/abc");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(503, response.StatusCode);
            Assert.AreEqual(3, _transport.Requests.Count);
        }

        [Test]
        public async Task Post_OnServiceUnavailable_IsNotRetried()
        {
            _transport.Enqueue(503, "").Enqueue(200, "{}");

            var response = await _client.PostAsync<RefreshResponse>("ai/chat", new { message = "hi" });

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Get_OnTransportError_IsRetried()
        {
            _transport.EnqueueTransportError("Cannot connect").Enqueue(200, "{\"data\":{\"token\":\"ok\"}}");

            var response = await _client.GetAsync<RefreshResponse>("devices");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(2, _transport.Requests.Count);
        }

        [Test]
        public async Task Post_OnTransportError_ReturnsNetworkError_WithoutRetry()
        {
            _transport.EnqueueTransportError("Connection reset").Enqueue(200, "{}");

            var response = await _client.PostAsync<RefreshResponse>("ai/chat", new { message = "hi" });

            Assert.IsFalse(response.IsSuccess);
            Assert.IsTrue(response.IsNetworkError);
            Assert.AreEqual("Connection reset", response.Error.Message);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Post_OnTooManyRequests_WaitsAndRepeats()
        {
            _transport
                .Enqueue(429, "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"Slow\",\"retry_after\":0}}")
                .Enqueue(200, "{\"data\":{\"token\":\"ok\"}}");

            var response = await _client.PostAsync<RefreshResponse>("ai/chat", new { message = "hi" });

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(2, _transport.Requests.Count);
        }

        [Test]
        public async Task TooManyRequests_WithLongRetryAfter_IsSurfaced()
        {
            _transport.Enqueue(429, "{\"error\":{\"code\":\"AI_RATE_LIMIT\",\"message\":\"Slow\",\"retry_after\":60}}");

            var response = await _client.PostAsync<RefreshResponse>("ai/chat", new { message = "hi" });

            Assert.AreEqual("AI_RATE_LIMIT", response.Error.Code);
            Assert.AreEqual(60, response.Error.RetryAfterSeconds);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Unauthorized_RefreshesOnceAndRetriesWithTheNewToken()
        {
            SignIn("old");
            _client.SetUnauthorizedHandler(used =>
            {
                Assert.AreEqual("old", used);
                SignIn("new");
                return Task.FromResult(true);
            });
            _transport.Enqueue(401, "{\"message\":\"Unauthenticated.\"}").Enqueue(200, "{\"data\":{\"token\":\"ok\"}}");

            var response = await _client.GetAsync<RefreshResponse>("devices");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("old", _transport.Requests[0].BearerToken);
            Assert.AreEqual("new", _transport.Requests[1].BearerToken);
        }

        [Test]
        public async Task Unauthorized_IsRefreshedOnlyOncePerRequest()
        {
            SignIn("old");
            var calls = 0;
            _client.SetUnauthorizedHandler(_ =>
            {
                calls++;
                SignIn("new" + calls);
                return Task.FromResult(true);
            });
            _transport.Enqueue(401, "{}").Enqueue(401, "{}").Enqueue(200, "{}");

            var response = await _client.GetAsync<RefreshResponse>("devices");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.Unauthenticated, response.Error.Code);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(2, _transport.Requests.Count);
        }

        [Test]
        public async Task Unauthorized_WhenRefreshFails_ReturnsTheOriginalError()
        {
            SignIn("old");
            _client.SetUnauthorizedHandler(_ => Task.FromResult(false));
            _transport.Enqueue(401, "{\"message\":\"Unauthenticated.\"}");

            var response = await _client.GetAsync<RefreshResponse>("devices");

            Assert.AreEqual(401, response.StatusCode);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Unauthorized_OnLoginPath_DoesNotRefresh()
        {
            SignIn("old");
            var called = false;
            _client.SetUnauthorizedHandler(_ =>
            {
                called = true;
                return Task.FromResult(true);
            });
            _transport.Enqueue(401, "{\"message\":\"Bad credentials.\"}");

            await _client.PostAsync<object>("auth/login", new { });

            Assert.IsFalse(called);
        }

        [Test]
        public async Task Multipart_SendsFormBytesWithItsContentTypeAndUploadTimeout()
        {
            SignIn();
            _transport.Enqueue(200, "{\"data\":{\"token\":\"x\"}}");
            var form = new MultipartFormBuilder().AddField("a", "b");

            await _client.PostMultipartAsync<RefreshResponse>("photos", form);

            var request = _transport.Requests[0];
            Assert.AreEqual(form.ContentType, request.ContentType);
            Assert.AreEqual(_config.uploadTimeoutSeconds, request.TimeoutSeconds);
            Assert.That(Encoding.UTF8.GetString(request.Body), Does.Contain("name=\"a\""));
        }

        [Test]
        public async Task Multipart_AfterRefresh_ResendsTheSameBytes()
        {
            SignIn("old");
            _client.SetUnauthorizedHandler(_ =>
            {
                SignIn("new");
                return Task.FromResult(true);
            });
            _transport.Enqueue(401, "{}").Enqueue(200, "{\"data\":{\"token\":\"x\"}}");

            await _client.PostMultipartAsync<RefreshResponse>("photos", new MultipartFormBuilder().AddFile("file", "a.png", "image/png", new byte[] { 1, 2, 3 }));

            CollectionAssert.AreEqual(_transport.Requests[0].Body, _transport.Requests[1].Body);
            Assert.AreEqual("new", _transport.Requests[1].BearerToken);
        }

        [Test]
        public async Task Cancelled_BeforeSending_ReturnsCancelledWithoutRequest()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                var response = await _client.GetAsync<RefreshResponse>("devices", source.Token);

                Assert.IsTrue(response.IsCancelled);
                Assert.AreEqual(0, _transport.Requests.Count);
            }
        }

        [Test]
        public async Task Cancelled_WhileWaitingToRetry_ReturnsCancelled()
        {
            _config.retryBaseDelaySeconds = 5f;
            _transport.Enqueue(503, "").Enqueue(200, "{}");

            using (var source = new CancellationTokenSource())
            {
                var pending = _client.GetAsync<RefreshResponse>("devices", source.Token);
                source.CancelAfter(100);

                var response = await pending;

                Assert.IsTrue(response.IsCancelled);
                Assert.AreEqual(1, _transport.Requests.Count);
            }
        }

        [Test]
        public async Task Stream_DeliversBodyChunksAndSucceeds()
        {
            _transport.EnqueueStream(200, "event: chunk\n", "data: x\n\n");
            var received = new StringBuilder();

            var response = await _client.PostStreamAsync("ai/chat/stream", new { message = "hi" }, (buffer, count) => received.Append(Encoding.UTF8.GetString(buffer, 0, count)));

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("event: chunk\ndata: x\n\n", received.ToString());
            Assert.AreEqual(_config.streamTimeoutSeconds, _transport.Requests[0].TimeoutSeconds);
        }

        [Test]
        public async Task Stream_ErrorStatus_DoesNotDeliverTheBodyAsStreamData()
        {
            _transport.EnqueueStream(403, "{\"error\":{\"code\":\"AI_BUDGET_EXCEEDED\",\"message\":\"Budget exceeded.\"}}");
            var delivered = 0;

            var response = await _client.PostStreamAsync("ai/chat/stream", new { message = "hi" }, (_, count) => delivered += count);

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual("AI_BUDGET_EXCEEDED", response.Error.Code);
            Assert.AreEqual(0, delivered);
        }

        [Test]
        public async Task Stream_IsNotRetriedOnServiceUnavailable()
        {
            _transport.EnqueueStream(503, "").EnqueueStream(200, "data: x\n\n");

            var response = await _client.PostStreamAsync("ai/chat/stream", new { }, (_, __) => { });

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

    }
}
