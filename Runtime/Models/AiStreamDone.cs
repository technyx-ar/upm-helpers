using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiStreamDone
    {
        [JsonProperty("message_ulid")]
        public string MessageId;

        [JsonProperty("conversation_ulid")]
        public string ConversationId;

        [JsonProperty("grounding")]
        [JsonConverter(typeof(EmptyArrayAsObjectConverter))]
        public AiGroundingData Grounding;

        [JsonProperty("sources")]
        public List<AiSourceData> Sources;

        [JsonProperty("tokens")]
        public AiTokenUsage Tokens;

        [JsonProperty("cost_cents")]
        public int? CostCents;

        [JsonProperty("provider")]
        public string Provider;

        [JsonProperty("model")]
        public string Model;

        [JsonProperty("latency_ms")]
        public int? LatencyMs;
    }
}
