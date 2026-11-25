using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiChatResult
    {
        [JsonProperty("message")]
        public AiMessageData Message;

        [JsonProperty("conversation")]
        public AiConversationRef Conversation;
    }
}
