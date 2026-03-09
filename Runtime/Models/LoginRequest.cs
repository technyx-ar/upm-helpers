using System;
using Newtonsoft.Json;

namespace Technyx.One.Models
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
