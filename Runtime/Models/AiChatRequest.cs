using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // The backend validates `sometimes` fields without `nullable`, so unset fields are omitted.
    [Serializable]
    public class AiChatRequest
    {
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message;

        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string Language;

        [JsonProperty("conversation_ulid", NullValueHandling = NullValueHandling.Ignore)]
        public string ConversationId;

        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public AiChatContext Context;

        [JsonProperty("kb_context", NullValueHandling = NullValueHandling.Ignore)]
        public List<AiKnowledgeItem> KnowledgeContext;
    }
}
