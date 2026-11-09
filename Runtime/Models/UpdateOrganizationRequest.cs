using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // Settings are merged into the stored ones, so only the keys that are set change.
    [Serializable]
    public class UpdateOrganizationRequest
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public OrganizationSettings Settings;
    }
}
