using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // The messages of a conversation are paged without a last_page, unlike the other collections.
    [Serializable]
    public class AiMessagesMeta
    {
        [JsonProperty("current_page")]
        public int CurrentPage;

        [JsonProperty("per_page")]
        public int PerPage;

        [JsonProperty("total")]
        public int Total;

        [JsonIgnore]
        public bool HasMore => (long)CurrentPage * PerPage < Total;
    }
}
