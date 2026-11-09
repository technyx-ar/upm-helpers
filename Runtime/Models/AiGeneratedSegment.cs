using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiGeneratedSegment
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("confidence")]
        public string Confidence;
    }
}
