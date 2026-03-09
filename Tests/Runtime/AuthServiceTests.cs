using System.Threading.Tasks;
using NUnit.Framework;
using Technyx.One.Auth;
using Technyx.One.Config;
using Technyx.One.Http;

namespace Technyx.One.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private AuthService _authService;
        private TokenStorage _tokenStorage;
        private ApiClient _apiClient;
        private OneConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new OneConfig
            {
                apiBaseUrl = "https://localhost/api/v1",
                tokenRefreshMarginSeconds = 300,
                encryptionSalt = "test-salt",
                requestTimeoutSeconds = 5,
            };

            _tokenStorage = new TokenStorage(_config.encryptionSalt);
            _tokenStorage.Clear();

            _apiClient = new ApiClient(_config, _tokenStorage);
            _authService = new AuthService(_apiClient, _tokenStorage, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _tokenStorage.Clear();
        }

        [Test]
        public void InitialState_IsAnonymous()
        {
            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
        }

        [Test]
        public void IsAuthenticated_WhenAnonymous_ReturnsFalse()
        {
            Assert.IsFalse(_authService.IsAuthenticated);
        }

        [Test]
        public void CurrentUser_WhenAnonymous_IsNull()
        {
            Assert.IsNull(_authService.CurrentUser);
        }

        [Test]
        public void OnAuthStateChanged_FiresOnStateTransition()
        {
            AuthState? capturedOld = null;
            AuthState? capturedNew = null;

            _authService.OnAuthStateChanged += (oldState, newState) =>
            {
                capturedOld = oldState;
                capturedNew = newState;
            };

            // RestoreSession with no stored token should not fire (stays Anonymous)
            _authService.RestoreSession();
            Assert.IsNull(capturedOld);
            Assert.IsNull(capturedNew);
        }

        [Test]
        public void RestoreSession_WithStoredToken_BecomesAuthenticated()
        {
            _tokenStorage.Save("test-token", System.DateTime.UtcNow.AddHours(24));

            AuthState? newState = null;
            _authService.OnAuthStateChanged += (_, s) => newState = s;

            _authService.RestoreSession();

            Assert.AreEqual(AuthState.Authenticated, _authService.CurrentState);
            Assert.AreEqual(AuthState.Authenticated, newState);
            Assert.IsTrue(_authService.IsAuthenticated);
        }

        [Test]
        public void RestoreSession_WithExpiredToken_StaysAnonymous()
        {
            _tokenStorage.Save("expired-token", System.DateTime.UtcNow.AddHours(-1));

            _authService.RestoreSession();

            Assert.AreEqual(AuthState.Anonymous, _authService.CurrentState);
            Assert.IsFalse(_authService.IsAuthenticated);
        }
    }
}
