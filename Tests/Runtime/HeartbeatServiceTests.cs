using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class HeartbeatServiceTests
    {
        private const string HeartbeatBody =
            "{\"success\":true,\"data\":{\"id\":\"01HB1\",\"battery_level\":85,\"created_at\":\"2026-03-17T10:00:00+00:00\"}}";

        private sealed class ManualDelay
        {
            private readonly object _gate = new object();
            private readonly List<TaskCompletionSource<bool>> _pending = new List<TaskCompletionSource<bool>>();
            private readonly List<TimeSpan> _requested = new List<TimeSpan>();

            public int Count
            {
                get { lock (_gate) return _requested.Count; }
            }

            public TimeSpan Requested(int index)
            {
                lock (_gate) return _requested[index];
            }

            public Task Delay(TimeSpan span, CancellationToken token)
            {
                var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                token.Register(() => source.TrySetCanceled());

                lock (_gate)
                {
                    _requested.Add(span);
                    _pending.Add(source);
                }

                return source.Task;
            }

            public void ReleaseLast()
            {
                lock (_gate) _pending[_pending.Count - 1].TrySetResult(true);
            }
        }

        private SdkConfig _config;
        private TokenStorage _storage;
        private FakeHttpTransport _transport;
        private ApiClient _client;
        private AuthService _auth;
        private DeviceService _devices;
        private HeartbeatService _heartbeat;
        private ManualDelay _delay;
        private double _jitter;
        private List<DeviceHeartbeatData> _sent;
        private List<HeartbeatFailure> _failed;

        [SetUp]
        public void SetUp()
        {
            _config = new SdkConfig
            {
                apiBaseUrl = "https://api.test/api/v1",
                encryptionSalt = "test-salt",
                maxRetries = 0,
                autoHeartbeat = true,
                heartbeatIntervalSeconds = 300,
            };

            _storage = new TokenStorage(_config.encryptionSalt, "_tnxtest_heartbeat");
            _storage.Clear();
            _storage.Save("test-token", DateTime.UtcNow.AddHours(1));

            _transport = new FakeHttpTransport();
            _client = new ApiClient(_config, _storage, _transport);
            _auth = new AuthService(_client, _storage, _config);
            _auth.RestoreSession();
            _devices = new DeviceService(_client);

            _delay = new ManualDelay();
            _jitter = 0;
            _heartbeat = new HeartbeatService(_devices, _auth, _config, _delay.Delay, () => _jitter);

            _sent = new List<DeviceHeartbeatData>();
            _failed = new List<HeartbeatFailure>();
            _heartbeat.HeartbeatSent += _sent.Add;
            _heartbeat.HeartbeatFailed += _failed.Add;
        }

        [TearDown]
        public void TearDown()
        {
            _heartbeat.Shutdown();
            _auth.Shutdown();
            _storage.Clear();
        }

        private static async Task Eventually(Func<bool> condition)
        {
            for (var i = 0; i < 500; i++)
            {
                if (condition())
                    return;

                await Task.Delay(10);
            }

            Assert.Fail("The condition was not met within 5 seconds.");
        }

        private string Url(int request) => _transport.Requests[request].Url.Substring(_config.apiBaseUrl.Length + 1);

        [Test]
        public async Task Start_SendsAHeartbeatAtOnceAndThenWaitsTheInterval()
        {
            _transport.Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(1, _transport.Requests.Count);
            Assert.AreEqual("devices/01DEV/heartbeat", Url(0));
            Assert.AreEqual(TimeSpan.FromSeconds(300), _delay.Requested(0));
            Assert.AreEqual(1, _sent.Count);
            Assert.AreEqual(85, _sent[0].BatteryLevel);
            Assert.IsTrue(_heartbeat.IsRunning);
            Assert.AreEqual("01DEV", _heartbeat.DeviceId);
            Assert.IsEmpty(_failed);
        }

        [Test]
        public async Task Start_WithoutATelemetryProvider_SendsAnEmptyHeartbeat()
        {
            _transport.Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            var body = JObject.Parse(System.Text.Encoding.UTF8.GetString(_transport.Requests[0].Body));
            foreach (var property in body.Properties())
                Assert.AreEqual(JTokenType.Null, property.Value.Type, property.Name);
        }

        [Test]
        public async Task ElapsedInterval_SendsTheNextHeartbeat()
        {
            _transport.Enqueue(201, HeartbeatBody).Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            _delay.ReleaseLast();
            await Eventually(() => _delay.Count == 2);

            Assert.AreEqual(2, _transport.Requests.Count);
            Assert.AreEqual(2, _sent.Count);
        }

        [Test]
        public async Task TelemetryProvider_IsReadBeforeEveryHeartbeat()
        {
            var battery = 0;
            _heartbeat.TelemetryProvider = () => new HeartbeatRequest { BatteryLevel = ++battery, ConnectivityStatus = "wifi" };
            _transport.Enqueue(201, HeartbeatBody).Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            _delay.ReleaseLast();
            await Eventually(() => _delay.Count == 2);

            var first = JObject.Parse(System.Text.Encoding.UTF8.GetString(_transport.Requests[0].Body));
            var second = JObject.Parse(System.Text.Encoding.UTF8.GetString(_transport.Requests[1].Body));
            Assert.AreEqual(1, (int)first["battery_level"]);
            Assert.AreEqual(2, (int)second["battery_level"]);
            Assert.AreEqual("wifi", (string)second["connectivity_status"]);
        }

        [Test]
        public async Task IntervalSeconds_AppliesFromTheNextWait()
        {
            _transport.Enqueue(201, HeartbeatBody);
            _heartbeat.IntervalSeconds = 120;

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(TimeSpan.FromSeconds(120), _delay.Requested(0));
        }

        [TestCase(59)]
        [TestCase(3601)]
        public void IntervalSeconds_OutsideTheBackendRange_Throws(int seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _heartbeat.IntervalSeconds = seconds);
        }

        [Test]
        public async Task TransientFailures_BackOffAndResetAfterASuccess()
        {
            _transport.Enqueue(503, "{}").Enqueue(503, "{}").Enqueue(201, HeartbeatBody).Enqueue(503, "{}");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            Assert.AreEqual(TimeSpan.FromSeconds(15), _delay.Requested(0));
            Assert.AreEqual(1, _failed[0].ConsecutiveFailures);
            Assert.AreEqual(503, _failed[0].StatusCode);
            Assert.AreEqual(15, _failed[0].RetryInSeconds);
            Assert.IsFalse(_failed[0].Stopped);
            Assert.AreEqual("01DEV", _failed[0].DeviceId);

            _delay.ReleaseLast();
            await Eventually(() => _delay.Count == 2);
            Assert.AreEqual(TimeSpan.FromSeconds(30), _delay.Requested(1));
            Assert.AreEqual(2, _failed[1].ConsecutiveFailures);

            _delay.ReleaseLast();
            await Eventually(() => _delay.Count == 3);
            Assert.AreEqual(TimeSpan.FromSeconds(300), _delay.Requested(2));
            Assert.AreEqual(1, _sent.Count);

            _delay.ReleaseLast();
            await Eventually(() => _delay.Count == 4);
            Assert.AreEqual(TimeSpan.FromSeconds(15), _delay.Requested(3));
            Assert.AreEqual(1, _failed[2].ConsecutiveFailures);
            Assert.IsTrue(_heartbeat.IsRunning);
        }

        [Test]
        public async Task Backoff_IsBoundedByTheInterval()
        {
            _heartbeat.IntervalSeconds = 60;
            for (var i = 0; i < 5; i++)
                _transport.Enqueue(503, "{}");

            _heartbeat.Start("01DEV");
            for (var i = 1; i <= 5; i++)
            {
                await Eventually(() => _delay.Count == i);
                if (i < 5)
                    _delay.ReleaseLast();
            }

            Assert.AreEqual(TimeSpan.FromSeconds(15), _delay.Requested(0));
            Assert.AreEqual(TimeSpan.FromSeconds(30), _delay.Requested(1));
            Assert.AreEqual(TimeSpan.FromSeconds(60), _delay.Requested(2));
            Assert.AreEqual(TimeSpan.FromSeconds(60), _delay.Requested(3));
            Assert.AreEqual(TimeSpan.FromSeconds(60), _delay.Requested(4));
        }

        [Test]
        public async Task Backoff_AddsUpToTwentyPercentJitter()
        {
            _jitter = 1;
            _transport.Enqueue(503, "{}");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(TimeSpan.FromSeconds(18), _delay.Requested(0));
        }

        [Test]
        public async Task RetryAfter_LongerThanTheBackoff_IsHonoured()
        {
            _transport.Enqueue(429, "{}", "Retry-After", "20");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(ApiErrorCodes.RateLimited, _failed[0].Error.Code);
            Assert.AreEqual(TimeSpan.FromSeconds(20), _delay.Requested(0));
        }

        [Test]
        public async Task NetworkError_IsReportedAndRetried()
        {
            _transport.EnqueueTransportError("offline");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(ApiErrorCodes.NetworkError, _failed[0].Error.Code);
            Assert.IsFalse(_failed[0].Stopped);
            Assert.IsTrue(_heartbeat.IsRunning);
        }

        [Test]
        public async Task InvalidTelemetry_IsReportedAndTheLoopKeepsRunning()
        {
            _transport.Enqueue(422, "{\"message\":\"The battery level field must not be greater than 100.\",\"errors\":{\"battery_level\":[\"The battery level field must not be greater than 100.\"]}}");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.AreEqual(ApiErrorCodes.ValidationError, _failed[0].Error.Code);
            Assert.AreEqual(1, _failed[0].Error.GetFieldErrors("battery_level").Count);
            Assert.IsTrue(_heartbeat.IsRunning);
        }

        [TestCase(403, ApiErrorCodes.Forbidden)]
        [TestCase(404, ApiErrorCodes.NotFound)]
        public async Task DeviceThatIsNotTheCallers_StopsTheLoopAndSaysSo(int status, string code)
        {
            _transport.Enqueue(status, "{\"message\":\"nope\"}");

            _heartbeat.Start("01DEV");
            await Eventually(() => !_heartbeat.IsRunning);

            Assert.AreEqual(1, _failed.Count);
            Assert.AreEqual(code, _failed[0].Error.Code);
            Assert.AreEqual(status, _failed[0].StatusCode);
            Assert.IsTrue(_failed[0].Stopped);
            Assert.AreEqual(0, _failed[0].RetryInSeconds);
            Assert.IsNull(_heartbeat.DeviceId);
            Assert.AreEqual(0, _delay.Count);
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public async Task Logout_StopsTheLoop()
        {
            _transport.Enqueue(201, HeartbeatBody).Enqueue(200, "{\"success\":true}");

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            await _auth.LogoutAsync();

            Assert.IsFalse(_heartbeat.IsRunning);
            Assert.IsNull(_heartbeat.DeviceId);

            _delay.ReleaseLast();
            await Task.Delay(100);
            Assert.AreEqual(2, _transport.Requests.Count);
            Assert.AreEqual(1, _sent.Count);
        }

        [Test]
        public async Task Stop_DiscardsAHeartbeatThatIsStillInFlight()
        {
            var gate = new TaskCompletionSource<bool>();
            _transport.EnqueueGated(201, HeartbeatBody, gate.Task);

            _heartbeat.Start("01DEV");
            await Eventually(() => _transport.Requests.Count == 1);
            _heartbeat.Stop();
            gate.SetResult(true);
            await Task.Delay(100);

            Assert.IsFalse(_heartbeat.IsRunning);
            Assert.IsEmpty(_sent);
            Assert.IsEmpty(_failed);
            Assert.AreEqual(0, _delay.Count);
        }

        [Test]
        public async Task Stop_EndsTheWait()
        {
            _transport.Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            _heartbeat.Stop();
            _delay.ReleaseLast();
            await Task.Delay(100);

            Assert.AreEqual(1, _transport.Requests.Count);
            Assert.IsFalse(_heartbeat.IsRunning);
        }

        [Test]
        public async Task Start_WhileRunning_ReplacesTheDevice()
        {
            _transport.Enqueue(201, HeartbeatBody).Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01A");
            await Eventually(() => _delay.Count == 1);
            _heartbeat.Start("01B");
            await Eventually(() => _delay.Count == 2);

            Assert.AreEqual("devices/01B/heartbeat", Url(1));
            Assert.AreEqual("01B", _heartbeat.DeviceId);
            Assert.IsTrue(_heartbeat.IsRunning);

            _delay.ReleaseLast();
            _transport.Enqueue(201, HeartbeatBody);
            await Eventually(() => _transport.Requests.Count == 3);
            Assert.AreEqual("devices/01B/heartbeat", Url(2));
        }

        [Test]
        public void Start_WithoutADeviceId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _heartbeat.Start(" "));
        }

        [Test]
        public async Task Start_WhenLoggedOut_Throws()
        {
            _transport.Enqueue(200, "{\"success\":true}");
            await _auth.LogoutAsync();

            Assert.Throws<InvalidOperationException>(() => _heartbeat.Start("01DEV"));
            Assert.AreEqual(1, _transport.Requests.Count);
        }

        [Test]
        public void Start_WhenAutoHeartbeatIsOff_Throws()
        {
            var off = new HeartbeatService(_devices, _auth, new SdkConfig { autoHeartbeat = false }, _delay.Delay);

            Assert.Throws<InvalidOperationException>(() => off.Start("01DEV"));
            off.Shutdown();
        }

        [Test]
        public void Start_AfterShutdown_Throws()
        {
            _heartbeat.Shutdown();

            Assert.Throws<InvalidOperationException>(() => _heartbeat.Start("01DEV"));
        }

        [Test]
        public async Task Shutdown_StopsTheLoopAndIgnoresLaterLogins()
        {
            _transport.Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);
            _heartbeat.Shutdown();

            Assert.IsFalse(_heartbeat.IsRunning);
        }

        [Test]
        public async Task SubscriberThatThrows_DoesNotStopTheLoop()
        {
            _heartbeat.HeartbeatSent += _ => throw new InvalidOperationException("subscriber bug");
            _transport.Enqueue(201, HeartbeatBody);

            _heartbeat.Start("01DEV");
            await Eventually(() => _delay.Count == 1);

            Assert.IsTrue(_heartbeat.IsRunning);
            Assert.AreEqual(1, _sent.Count);
        }

        [Test]
        public async Task TelemetryProviderThatThrows_StopsTheLoopWithAVisibleFailure()
        {
            _heartbeat.TelemetryProvider = () => throw new InvalidOperationException("no battery api");

            _heartbeat.Start("01DEV");
            await Eventually(() => _failed.Count == 1);
            await Eventually(() => !_heartbeat.IsRunning);

            Assert.IsTrue(_failed[0].Stopped);
            Assert.AreEqual(ApiErrorCodes.Unknown, _failed[0].Error.Code);
            Assert.AreEqual("no battery api", _failed[0].Error.Message);
            Assert.AreEqual(0, _transport.Requests.Count);
        }

        [Test]
        public async Task RegisteringADevice_StartsTheHeartbeatForIt()
        {
            _transport.Enqueue(201, "{\"success\":true,\"data\":{\"id\":\"01NEW\",\"name\":\"Quest\"}}").Enqueue(201, HeartbeatBody);

            var response = await _devices.RegisterAsync(new RegisterDeviceRequest { Name = "Quest", Type = DeviceType.Quest3, SerialNumber = "SN-1" });
            await Eventually(() => _delay.Count == 1);

            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("devices", Url(0));
            Assert.AreEqual("devices/01NEW/heartbeat", Url(1));
            Assert.AreEqual("01NEW", _heartbeat.DeviceId);
        }

        [Test]
        public async Task RegisteringADevice_WhenAutoHeartbeatIsOff_StartsNothing()
        {
            var config = new SdkConfig { autoHeartbeat = false };
            var devices = new DeviceService(_client);
            var heartbeat = new HeartbeatService(devices, _auth, config, _delay.Delay);
            _transport.Enqueue(201, "{\"success\":true,\"data\":{\"id\":\"01NEW\",\"name\":\"Quest\"}}");

            await devices.RegisterAsync(new RegisterDeviceRequest { Name = "Quest", Type = DeviceType.Quest3, SerialNumber = "SN-1" });
            await Task.Delay(100);

            Assert.IsFalse(heartbeat.IsRunning);
            Assert.AreEqual(1, _transport.Requests.Count);
            heartbeat.Shutdown();
        }

        [Test]
        public async Task FailedRegistration_StartsNothing()
        {
            _transport.Enqueue(422, "{\"message\":\"The serial number has already been taken.\",\"errors\":{\"serial_number\":[\"The serial number has already been taken.\"]}}");

            var response = await _devices.RegisterAsync(new RegisterDeviceRequest { Name = "Quest", Type = DeviceType.Quest3, SerialNumber = "SN-1" });
            await Task.Delay(100);

            Assert.IsFalse(response.IsSuccess);
            Assert.IsFalse(_heartbeat.IsRunning);
            Assert.AreEqual(1, _transport.Requests.Count);
        }
    }
}
