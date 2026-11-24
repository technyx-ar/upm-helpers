using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // The function call as the Realtime model sent it: Name and Arguments (a JSON object string) are passed through unchanged.
    [Serializable]
    public class VoiceToolCallRequest
    {
        [JsonProperty("session_token")]
        public string SessionToken;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public string Arguments;

        [JsonProperty("call_id", NullValueHandling = NullValueHandling.Ignore)]
        public string CallId;
    }
}
