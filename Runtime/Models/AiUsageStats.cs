using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiUsageStats
    {
        [JsonProperty("period")]
        public AiUsagePeriod Period;

        [JsonProperty("totals")]
        public AiUsageTotals Totals;

        [JsonProperty("by_day")]
        public List<AiUsageDayData> ByDay;

        [JsonProperty("by_user")]
        public List<AiUsageUserData> ByUser;
    }
}
