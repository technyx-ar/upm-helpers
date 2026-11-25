using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiMessageData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("role")]
        public string Role;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("sources")]
        public List<AiSourceData> Sources;

        [JsonProperty("grounding")]
        [JsonConverter(typeof(EmptyArrayAsObjectConverter))]
        public AiGroundingData Grounding;

        [JsonProperty("provider")]
        public string Provider;

        [JsonProperty("model")]
        public string Model;

        [JsonProperty("tokens")]
        public AiTokenUsage Tokens;

        [JsonProperty("audio_url")]
        public string AudioUrl;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
