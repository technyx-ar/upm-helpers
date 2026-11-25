using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiKnowledgeItem
    {
        [JsonProperty("source_id", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceId;

        [JsonProperty("source_type", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceType;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title;

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content;

        [JsonProperty("relevance_score", NullValueHandling = NullValueHandling.Ignore)]
        public double? RelevanceScore;

        [JsonProperty("document_meta", NullValueHandling = NullValueHandling.Ignore)]
        public AiDocumentMeta DocumentMeta;
    }
}
