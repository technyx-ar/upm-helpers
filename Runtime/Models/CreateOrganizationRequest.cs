using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class CreateOrganizationRequest
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public OrganizationSettings Settings;
    }
}
