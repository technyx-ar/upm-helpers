using NUnit.Framework;
using Technyx.Sdk.Config;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Tests
{
    [TestFixture]
    public class DeviceServiceTests
    {
        private DeviceService _deviceService;
        private ApiClient _apiClient;
        private SdkConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new SdkConfig
            {
                apiBaseUrl = "https://localhost/api/v1",
                tokenRefreshMarginSeconds = 300,
                encryptionSalt = "test-salt",
                requestTimeoutSeconds = 5,
            };

            var tokenStorage = new TokenStorage(_config.encryptionSalt);
            tokenStorage.Clear();

            _apiClient = new ApiClient(_config, tokenStorage);
            _deviceService = new DeviceService(_apiClient);
        }

        [Test]
        public void DeviceService_IsCreatedSuccessfully()
        {
            Assert.IsNotNull(_deviceService);
        }

        [Test]
        public void RegisterDeviceRequest_SerializesCorrectly()
        {
            var request = new RegisterDeviceRequest
            {
                Name = "Quest 3 - Sklad A",
                Type = "quest_3",
                SerialNumber = "SN-ABC123",
                FirmwareVersion = "1.0.0",
                OsVersion = "14.0",
                AppVersion = "2.1.0",
            };

            Assert.AreEqual("Quest 3 - Sklad A", request.Name);
            Assert.AreEqual("quest_3", request.Type);
            Assert.AreEqual("SN-ABC123", request.SerialNumber);
            Assert.AreEqual("1.0.0", request.FirmwareVersion);
        }

        [Test]
        public void UpdateDeviceRequest_SerializesCorrectly()
        {
            var request = new UpdateDeviceRequest
            {
                Name = "Updated Name",
                Status = "active",
            };

            Assert.AreEqual("Updated Name", request.Name);
            Assert.AreEqual("active", request.Status);
        }

        [Test]
        public void HeartbeatRequest_SerializesCorrectly()
        {
            var request = new HeartbeatRequest
            {
                BatteryLevel = 85,
                GpsLat = 48.1486,
                GpsLng = 17.1077,
                ConnectivityStatus = "wifi",
                AppVersion = "2.1.0",
            };

            Assert.AreEqual(85, request.BatteryLevel);
            Assert.AreEqual(48.1486, request.GpsLat);
            Assert.AreEqual(17.1077, request.GpsLng);
            Assert.AreEqual("wifi", request.ConnectivityStatus);
        }

        [Test]
        public void DeviceConfig_HasCorrectStructure()
        {
            var config = new DeviceConfig
            {
                LoggingLevel = "info",
                UpdateChannel = "stable",
                HeartbeatIntervalSeconds = 300,
                MaxOfflineHours = 72,
                Features = new DeviceFeatures
                {
                    VoiceCommands = true,
                    OfflineMode = true,
                    CameraCapture = true,
                    GpsTracking = true,
                },
            };

            Assert.AreEqual("info", config.LoggingLevel);
            Assert.AreEqual(300, config.HeartbeatIntervalSeconds);
            Assert.IsTrue(config.Features.VoiceCommands);
            Assert.IsTrue(config.Features.GpsTracking);
        }

        [Test]
        public void DeviceData_CanBeDeserialized()
        {
            var json = @"{
                ""id"": ""01JQTESTDEVICE0000000000"",
                ""name"": ""Quest 3 Lab"",
                ""type"": ""quest_3"",
                ""serial_number"": ""SN-123"",
                ""status"": ""active"",
                ""firmware_version"": ""1.0"",
                ""os_version"": ""14.0"",
                ""app_version"": ""2.0"",
                ""last_seen_at"": ""2026-03-10T12:00:00+00:00"",
                ""created_at"": ""2026-03-10T10:00:00+00:00"",
                ""updated_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var device = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceData>(json);

            Assert.IsNotNull(device);
            Assert.AreEqual("01JQTESTDEVICE0000000000", device.Id);
            Assert.AreEqual("Quest 3 Lab", device.Name);
            Assert.AreEqual("quest_3", device.Type);
            Assert.AreEqual("active", device.Status);
        }

        [Test]
        public void DeviceHeartbeatData_CanBeDeserialized()
        {
            var json = @"{
                ""id"": ""01JQTESTHEARTBEAT000000"",
                ""battery_level"": 75,
                ""gps_lat"": 48.1486,
                ""gps_lng"": 17.1077,
                ""connectivity_status"": ""wifi"",
                ""app_version"": ""2.1.0"",
                ""created_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var heartbeat = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceHeartbeatData>(json);

            Assert.IsNotNull(heartbeat);
            Assert.AreEqual(75, heartbeat.BatteryLevel);
            Assert.AreEqual(48.1486, heartbeat.GpsLat);
            Assert.AreEqual("wifi", heartbeat.ConnectivityStatus);
        }

        [Test]
        public void PaginatedResponse_CanBeDeserialized()
        {
            var json = @"{
                ""data"": [
                    { ""id"": ""01JQDEVICE00000000000001"", ""name"": ""Device A"", ""type"": ""quest_3"", ""serial_number"": ""SN-1"", ""status"": ""active"" },
                    { ""id"": ""01JQDEVICE00000000000002"", ""name"": ""Device B"", ""type"": ""vision_pro"", ""serial_number"": ""SN-2"", ""status"": ""registered"" }
                ],
                ""current_page"": 1,
                ""last_page"": 3,
                ""per_page"": 20,
                ""total"": 42,
                ""from"": 1,
                ""to"": 20
            }";

            var page = Newtonsoft.Json.JsonConvert.DeserializeObject<PaginatedResponse<DeviceData>>(json);

            Assert.IsNotNull(page);
            Assert.AreEqual(2, page.Data.Count);
            Assert.AreEqual(1, page.CurrentPage);
            Assert.AreEqual(3, page.LastPage);
            Assert.AreEqual(42, page.Total);
            Assert.IsTrue(page.HasMorePages);
            Assert.AreEqual("Device A", page.Data[0].Name);
            Assert.AreEqual("vision_pro", page.Data[1].Type);
        }
    }
}
