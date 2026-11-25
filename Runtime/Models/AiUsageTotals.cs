using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiUsageTotals
    {
        [JsonProperty("requests")]
        public long? Requests;

        [JsonProperty("tokens_input")]
        public long? TokensInput;

        [JsonProperty("tokens_output")]
        public long? TokensOutput;

        [JsonProperty("cost_cents")]
        public long? CostCents;

        [JsonProperty("avg_grounded_ratio")]
        public double? AvgGroundedRatio;

        [JsonProperty("avg_latency_ms")]
        public long? AvgLatencyMs;

        [JsonProperty("kb_context_usage_rate")]
        public double? KbContextUsageRate;
    }
}
