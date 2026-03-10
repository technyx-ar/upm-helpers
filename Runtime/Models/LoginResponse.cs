using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class LoginResponse
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("token_type")]
        public string TokenType;

        [JsonProperty("expires_at")]
        public string ExpiresAt;

        [JsonProperty("user")]
        public UserData User;
    }
}
