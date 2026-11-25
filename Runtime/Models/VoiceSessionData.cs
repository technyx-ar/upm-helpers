using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    // EphemeralKey and SessionToken are set for oai_rt sessions, RoomName, Token and LiveKitUrl for webrtc sessions.
    // They are returned once, when the session is created.
    [Serializable]
    public class VoiceSessionData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("transport")]
        public string Transport;

        [JsonProperty("language")]
        public string Language;

        [JsonProperty("config")]
        public JObject Config;

        [JsonProperty("started_at")]
        public string StartedAt;

        [JsonProperty("ended_at")]
        public string EndedAt;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("ephemeral_key")]
        public string EphemeralKey;

        [JsonProperty("session_token")]
        public string SessionToken;

        [JsonProperty("room_name")]
        public string RoomName;

        [JsonProperty("token")]
        public string Token;

        [JsonProperty("livekit_url")]
        public string LiveKitUrl;
    }
}
