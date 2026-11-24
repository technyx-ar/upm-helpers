using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class VoiceCommandRequest
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("language")]
        public string Language;

        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public AiChatContext Context;
    }
}
