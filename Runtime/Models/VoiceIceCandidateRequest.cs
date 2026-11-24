using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class VoiceIceCandidateRequest
    {
        [JsonProperty("session_token")]
        public string SessionToken;

        [JsonProperty("candidate")]
        public string Candidate;

        [JsonProperty("sdp_mid", NullValueHandling = NullValueHandling.Ignore)]
        public string SdpMid;

        [JsonProperty("sdp_m_line_index", NullValueHandling = NullValueHandling.Ignore)]
        public int? SdpMLineIndex;
    }
}
