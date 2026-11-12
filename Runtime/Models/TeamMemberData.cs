using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class TeamMemberData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("role")]
        public string Role;

        [JsonProperty("joined_at")]
        public string JoinedAt;
    }
}
