using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiChatContext
    {
        [JsonProperty("task_ulid", NullValueHandling = NullValueHandling.Ignore)]
        public string TaskId;

        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public string Location;
    }
}
