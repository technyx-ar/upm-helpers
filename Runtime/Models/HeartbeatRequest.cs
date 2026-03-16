using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class HeartbeatRequest
    {
        [JsonProperty("battery_level")]
        public int? BatteryLevel;

        [JsonProperty("gps_lat")]
        public double? GpsLat;

        [JsonProperty("gps_lng")]
        public double? GpsLng;

        [JsonProperty("connectivity_status")]
        public string ConnectivityStatus;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("meta")]
        public Dictionary<string, object> Meta;
    }
}
