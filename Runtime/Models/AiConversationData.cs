using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiConversationData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("language")]
        public string Language;

        [JsonProperty("messages_count")]
        public int? MessagesCount;

        [JsonProperty("last_message")]
        public AiLastMessageData LastMessage;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("updated_at")]
        public string UpdatedAt;
    }
}
