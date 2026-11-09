using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiConversationDetail
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("language")]
        public string Language;

        // null, an object or an empty array, depending on what the conversation was started with.
        [JsonProperty("context")]
        public JToken Context;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("updated_at")]
        public string UpdatedAt;

        [JsonProperty("messages")]
        public List<AiMessageData> Messages;

        [JsonProperty("messages_meta")]
        public AiMessagesMeta MessagesMeta;
    }
}
