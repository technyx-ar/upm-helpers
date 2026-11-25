using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiSourceData
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("relevance_score")]
        public double? RelevanceScore;

        [JsonProperty("referenced")]
        public bool? Referenced;

        [JsonProperty("document_meta")]
        public JToken DocumentMeta;
    }
}
