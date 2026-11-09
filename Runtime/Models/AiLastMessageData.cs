using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiLastMessageData
    {
        [JsonProperty("role")]
        public string Role;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
