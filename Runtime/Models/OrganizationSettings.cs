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

        // One of LanguageCodes.
        [JsonProperty("default_language", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultLanguage;

        // Minutes, 5 to 480.
        [JsonProperty("inactivity_timeout", NullValueHandling = NullValueHandling.Ignore)]
        public int? InactivityTimeout;

        // Keys of the stored settings that this SDK version does not know.
        [JsonExtensionData]
        public IDictionary<string, JToken> Extra;
    }
}
