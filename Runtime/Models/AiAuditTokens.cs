using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiAuditTokens
    {
        [JsonProperty("input")]
        public int? Input;

        [JsonProperty("output")]
        public int? Output;

        [JsonProperty("total")]
        public int? Total;
    }
}
