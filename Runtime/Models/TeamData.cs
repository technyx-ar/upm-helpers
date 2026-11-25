using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class TeamData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("manager")]
        public TeamManagerData Manager;

        // Only the single-team endpoint loads the members; the list and write endpoints leave this null.
        [JsonProperty("members")]
        public List<TeamMemberData> Members;

        [JsonProperty("members_count")]
        public int? MembersCount;

        [JsonProperty("created_at")]
        public string CreatedAt;
    }
}
