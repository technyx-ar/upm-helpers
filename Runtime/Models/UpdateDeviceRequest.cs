using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class UpdateDeviceRequest
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("status")]
        public string Status;
    }
}
