namespace Technyx.Sdk.Models
{
    public static class VoiceTransports
    {
        public const string WebSocket = "ws";
        public const string LiveKit = "webrtc";
        public const string OpenAiRealtime = "oai_rt";
    }

    public static class VoiceSessionStatus
    {
        public const string Created = "created";
        public const string Active = "active";
        public const string Ended = "ended";
    }
}
