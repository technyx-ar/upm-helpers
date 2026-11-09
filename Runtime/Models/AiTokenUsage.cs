using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiTokenUsage
    {
        [JsonProperty("input")]
        public int? Input;

        [JsonProperty("output")]
        public int? Output;
    }
}
