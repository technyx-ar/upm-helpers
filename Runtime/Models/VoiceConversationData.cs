using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class VoiceConversationData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("role")]
        public string Role;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("audio_url")]
        public string AudioUrl;

        [JsonProperty("context")]
        public JObject Context;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
