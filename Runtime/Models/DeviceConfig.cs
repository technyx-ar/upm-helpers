using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class DeviceConfig
    {
        [JsonProperty("logging_level")]
        public string LoggingLevel;

        [JsonProperty("update_channel")]
        public string UpdateChannel;

        [JsonProperty("features")]
        public DeviceFeatures Features;

        [JsonProperty("heartbeat_interval_seconds")]
        public int HeartbeatIntervalSeconds;

        [JsonProperty("max_offline_hours")]
        public int MaxOfflineHours;
    }

    [Serializable]
    public class DeviceFeatures
    {
        [JsonProperty("voice_commands")]
        public bool VoiceCommands;

        [JsonProperty("offline_mode")]
        public bool OfflineMode;

        [JsonProperty("camera_capture")]
        public bool CameraCapture;

        [JsonProperty("gps_tracking")]
        public bool GpsTracking;
    }
}
