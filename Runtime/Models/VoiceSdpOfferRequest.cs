using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class VoiceSdpOfferRequest
    {
        [JsonProperty("session_token")]
        public string SessionToken;

        [JsonProperty("sdp")]
        public string Sdp;

        [JsonProperty("type")]
        public string Type = "offer";
    }
}
