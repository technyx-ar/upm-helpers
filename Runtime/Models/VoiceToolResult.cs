using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    // Output is the string to send back to OpenAI as the function_call_output of CallId.
    [Serializable]
    public class VoiceToolResult
    {
        [JsonProperty("call_id")]
        public string CallId;

        [JsonProperty("output")]
        public string Output;

        [JsonProperty("sources")]
        public JArray Sources;
    }
}
