using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
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

        // --- RegisterDeviceRequest ---

        [Test]
        public void RegisterDeviceRequest_SerializesAllFields()
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

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"name\":\"Quest 3 - Sklad A\""));
            Assert.That(json, Does.Contain("\"type\":\"quest_3\""));
            Assert.That(json, Does.Contain("\"serial_number\":\"SN-ABC123\""));
            Assert.That(json, Does.Contain("\"firmware_version\":\"1.0.0\""));
            Assert.That(json, Does.Contain("\"os_version\":\"14.0\""));
            Assert.That(json, Does.Contain("\"app_version\":\"2.1.0\""));
        }

        [Test]
        public void RegisterDeviceRequest_OptionalFieldsDefaultToNull()
        {
            var request = new RegisterDeviceRequest
            {
                Name = "Test",
                Type = "quest_3",
                SerialNumber = "SN-1",
            };

            Assert.IsNull(request.FirmwareVersion);
            Assert.IsNull(request.OsVersion);
            Assert.IsNull(request.AppVersion);
        }

        // --- UpdateDeviceRequest ---

        [Test]
        public void UpdateDeviceRequest_SerializesAllFields()
        {
            var request = new UpdateDeviceRequest
            {
                Name = "Updated Name",
                Status = "active",
                UserId = "01JQUSER000000000000000",
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"name\":\"Updated Name\""));
            Assert.That(json, Does.Contain("\"status\":\"active\""));
            Assert.That(json, Does.Contain("\"user_id\":\"01JQUSER000000000000000\""));
        }

        [Test]
        public void UpdateDeviceRequest_NullFieldsAreSerializedAsNull()
        {
            var request = new UpdateDeviceRequest
            {
                Name = "Only Name",
            };

            Assert.IsNull(request.Status);
            Assert.IsNull(request.UserId);
        }

        // --- HeartbeatRequest ---

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

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"battery_level\":85"));
            Assert.That(json, Does.Contain("\"connectivity_status\":\"wifi\""));
            Assert.That(json, Does.Contain("\"app_version\":\"2.1.0\""));
        }

        [Test]
        public void HeartbeatRequest_SupportsMetaDictionary()
        {
            var request = new HeartbeatRequest
            {
                BatteryLevel = 50,
                Meta = new Dictionary<string, object>
                {
                    { "cpu_temp", 42.5 },
                    { "memory_free_mb", 1024 },
                },
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"cpu_temp\":42.5"));
            Assert.That(json, Does.Contain("\"memory_free_mb\":1024"));
        }

        // --- DeviceConfig ---

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
            Assert.AreEqual("stable", config.UpdateChannel);
            Assert.AreEqual(300, config.HeartbeatIntervalSeconds);
            Assert.AreEqual(72, config.MaxOfflineHours);
            Assert.IsTrue(config.Features.VoiceCommands);
            Assert.IsTrue(config.Features.OfflineMode);
            Assert.IsTrue(config.Features.CameraCapture);
            Assert.IsTrue(config.Features.GpsTracking);
        }

        [Test]
        public void DeviceConfig_RoundTripsJsonCorrectly()
        {
            var config = new DeviceConfig
            {
                LoggingLevel = "debug",
                UpdateChannel = "beta",
                HeartbeatIntervalSeconds = 60,
                MaxOfflineHours = 24,
                Features = new DeviceFeatures
                {
                    VoiceCommands = false,
                    OfflineMode = true,
                    CameraCapture = false,
                    GpsTracking = true,
                },
            };

            var json = JsonConvert.SerializeObject(config);
            var deserialized = JsonConvert.DeserializeObject<DeviceConfig>(json);

            Assert.AreEqual("debug", deserialized.LoggingLevel);
            Assert.AreEqual("beta", deserialized.UpdateChannel);
            Assert.AreEqual(60, deserialized.HeartbeatIntervalSeconds);
            Assert.AreEqual(24, deserialized.MaxOfflineHours);
            Assert.IsFalse(deserialized.Features.VoiceCommands);
            Assert.IsTrue(deserialized.Features.OfflineMode);
            Assert.IsFalse(deserialized.Features.CameraCapture);
            Assert.IsTrue(deserialized.Features.GpsTracking);
        }

        // --- DeviceData deserialization ---

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

            var device = JsonConvert.DeserializeObject<DeviceData>(json);

            Assert.IsNotNull(device);
            Assert.AreEqual("01JQTESTDEVICE0000000000", device.Id);
            Assert.AreEqual("Quest 3 Lab", device.Name);
            Assert.AreEqual("quest_3", device.Type);
            Assert.AreEqual("SN-123", device.SerialNumber);
            Assert.AreEqual("active", device.Status);
            Assert.AreEqual("1.0", device.FirmwareVersion);
            Assert.AreEqual("14.0", device.OsVersion);
            Assert.AreEqual("2.0", device.AppVersion);
            Assert.IsNotNull(device.LastSeenAt);
            Assert.IsNotNull(device.CreatedAt);
            Assert.IsNotNull(device.UpdatedAt);
        }

        [Test]
        public void DeviceData_WithNestedConfig_Deserializes()
        {
            var json = @"{
                ""id"": ""01JQTESTDEVICE0000000000"",
                ""name"": ""Quest 3 Lab"",
                ""type"": ""quest_3"",
                ""serial_number"": ""SN-123"",
                ""status"": ""active"",
                ""config"": {
                    ""logging_level"": ""info"",
                    ""update_channel"": ""stable"",
                    ""heartbeat_interval_seconds"": 300,
                    ""max_offline_hours"": 72,
                    ""features"": {
                        ""voice_commands"": true,
                        ""offline_mode"": true,
                        ""camera_capture"": false,
                        ""gps_tracking"": true
                    }
                },
                ""created_at"": ""2026-03-10T10:00:00+00:00"",
                ""updated_at"": ""2026-03-10T10:00:00+00:00""
            }";

            var device = JsonConvert.DeserializeObject<DeviceData>(json);

            Assert.IsNotNull(device.Config);
            Assert.AreEqual("info", device.Config.LoggingLevel);
            Assert.AreEqual(300, device.Config.HeartbeatIntervalSeconds);
            Assert.IsTrue(device.Config.Features.VoiceCommands);
            Assert.IsFalse(device.Config.Features.CameraCapture);
        }

        [Test]
        public void DeviceData_WithNullConfig_Deserializes()
        {
            var json = @"{
                ""id"": ""01JQTESTDEVICE0000000000"",
                ""name"": ""Device"",
                ""type"": ""quest_3"",
                ""serial_number"": ""SN-1"",
                ""status"": ""registered"",
                ""config"": null,
                ""created_at"": ""2026-03-10T10:00:00+00:00"",
                ""updated_at"": ""2026-03-10T10:00:00+00:00""
            }";

            var device = JsonConvert.DeserializeObject<DeviceData>(json);

            Assert.IsNotNull(device);
            Assert.IsNull(device.Config);
        }

        [Test]
        public void DeviceData_WithLatestHeartbeat_Deserializes()
        {
            var json = @"{
                ""id"": ""01JQTESTDEVICE0000000000"",
                ""name"": ""Device"",
                ""type"": ""quest_3"",
                ""serial_number"": ""SN-1"",
                ""status"": ""active"",
                ""latest_heartbeat"": {
                    ""id"": ""01JQHEARTBEAT00000000000"",
                    ""battery_level"": 85,
                    ""gps_lat"": 48.1486,
                    ""gps_lng"": 17.1077,
                    ""connectivity_status"": ""wifi"",
                    ""app_version"": ""2.1.0"",
                    ""created_at"": ""2026-03-10T12:00:00+00:00""
                },
                ""created_at"": ""2026-03-10T10:00:00+00:00"",
                ""updated_at"": ""2026-03-10T10:00:00+00:00""
            }";

            var device = JsonConvert.DeserializeObject<DeviceData>(json);

            Assert.IsNotNull(device.LatestHeartbeat);
            Assert.AreEqual(85, device.LatestHeartbeat.BatteryLevel);
            Assert.AreEqual(48.1486, device.LatestHeartbeat.GpsLat);
            Assert.AreEqual("wifi", device.LatestHeartbeat.ConnectivityStatus);
        }

        // --- DeviceHeartbeatData deserialization ---

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

            var heartbeat = JsonConvert.DeserializeObject<DeviceHeartbeatData>(json);

            Assert.IsNotNull(heartbeat);
            Assert.AreEqual("01JQTESTHEARTBEAT000000", heartbeat.Id);
            Assert.AreEqual(75, heartbeat.BatteryLevel);
            Assert.AreEqual(48.1486, heartbeat.GpsLat);
            Assert.AreEqual(17.1077, heartbeat.GpsLng);
            Assert.AreEqual("wifi", heartbeat.ConnectivityStatus);
            Assert.AreEqual("2.1.0", heartbeat.AppVersion);
        }

        [Test]
        public void DeviceHeartbeatData_WithNullFields_Deserializes()
        {
            var json = @"{
                ""id"": ""01JQTESTHEARTBEAT000000"",
                ""battery_level"": null,
                ""gps_lat"": null,
                ""gps_lng"": null,
                ""connectivity_status"": null,
                ""app_version"": null,
                ""meta"": null,
                ""created_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var heartbeat = JsonConvert.DeserializeObject<DeviceHeartbeatData>(json);

            Assert.IsNotNull(heartbeat);
            Assert.IsNull(heartbeat.BatteryLevel);
            Assert.IsNull(heartbeat.GpsLat);
            Assert.IsNull(heartbeat.GpsLng);
            Assert.IsNull(heartbeat.ConnectivityStatus);
            Assert.IsNull(heartbeat.Meta);
        }

        [Test]
        public void DeviceHeartbeatData_WithMeta_Deserializes()
        {
            var json = @"{
                ""id"": ""01JQTESTHEARTBEAT000000"",
                ""battery_level"": 50,
                ""meta"": { ""cpu_temp"": 42.5, ""signal_strength"": -65 },
                ""created_at"": ""2026-03-10T12:00:00+00:00""
            }";

            var heartbeat = JsonConvert.DeserializeObject<DeviceHeartbeatData>(json);

            Assert.IsNotNull(heartbeat.Meta);
            Assert.AreEqual(2, heartbeat.Meta.Count);
        }

        // --- UpdateDeviceConfigRequest ---

        [Test]
        public void UpdateDeviceConfigRequest_OmitsNullFields()
        {
            var request = new UpdateDeviceConfigRequest
            {
                LoggingLevel = "debug",
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"logging_level\":\"debug\""));
            Assert.That(json, Does.Not.Contain("update_channel"));
            Assert.That(json, Does.Not.Contain("features"));
            Assert.That(json, Does.Not.Contain("heartbeat_interval_seconds"));
            Assert.That(json, Does.Not.Contain("max_offline_hours"));
        }

        [Test]
        public void UpdateDeviceConfigRequest_PartialFeaturesUpdate()
        {
            var request = new UpdateDeviceConfigRequest
            {
                Features = new UpdateDeviceFeaturesRequest
                {
                    VoiceCommands = false,
                },
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"voice_commands\":false"));
            Assert.That(json, Does.Not.Contain("offline_mode"));
            Assert.That(json, Does.Not.Contain("camera_capture"));
            Assert.That(json, Does.Not.Contain("gps_tracking"));
        }

        [Test]
        public void UpdateDeviceConfigRequest_AllFieldsSet()
        {
            var request = new UpdateDeviceConfigRequest
            {
                LoggingLevel = "error",
                UpdateChannel = "beta",
                HeartbeatIntervalSeconds = 120,
                MaxOfflineHours = 48,
                Features = new UpdateDeviceFeaturesRequest
                {
                    VoiceCommands = true,
                    OfflineMode = false,
                    CameraCapture = true,
                    GpsTracking = false,
                },
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.That(json, Does.Contain("\"logging_level\":\"error\""));
            Assert.That(json, Does.Contain("\"update_channel\":\"beta\""));
            Assert.That(json, Does.Contain("\"heartbeat_interval_seconds\":120"));
            Assert.That(json, Does.Contain("\"max_offline_hours\":48"));
            Assert.That(json, Does.Contain("\"voice_commands\":true"));
            Assert.That(json, Does.Contain("\"offline_mode\":false"));
        }

        // --- PaginatedResponse ---

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

            var page = JsonConvert.DeserializeObject<PaginatedResponse<DeviceData>>(json);

            Assert.IsNotNull(page);
            Assert.AreEqual(2, page.Data.Count);
            Assert.AreEqual(1, page.CurrentPage);
            Assert.AreEqual(3, page.LastPage);
            Assert.AreEqual(20, page.PerPage);
            Assert.AreEqual(42, page.Total);
            Assert.AreEqual(1, page.From);
            Assert.AreEqual(20, page.To);
            Assert.IsTrue(page.HasMorePages);
            Assert.AreEqual("Device A", page.Data[0].Name);
            Assert.AreEqual("quest_3", page.Data[0].Type);
            Assert.AreEqual("vision_pro", page.Data[1].Type);
        }

        [Test]
        public void PaginatedResponse_LastPage_HasNoMorePages()
        {
            var json = @"{
                ""data"": [
                    { ""id"": ""01JQDEVICE00000000000001"", ""name"": ""Device A"", ""type"": ""quest_3"", ""serial_number"": ""SN-1"", ""status"": ""active"" }
                ],
                ""current_page"": 3,
                ""last_page"": 3,
                ""per_page"": 20,
                ""total"": 42,
                ""from"": 41,
                ""to"": 42
            }";

            var page = JsonConvert.DeserializeObject<PaginatedResponse<DeviceData>>(json);

            Assert.IsFalse(page.HasMorePages);
            Assert.AreEqual(3, page.CurrentPage);
            Assert.AreEqual(3, page.LastPage);
        }

        [Test]
        public void PaginatedResponse_EmptyPage_Deserializes()
        {
            var json = @"{
                ""data"": [],
                ""current_page"": 1,
                ""last_page"": 1,
                ""per_page"": 20,
                ""total"": 0,
                ""from"": null,
                ""to"": null
            }";

            var page = JsonConvert.DeserializeObject<PaginatedResponse<DeviceData>>(json);

            Assert.IsNotNull(page);
            Assert.AreEqual(0, page.Data.Count);
            Assert.AreEqual(0, page.Total);
            Assert.IsNull(page.From);
            Assert.IsNull(page.To);
            Assert.IsFalse(page.HasMorePages);
        }
    }
}
