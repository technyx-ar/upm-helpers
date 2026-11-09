using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class CreateTeamRequest
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description;

        [JsonProperty("manager_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ManagerId;
    }
}
