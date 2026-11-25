using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiUsageUserData
    {
        [JsonProperty("user_ulid")]
        public string UserId;

        [JsonProperty("user_name")]
        public string UserName;

        [JsonProperty("requests")]
        public long? Requests;

        [JsonProperty("cost_cents")]
        public long? CostCents;

        [JsonProperty("avg_grounded_ratio")]
        public double? AvgGroundedRatio;
    }
}
