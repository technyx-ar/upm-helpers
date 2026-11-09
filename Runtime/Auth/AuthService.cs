using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using UnityEngine;

namespace Technyx.Sdk.Auth
{
    public class AuthService
    {
        private readonly ApiClient _http;
        private readonly TokenStorage _tokenStorage;
        private readonly SdkConfig _config;

        private Coroutine _refreshCoroutine;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public event Action<AuthState, AuthState> OnAuthStateChanged;

        public AuthState CurrentState { get; private set; } = AuthState.Anonymous;
        public UserData CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentState == AuthState.Authenticated;

        public AuthService(ApiClient http, TokenStorage tokenStorage, SdkConfig config)
        {
            _http = http;
            _tokenStorage = tokenStorage;
            _config = config;

            _http.SetUnauthorizedHandler(HandleUnauthorized);
        }

        internal void RestoreSession()
        {
            var token = _tokenStorage.Load();
            if (token != null)
            {
                SetState(AuthState.Authenticated);
                ScheduleRefresh(token.ExpiresAt);
            }
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(string email, string password, string deviceName = null)
        {
            var body = new LoginRequest
            {
                Email = email,
                Password = password,
                DeviceName = deviceName ?? SystemInfo.deviceModel,
            };

            var response = await _http.PostAsync<LoginResponse>("auth/login", body);

            if (response.IsSuccess)
            {
                if (response.Data == null || !TryParseExpiry(response.Data.ExpiresAt, out var expiresAt))
                    return ApiResponse<LoginResponse>.Fail(InvalidSessionPayload(), response.StatusCode, response.RawBody);

                _tokenStorage.Save(response.Data.Token, expiresAt);
                CurrentUser = response.Data.User;
                SetState(AuthState.Authenticated);
                ScheduleRefresh(expiresAt);
            }

            return response;
        }

        internal static bool TryParseExpiry(string value, out DateTime expiresAtUtc)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                expiresAtUtc = parsed.UtcDateTime;
                return true;
            }

            expiresAtUtc = default;
            return false;
        }

        private static ApiError InvalidSessionPayload() => new()
        {
            Code = "PARSE_ERROR",
            Message = "The server response did not contain a valid token and expiry.",
        };

        public async Task<bool> LogoutAsync()
        {
            var response = await _http.PostAsync<object>("auth/logout");

            CancelScheduledRefresh();
            _tokenStorage.Clear();
            CurrentUser = null;
            SetState(AuthState.Anonymous);

            return response.StatusCode == 204 || response.IsSuccess;
        }

        public async Task<ApiResponse<RefreshResponse>> RefreshAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                var previousState = CurrentState;
                SetState(AuthState.Refreshing);

                var response = await _http.PostAsync<RefreshResponse>("auth/refresh");

                if (response.IsSuccess && response.Data != null && TryParseExpiry(response.Data.ExpiresAt, out var expiresAt))
                {
                    _tokenStorage.Save(response.Data.Token, expiresAt);
                    SetState(AuthState.Authenticated);
                    ScheduleRefresh(expiresAt);
                    return response;
                }

                _tokenStorage.Clear();
                CurrentUser = null;
                SetState(AuthState.Error);

                return response.IsSuccess
                    ? ApiResponse<RefreshResponse>.Fail(InvalidSessionPayload(), response.StatusCode, response.RawBody)
                    : response;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<ApiResponse<UserData>> GetCurrentUserAsync()
        {
            var response = await _http.GetAsync<UserData>("auth/user");

            if (response.IsSuccess && response.Data != null)
                CurrentUser = response.Data;

            return response;
        }

        private async Task<bool> HandleUnauthorized()
        {
            var result = await RefreshAsync();
            return result.IsSuccess;
        }

        private void ScheduleRefresh(DateTime expiresAt)
        {
            CancelScheduledRefresh();

            var delay = expiresAt - DateTime.UtcNow - TimeSpan.FromSeconds(_config.tokenRefreshMarginSeconds);
            if (delay.TotalSeconds < 5)
                delay = TimeSpan.FromSeconds(5);

            var instance = TechnyxSdk.Instance;
            if (instance != null)
                _refreshCoroutine = instance.StartCoroutine(RunScheduledRefresh((float)delay.TotalSeconds));
        }

        private IEnumerator RunScheduledRefresh(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);

            Debug.Log("[Technyx.Sdk] Auto-refreshing token...");
            var task = RefreshAsync();

            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                Debug.LogException(task.Exception);
        }

        private void CancelScheduledRefresh()
        {
            if (_refreshCoroutine != null)
            {
                var instance = TechnyxSdk.Instance;
                if (instance != null)
                    instance.StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }
        }

        private void SetState(AuthState newState)
        {
            if (CurrentState == newState)
                return;

            var oldState = CurrentState;
            CurrentState = newState;

            try
            {
                OnAuthStateChanged?.Invoke(oldState, newState);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
