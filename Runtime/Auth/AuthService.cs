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
        private const float MinimumRefreshDelaySeconds = 5f;

        private readonly ApiClient _http;
        private readonly TokenStorage _tokenStorage;
        private readonly SdkConfig _config;
        private readonly object _refreshGate = new object();

        private TaskCompletionSource<ApiResponse<RefreshResponse>> _refreshInFlight;
        private Coroutine _refreshCoroutine;
        private int _refreshFailures;
        private bool _shutDown;

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

        internal void Shutdown()
        {
            _shutDown = true;
            CancelScheduledRefresh();
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(
            string email,
            string password,
            string deviceName = null,
            CancellationToken cancellationToken = default)
        {
            var body = new LoginRequest
            {
                Email = email,
                Password = password,
                DeviceName = deviceName ?? SystemInfo.deviceModel,
            };

            var response = await _http.PostAsync<LoginResponse>("auth/login", body, cancellationToken);

            if (response.IsSuccess)
            {
                if (response.Data == null
                    || string.IsNullOrEmpty(response.Data.Token)
                    || !TryParseExpiry(response.Data.ExpiresAt, out var expiresAt))
                    return ApiResponse<LoginResponse>.Fail(InvalidSessionPayload(), response.StatusCode, response.RawBody);

                _tokenStorage.Save(response.Data.Token, expiresAt);
                CurrentUser = response.Data.User;
                _refreshFailures = 0;
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

        private static ApiError InvalidSessionPayload() => new ApiError
        {
            Code = ApiErrorCodes.ParseError,
            Message = "The server response did not contain a valid token and expiry.",
        };

        public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsync<object>("auth/logout", null, cancellationToken);

            CancelScheduledRefresh();
            _tokenStorage.Clear();
            CurrentUser = null;
            _refreshFailures = 0;
            SetState(AuthState.Anonymous);

            return response.IsSuccess;
        }

        // Concurrent callers share one request. A failed request only ends the session when the server
        // rejected the token; a network or server failure keeps the session and retries later.
        public async Task<ApiResponse<RefreshResponse>> RefreshAsync()
        {
            TaskCompletionSource<ApiResponse<RefreshResponse>> source;
            Task<ApiResponse<RefreshResponse>> running = null;
            lock (_refreshGate)
            {
                if (_refreshInFlight != null && !_refreshInFlight.Task.IsCompleted)
                    running = _refreshInFlight.Task;

                source = running == null
                    ? _refreshInFlight = new TaskCompletionSource<ApiResponse<RefreshResponse>>()
                    : null;
            }

            if (running != null)
                return await running;

            try
            {
                var result = await RunRefreshAsync();
                source.TrySetResult(result);
            }
            catch (Exception e)
            {
                source.TrySetException(e);
            }

            return await source.Task;
        }

        private async Task<ApiResponse<RefreshResponse>> RunRefreshAsync()
        {
            var stored = _tokenStorage.Load();
            if (stored == null)
            {
                if (CurrentState != AuthState.Anonymous)
                {
                    CurrentUser = null;
                    SetState(AuthState.Error);
                }

                return ApiResponse<RefreshResponse>.Fail(
                    new ApiError { Code = ApiErrorCodes.NoSession, Message = "There is no stored session to refresh." },
                    0,
                    "");
            }

            SetState(AuthState.Refreshing);

            var response = await _http.PostAsync<RefreshResponse>("auth/refresh");

            if (response.IsSuccess)
            {
                if (response.Data != null
                    && !string.IsNullOrEmpty(response.Data.Token)
                    && TryParseExpiry(response.Data.ExpiresAt, out var expiresAt))
                {
                    _tokenStorage.Save(response.Data.Token, expiresAt);
                    _refreshFailures = 0;
                    SetState(AuthState.Authenticated);
                    ScheduleRefresh(expiresAt);
                    return response;
                }

                RejectSession(stored.Token);
                return ApiResponse<RefreshResponse>.Fail(InvalidSessionPayload(), response.StatusCode, response.RawBody);
            }

            if (response.StatusCode == 401)
            {
                RejectSession(stored.Token);
                return response;
            }

            KeepSessionAndRetry();
            return response;
        }

        // A newer login may have replaced the token while the request was running; that session stays.
        private void RejectSession(string rejectedToken)
        {
            var current = _tokenStorage.Load();
            if (current != null && current.Token != rejectedToken)
            {
                SetState(AuthState.Authenticated);
                return;
            }

            CancelScheduledRefresh();
            _tokenStorage.Clear();
            CurrentUser = null;
            SetState(AuthState.Error);
        }

        private void KeepSessionAndRetry()
        {
            var failures = _refreshFailures++;

            if (_tokenStorage.Load() == null)
            {
                CurrentUser = null;
                SetState(AuthState.Error);
                return;
            }

            SetState(AuthState.Authenticated);
            ScheduleRefreshIn((float)RefreshRetryDelay(failures, _config).TotalSeconds);
        }

        internal static TimeSpan RefreshRetryDelay(int failures, SdkConfig config)
        {
            var seconds = config.refreshRetryBaseSeconds * Math.Pow(2, failures);
            return TimeSpan.FromSeconds(Math.Min(seconds, config.refreshRetryMaxSeconds));
        }

        public async Task<ApiResponse<UserData>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync<UserData>("auth/user", cancellationToken);

            if (response.IsSuccess && response.Data != null)
                CurrentUser = response.Data;

            return response;
        }

        private async Task<bool> HandleUnauthorized(string usedToken)
        {
            var current = _tokenStorage.Load();
            if (current != null && current.Token != usedToken)
                return true;

            var result = await RefreshAsync();
            return result.IsSuccess;
        }

        private void ScheduleRefresh(DateTime expiresAt)
        {
            var delay = expiresAt - DateTime.UtcNow - TimeSpan.FromSeconds(_config.tokenRefreshMarginSeconds);
            ScheduleRefreshIn((float)delay.TotalSeconds);
        }

        private void ScheduleRefreshIn(float delaySeconds)
        {
            CancelScheduledRefresh();

            if (_shutDown)
                return;

            var instance = TechnyxSdk.Instance;
            if (instance != null)
                _refreshCoroutine = instance.StartCoroutine(RunScheduledRefresh(Math.Max(delaySeconds, MinimumRefreshDelaySeconds)));
        }

        private IEnumerator RunScheduledRefresh(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);

            _refreshCoroutine = null;
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
