using System;
using System.Threading;
using System.Threading.Tasks;
using Technyx.One.Config;
using Technyx.One.Http;
using Technyx.One.Models;
using UnityEngine;

namespace Technyx.One.Auth
{
    public class AuthService
    {
        private readonly ApiClient _http;
        private readonly TokenStorage _tokenStorage;
        private readonly OneConfig _config;

        private CancellationTokenSource _refreshCts;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public event Action<AuthState, AuthState> OnAuthStateChanged;

        public AuthState CurrentState { get; private set; } = AuthState.Anonymous;
        public UserData CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentState == AuthState.Authenticated;

        public AuthService(ApiClient http, TokenStorage tokenStorage, OneConfig config)
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

            if (response.IsSuccess && response.Data != null)
            {
                var expiresAt = DateTime.Parse(response.Data.ExpiresAt).ToUniversalTime();
                _tokenStorage.Save(response.Data.Token, expiresAt);
                CurrentUser = response.Data.User;
                SetState(AuthState.Authenticated);
                ScheduleRefresh(expiresAt);
            }

            return response;
        }

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

                if (response.IsSuccess && response.Data != null)
                {
                    var expiresAt = DateTime.Parse(response.Data.ExpiresAt).ToUniversalTime();
                    _tokenStorage.Save(response.Data.Token, expiresAt);
                    SetState(AuthState.Authenticated);
                    ScheduleRefresh(expiresAt);
                }
                else
                {
                    _tokenStorage.Clear();
                    CurrentUser = null;
                    SetState(AuthState.Error);
                }

                return response;
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
            _refreshCts = new CancellationTokenSource();

            var delay = expiresAt - DateTime.UtcNow - TimeSpan.FromSeconds(_config.tokenRefreshMarginSeconds);
            if (delay <= TimeSpan.Zero)
                delay = TimeSpan.FromSeconds(5);

            _ = RunScheduledRefresh(delay, _refreshCts.Token);
        }

        private async Task RunScheduledRefresh(TimeSpan delay, CancellationToken ct)
        {
            try
            {
                await Task.Delay(delay, ct);
                if (!ct.IsCancellationRequested)
                {
                    Debug.Log("[Technyx.One] Auto-refreshing token...");
                    await RefreshAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected on cancellation
            }
        }

        private void CancelScheduledRefresh()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;
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
