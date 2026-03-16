using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class DeviceData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("serial_number")]
        public string SerialNumber;

        [JsonProperty("firmware_version")]
        public string FirmwareVersion;

        [JsonProperty("os_version")]
        public string OsVersion;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("config")]
        public DeviceConfig Config;

        [JsonProperty("last_seen_at")]
        public string LastSeenAt;

        [JsonProperty("latest_heartbeat")]
        public DeviceHeartbeatData LatestHeartbeat;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("updated_at")]
        public string UpdatedAt;
    }
}
