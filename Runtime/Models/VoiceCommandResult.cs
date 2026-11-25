using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    // An audio upload that contains no recognizable speech returns an empty Text and ConversationId.
    [Serializable]
    public class VoiceCommandResult
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("audio_url")]
        public string AudioUrl;

        [JsonProperty("conversation_id")]
        public string ConversationId;

        [JsonProperty("sources")]
        public JArray Sources;
    }
}
