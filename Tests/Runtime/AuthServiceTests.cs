using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private const string LoginBody =
            "{\"success\":true,\"data\":{\"token\":\"login-token\",\"token_type\":\"Bearer\",\"expires_at\":\"{EXP}\","
            + "\"user\":{\"id\":\"01USER\",\"name\":\"Mario\",\"email\":\"m@example.com\"}}}";

        private AuthService _authService;
        private TokenStorage _tokenStorage;
        private ApiClient _apiClient;
        private FakeHttpTransport _transport;
        private SdkConfig _config;
        private List<AuthState> _states;

        [SetUp]
        public void SetUp()
        {
            _config = new SdkConfig
            {
                apiBaseUrl = "https://localhost/api/v1",
                tokenRefreshMarginSeconds = 300,
                encryptionSalt = "test-salt",
                requestTimeoutSeconds = 5,
                maxRetries = 0,
                refreshRetryBaseSeconds = 10f,
                refreshRetryMaxSeconds = 120,
            };

            _tokenStorage = new TokenStorage(_config.encryptionSalt, "_tnxtest_auth");
            _tokenStorage.Clear();

            _transport = new FakeHttpTransport();
            _apiClient = new ApiClient(_config, _tokenStorage, _transport);
            _authService = new AuthService(_apiClient, _tokenStorage, _config);

            _states = new List<AuthState>();
            _authService.OnAuthStateChanged += (_, state) => _states.Add(state);
        }

        [TearDown]
        public void TearDown()
        {
            _authService.Shutdown();
            _tokenStorage.Clear();
        }

        private static string Expiry(int hours) => DateTime.UtcNow.AddHours(hours).ToString("O");

        private static string RefreshBody(string token, string expiresAt) =>
            "{\"success\":true,\"data\":{\"token\":\"" + token + "\",\"token_type\":\"Bearer\",\"expires_at\":\"" + expiresAt + "\"}}";

        [Test]
        public void InitialState_IsAnonymous()
        {
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
            Assert.IsFalse(_authService.IsAuthenticated);
            Assert.IsNull(_authService.CurrentUser);
        }

        [Test]
        public void RestoreSession_WithNoStoredToken_StaysAnonymousAndFiresNothing()
        {
            _authService.RestoreSession();

            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
            Assert.IsEmpty(_states);
        }

        [Test]
        public void RestoreSession_WithStoredToken_BecomesAuthenticated()
        {
            _tokenStorage.Save("test-token", DateTime.UtcNow.AddHours(24));

            _authService.RestoreSession();

            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
            Assert.IsTrue(_authService.IsAuthenticated);
            CollectionAssert.AreEqual(new[] { AuthState.Authenticated }, _states);
        }

        [Test]
        public void RestoreSession_WithExpiredToken_StaysAnonymous()
        {
            _tokenStorage.Save("expired-token", DateTime.UtcNow.AddHours(-1));

            _authService.RestoreSession();

            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
        }

        [Test]
        public async Task Login_Success_StoresTokenAndAuthenticates()
        {
            _transport.Enqueue(200, LoginBody.Replace("{EXP}", Expiry(24)));

            var response = await _authService.LoginAsync("m@example.com", "secret", "Quest 3");

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("login-token", _tokenStorage.Load().Token);
            Assert.AreEqual("Mario", _authService.CurrentUser.Name);
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
        }

        [Test]
        public async Task Login_WrongCredentials_StaysAnonymousAndSurfacesTheError()
        {
            _transport.Enqueue(401, "{\"message\":\"Invalid credentials.\"}");

            var response = await _authService.LoginAsync("m@example.com", "wrong");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.Unauthenticated, response.Error.Code);
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
            Assert.IsNull(_tokenStorage.Load());
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Login_WithUnparsableExpiry_IsAParseError()
        {
            _transport.Enqueue(200, LoginBody.Replace("{EXP}", "not a date"));

            var response = await _authService.LoginAsync("m@example.com", "secret");

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
            Assert.IsNull(_tokenStorage.Load());
        }

        [Test]
        public async Task Refresh_Success_ReplacesTheToken()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.Enqueue(200, RefreshBody("fresh", Expiry(24)));

            var response = await _authService.RefreshAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("fresh", _tokenStorage.Load().Token);
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
            CollectionAssert.AreEqual(new[] { AuthState.Authenticated, AuthState.Refreshing, AuthState.Authenticated }, _states);
        }

        [Test]
        public async Task Refresh_Rejected401_ClearsTheSessionAndReportsError()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.Enqueue(401, "{\"message\":\"Unauthenticated.\"}");

            var response = await _authService.RefreshAsync();

            Assert.IsFalse(response.IsSuccess);
            Assert.IsNull(_tokenStorage.Load());
            Assert.AreEqual(AuthState.Error, _authService.CurrentState);
            Assert.IsNull(_authService.CurrentUser);
        }

        [TestCase(503)]
        [TestCase(500)]
        [TestCase(429)]
        public async Task Refresh_ServerTrouble_KeepsTheSession(int status)
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.Enqueue(status, "");

            var response = await _authService.RefreshAsync();

            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual("old", _tokenStorage.Load().Token);
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
        }

        [Test]
        public async Task Refresh_NetworkDown_KeepsTheSession()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.EnqueueTransportError("Cannot resolve host");

            var response = await _authService.RefreshAsync();

            Assert.IsTrue(response.IsNetworkError);
            Assert.AreEqual("old", _tokenStorage.Load().Token);
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
        }

        [Test]
        public async Task Refresh_WithInvalidPayload_EndsTheSession()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.Enqueue(200, RefreshBody("fresh", "garbage"));

            var response = await _authService.RefreshAsync();

            Assert.AreEqual(ApiErrorCodes.ParseError, response.Error.Code);
            Assert.IsNull(_tokenStorage.Load());
            Assert.AreEqual(AuthState.Error, _authService.CurrentState);
        }

        [Test]
        public async Task Refresh_WithoutStoredSession_SendsNoRequest()
        {
            var response = await _authService.RefreshAsync();

            Assert.AreEqual(ApiErrorCodes.NoSession, response.Error.Code);
            Assert.AreEqual(0, _transport.Requests.Count);
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
        }

        [Test]
        public async Task Refresh_WhenTheSessionExpiredMeanwhile_MovesToError()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddHours(1));
            _authService.RestoreSession();
            _tokenStorage.Save("old", DateTime.UtcNow.AddSeconds(-1));

            var response = await _authService.RefreshAsync();

            Assert.AreEqual(ApiErrorCodes.NoSession, response.Error.Code);
            Assert.AreEqual(AuthState.Error, _authService.CurrentState);
        }

        [Test]
        public async Task Refresh_ConcurrentCallers_ShareOneRequest()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            var gate = new TaskCompletionSource<bool>();
            _transport.EnqueueGated(200, RefreshBody("fresh", Expiry(24)), gate.Task);

            var first = _authService.RefreshAsync();
            var second = _authService.RefreshAsync();
            var third = _authService.RefreshAsync();
            gate.SetResult(true);

            var results = await Task.WhenAll(first, second, third);

            Assert.AreEqual(1, _transport.Requests.Count);
            Assert.IsTrue(results[0].IsSuccess && results[1].IsSuccess && results[2].IsSuccess);
        }

        [Test]
        public async Task Refresh_AfterACompletedRefresh_StartsANewRequest()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport.Enqueue(200, RefreshBody("one", Expiry(24))).Enqueue(200, RefreshBody("two", Expiry(24)));

            await _authService.RefreshAsync();
            await _authService.RefreshAsync();

            Assert.AreEqual(2, _transport.Requests.Count);
            Assert.AreEqual("two", _tokenStorage.Load().Token);
        }

        [Test]
        public async Task Refresh_RejectedWhileANewerLoginHappened_KeepsTheNewSession()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            var gate = new TaskCompletionSource<bool>();
            _transport.EnqueueGated(401, "{}", gate.Task);

            var pending = _authService.RefreshAsync();
            _tokenStorage.Save("newer-login", DateTime.UtcNow.AddHours(1));
            gate.SetResult(true);
            await pending;

            Assert.AreEqual("newer-login", _tokenStorage.Load().Token);
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
        }

        [Test]
        public async Task ExpiredAccessToken_IsRefreshedAndTheRequestRepeated()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            _transport
                .Enqueue(401, "{\"message\":\"Unauthenticated.\"}")
                .Enqueue(200, RefreshBody("fresh", Expiry(24)))
                .Enqueue(200, "{\"data\":{\"id\":\"01USER\",\"name\":\"Mario\",\"email\":\"m@example.com\"}}");

            var response = await _authService.GetCurrentUserAsync();

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("Mario", _authService.CurrentUser.Name);
            Assert.AreEqual("auth/refresh", _transport.Requests[1].Url.Substring(_transport.Requests[1].Url.IndexOf("auth/", StringComparison.Ordinal)));
            Assert.AreEqual("fresh", _transport.Requests[2].BearerToken);
        }

        [Test]
        public async Task ParallelRequestsWithAnExpiredToken_TriggerASingleRefresh()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddMinutes(10));
            _authService.RestoreSession();
            var gate = new TaskCompletionSource<bool>();
            _transport.Handler = async spec =>
            {
                if (spec.Url.EndsWith("auth/refresh", StringComparison.Ordinal))
                {
                    await gate.Task;
                    return FakeHttpTransport.Result(200, RefreshBody("fresh", Expiry(24)));
                }

                return spec.BearerToken == "old"
                    ? FakeHttpTransport.Result(401, "{}")
                    : FakeHttpTransport.Result(200, "{\"data\":{\"id\":\"x\"}}");
            };

            var a = _apiClient.GetAsync<Models.UserData>("auth/user");
            var b = _apiClient.GetAsync<Models.UserData>("auth/user");
            gate.SetResult(true);
            var results = await Task.WhenAll(a, b);

            Assert.IsTrue(results[0].IsSuccess && results[1].IsSuccess);
            var refreshes = 0;
            foreach (var request in _transport.Requests)
            {
                if (request.Url.EndsWith("auth/refresh", StringComparison.Ordinal))
                    refreshes++;
            }

            Assert.AreEqual(1, refreshes);
        }

        [Test]
        public async Task Logout_ClearsTheSessionEvenWhenTheServerCallFails()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddHours(1));
            _authService.RestoreSession();
            _transport.EnqueueTransportError("offline");

            var ok = await _authService.LogoutAsync();

            Assert.IsFalse(ok);
            Assert.IsNull(_tokenStorage.Load());
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
        }

        [Test]
        public async Task Logout_WithADeadToken_DoesNotTryToRefresh()
        {
            _tokenStorage.Save("old", DateTime.UtcNow.AddHours(1));
            _authService.RestoreSession();
            _transport.Enqueue(401, "{}");

            await _authService.LogoutAsync();

            Assert.AreEqual(1, _transport.Requests.Count);
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
        }

        [Test]
        public void RefreshRetryDelay_GrowsAndIsCapped()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(10), AuthService.RefreshRetryDelay(0, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(20), AuthService.RefreshRetryDelay(1, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(40), AuthService.RefreshRetryDelay(2, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(120), AuthService.RefreshRetryDelay(10, _config));
            Assert.AreEqual(TimeSpan.FromSeconds(120), AuthService.RefreshRetryDelay(5000, _config));
        }

        [Test]
        public void ThrowingStateHandler_DoesNotBreakTheService()
        {
            _authService.OnAuthStateChanged += (_, __) => throw new InvalidOperationException("handler bug");
            _tokenStorage.Save("t", DateTime.UtcNow.AddHours(1));

            Assert.DoesNotThrow(() => _authService.RestoreSession());
            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
        }
    }
}
