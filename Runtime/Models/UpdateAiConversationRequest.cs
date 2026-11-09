using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class UpdateAiConversationRequest
    {
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title;

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status;
    }
}
