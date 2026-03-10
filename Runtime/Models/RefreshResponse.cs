using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class RefreshResponse
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("token_type")]
        public string TokenType;

        [JsonProperty("expires_at")]
        public string ExpiresAt;
    }
}
