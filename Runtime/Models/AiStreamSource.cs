using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    // A source the finished answer refers to. The backend sends these after the last chunk, not next to
    // the passage that uses them.
    [Serializable]
    public class AiStreamSource
    {
        [JsonProperty("source_id")]
        public string SourceId;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("relevance_score")]
        public double? RelevanceScore;

        [JsonProperty("document_meta")]
        public JToken DocumentMeta;
    }
}
