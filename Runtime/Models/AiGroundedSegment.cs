using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiGroundedSegment
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("source_id")]
        public string SourceId;

        [JsonProperty("document_meta")]
        public JToken DocumentMeta;
    }
}
