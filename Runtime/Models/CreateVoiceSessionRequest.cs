using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class CreateVoiceSessionRequest
    {
        [JsonProperty("language")]
        public string Language;

        [JsonProperty("transport")]
        public string Transport;
    }
}
