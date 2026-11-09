using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // The backend sends the sums of a day as decimal strings; Newtonsoft converts them.
    [Serializable]
    public class AiUsageDayData
    {
        [JsonProperty("date")]
        public string Date;

        [JsonProperty("requests")]
        public long? Requests;

        [JsonProperty("tokens_total")]
        public long? TokensTotal;

        [JsonProperty("cost_cents")]
        public long? CostCents;

        [JsonProperty("avg_grounded_ratio")]
        public double? AvgGroundedRatio;
    }
}
