using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class VoiceSdpAnswer
    {
        [JsonProperty("sdp")]
        public string Sdp;

        [JsonProperty("type")]
        public string Type;
    }
}
