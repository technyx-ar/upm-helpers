using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class LoginRequest
    {
        [JsonProperty("email")]
        public string Email;

        [JsonProperty("password")]
        public string Password;

        [JsonProperty("device_name")]
        public string DeviceName;
    }
}
