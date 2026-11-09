using System;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using UnityEngine;

namespace Technyx.Sdk.Devices
{
    public sealed class HeartbeatService
    {
        public const int MinIntervalSeconds = 60;
        public const int MaxIntervalSeconds = 3600;

        private const double FirstRetrySeconds = 15;
        private const double JitterShare = 0.2;

        private static readonly Random Random = new Random();

        private readonly DeviceService _devices;
        private readonly AuthService _auth;
        private readonly SdkConfig _config;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;
        private readonly Func<double> _jitter;

        private CancellationTokenSource _run;
        private int _intervalSeconds;
        private bool _shutDown;

        public event Action<DeviceHeartbeatData> HeartbeatSent;
        public event Action<HeartbeatFailure> HeartbeatFailed;

        public Func<HeartbeatRequest> TelemetryProvider { get; set; }

        public string DeviceId { get; private set; }

        public bool IsRunning => _run != null;

        public int IntervalSeconds
        {
            get => _intervalSeconds;
            set
            {
                if (value < MinIntervalSeconds || value > MaxIntervalSeconds)
                    throw new ArgumentOutOfRangeException(nameof(value), $"The interval must be between {MinIntervalSeconds} and {MaxIntervalSeconds} seconds.");

                _intervalSeconds = value;
            }
        }

        internal HeartbeatService(
            DeviceService devices,
            AuthService auth,
            SdkConfig config,
            Func<TimeSpan, CancellationToken, Task> delay = null,
            Func<double> jitter = null)
        {
            _devices = devices;
            _auth = auth;
            _config = config;
            _delay = delay ?? Task.Delay;
            _jitter = jitter ?? (() => Random.NextDouble());
            _intervalSeconds = config.heartbeatIntervalSeconds;

            _auth.OnAuthStateChanged += OnAuthStateChanged;

            if (_config.autoHeartbeat)
                _devices.DeviceRegistered += OnDeviceRegistered;
        }

        public void Start(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("A device id is required.", nameof(deviceId));

            if (_shutDown)
                throw new InvalidOperationException("The SDK was reconfigured; start the heartbeat on the current TechnyxSdk.Heartbeat.");

            if (!_config.autoHeartbeat)
                throw new InvalidOperationException("Automatic heartbeat is off. Set autoHeartbeat to true in the SDK configuration.");

            if (_auth.CurrentState != AuthState.Authenticated && _auth.CurrentState != AuthState.Refreshing)
                throw new InvalidOperationException("Log in before starting the heartbeat.");

            Stop();

            var run = new CancellationTokenSource();
            _run = run;
            DeviceId = deviceId;
            _ = RunAsync(deviceId, run);
        }

        public void Stop()
        {
            var run = _run;
            if (run == null)
                return;

            _run = null;
            DeviceId = null;
            run.Cancel();
        }

        internal void Shutdown()
        {
            _shutDown = true;
            Stop();
            _auth.OnAuthStateChanged -= OnAuthStateChanged;
            _devices.DeviceRegistered -= OnDeviceRegistered;
        }

        private void OnAuthStateChanged(AuthState oldState, AuthState newState)
        {
            if (newState == AuthState.Anonymous || newState == AuthState.Error)
                Stop();
        }

        private void OnDeviceRegistered(DeviceData device)
        {
            try
            {
                Start(device.Id);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                Raise(HeartbeatFailed, new HeartbeatFailure
                {
                    DeviceId = device.Id,
                    Error = new ApiError { Code = ApiErrorCodes.Unknown, Message = e.Message },
                    Stopped = true,
                });
            }
        }

        private async Task RunAsync(string deviceId, CancellationTokenSource run)
        {
            var token = run.Token;
            var failures = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var request = TelemetryProvider?.Invoke() ?? new HeartbeatRequest();
                    var response = await _devices.SendHeartbeatAsync(deviceId, request, token);

                    if (token.IsCancellationRequested)
                        break;

                    double waitSeconds;

                    if (response.IsSuccess)
                    {
                        failures = 0;
                        Raise(HeartbeatSent, response.Data);
                        waitSeconds = _intervalSeconds;
                    }
                    else
                    {
                        failures++;
                        var stopped = IsPermanent(response.Error);
                        waitSeconds = stopped ? 0 : RetryDelaySeconds(failures, response.Error);

                        Raise(HeartbeatFailed, new HeartbeatFailure
                        {
                            DeviceId = deviceId,
                            Error = response.Error,
                            StatusCode = response.StatusCode,
                            ConsecutiveFailures = failures,
                            Stopped = stopped,
                            RetryInSeconds = waitSeconds,
                        });

                        if (stopped)
                            break;
                    }

                    await _delay(TimeSpan.FromSeconds(waitSeconds), token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Raise(HeartbeatFailed, new HeartbeatFailure
                {
                    DeviceId = deviceId,
                    Error = new ApiError { Code = ApiErrorCodes.Unknown, Message = e.Message },
                    ConsecutiveFailures = failures + 1,
                    Stopped = true,
                });
            }
            finally
            {
                if (ReferenceEquals(_run, run))
                {
                    _run = null;
                    DeviceId = null;
                }

                run.Dispose();
            }
        }

        // The device is gone or not the caller's; retrying cannot help. Everything else, including 401
        // (the auth state ends the loop when the session is really lost) and 422, is retried.
        private static bool IsPermanent(ApiError error)
        {
            return error != null && (error.Code == ApiErrorCodes.Forbidden || error.Code == ApiErrorCodes.NotFound);
        }

        private double RetryDelaySeconds(int failures, ApiError error)
        {
            var backoff = FirstRetrySeconds * Math.Pow(2, Math.Min(failures - 1, 20));
            var retryAfter = error?.RetryAfterSeconds ?? 0;
            var delay = Math.Max(backoff, retryAfter) * (1 + JitterShare * _jitter());

            return Math.Min(delay, _intervalSeconds);
        }

        private static void Raise<T>(Action<T> handler, T value)
        {
            if (handler == null)
                return;

            foreach (var subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)subscriber)(value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
