using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class UserData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("phone")]
        public string Phone;

        [JsonProperty("language")]
        public string Language;

        [JsonProperty("timezone")]
        public string Timezone;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
