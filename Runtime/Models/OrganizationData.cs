using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class OrganizationData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("slug")]
        public string Slug;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("settings")]
        [JsonConverter(typeof(EmptyArrayAsObjectConverter))]
        public OrganizationSettings Settings;

        [JsonProperty("logo")]
        public string Logo;

        [JsonProperty("users_count")]
        public int? UsersCount;

        [JsonProperty("teams_count")]
        public int? TeamsCount;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("updated_at")]
        public string UpdatedAt;
    }
}
