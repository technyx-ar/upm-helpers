using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class OrganizationSettings
    {
        [JsonProperty("invoice_prefix", NullValueHandling = NullValueHandling.Ignore)]
        public string InvoicePrefix;

        [JsonProperty("default_language", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultLanguage;

        [JsonProperty("inactivity_timeout", NullValueHandling = NullValueHandling.Ignore)]
        public int? InactivityTimeout;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra;
    }
}
