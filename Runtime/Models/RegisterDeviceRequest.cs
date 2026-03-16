using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class RegisterDeviceRequest
    {
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
    }
}
