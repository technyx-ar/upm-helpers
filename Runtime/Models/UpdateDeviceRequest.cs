using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    // The backend validates `sometimes|string` without `nullable`, so unset fields must be omitted, not sent as null.
    [Serializable]
    public class UpdateDeviceRequest
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status;

        [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
        public string UserId;
    }
}
