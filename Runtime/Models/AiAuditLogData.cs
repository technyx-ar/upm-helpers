using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiAuditLogData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("action")]
        public string Action;

        [JsonProperty("provider")]
        public string Provider;

        [JsonProperty("model")]
        public string Model;

        [JsonProperty("tokens")]
        public AiAuditTokens Tokens;

        [JsonProperty("cost_cents")]
        public int? CostCents;

        [JsonProperty("latency_ms")]
        public int? LatencyMs;

        // null when the answer was not analysed for grounding.
        [JsonProperty("grounded_ratio")]
        public double? GroundedRatio;

        [JsonProperty("sources_count")]
        public int? SourcesCount;

        // Only present on a single entry (GetAuditAsync); null in a list, and null for an entry without sources.
        [JsonProperty("sources_detail")]
        public List<AiSourceData> SourcesDetail;

        [JsonProperty("kb_context_provided")]
        public bool? KbContextProvided;

        [JsonProperty("language")]
        public string Language;

        // Only present when the request failed.
        [JsonProperty("error")]
        public string Error;

        [JsonProperty("user")]
        public AiAuditUserData User;

        // null for a request without a stored conversation.
        [JsonProperty("conversation_id")]
        public string ConversationId;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
