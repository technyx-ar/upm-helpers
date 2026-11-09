using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiUsagePeriod
    {
        [JsonProperty("from")]
        public string From;

        [JsonProperty("to")]
        public string To;
    }
}
