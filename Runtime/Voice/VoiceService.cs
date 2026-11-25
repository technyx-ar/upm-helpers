using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Voice
{
    public class VoiceService
    {
        public const int MaxAudioBytes = 25 * 1024 * 1024;

        private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".webm" };

        private readonly ApiClient _http;

        public VoiceService(ApiClient http)
        {
            _http = http;
        }

        public Task<ApiResponse<VoiceSessionData>> CreateSessionAsync(
            CreateVoiceSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostLongRunningAsync<VoiceSessionData>("voice/sessions", request, cancellationToken);
        }

        public Task<ApiResponse<VoiceSessionData>> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<VoiceSessionData>($"voice/sessions/{Guard.Segment(sessionId, nameof(sessionId))}", cancellationToken);
        }

        public Task<ApiResponse<VoiceSessionData>> EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return _http.PostAsync<VoiceSessionData>($"voice/sessions/{Guard.Segment(sessionId, nameof(sessionId))}/end", null, cancellationToken);
        }

        public Task<ApiResponse<VoiceCommandResult>> SendCommandAsync(
            VoiceCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostLongRunningAsync<VoiceCommandResult>("voice/command", request, cancellationToken);
        }

        // The whole recording is held in memory while it is encoded and sent. The backend accepts mp3, wav, ogg and webm.
        public Task<ApiResponse<VoiceCommandResult>> SendAudioAsync(
            byte[] audio,
            string fileName,
            string contentType,
            string language = null,
            AiChatContext context = null,
            bool speak = true,
            CancellationToken cancellationToken = default)
        {
            var invalid = UploadValidation.CheckFile<VoiceCommandResult>("audio", audio, fileName, contentType, MaxAudioBytes);
            if (invalid != null)
                return Task.FromResult(invalid);

            if (Array.IndexOf(AudioExtensions, Path.GetExtension(fileName).ToLowerInvariant()) < 0)
                return Task.FromResult(UploadValidation.Failure<VoiceCommandResult>("audio", "Only mp3, wav, ogg and webm audio can be uploaded."));

            var form = new MultipartFormBuilder()
                .AddFile("audio", fileName, contentType, audio);

            if (!string.IsNullOrWhiteSpace(language))
                form.AddField("language", language);

            if (!speak)
                form.AddField("speak", "0");

            if (!string.IsNullOrEmpty(context?.TaskId))
                form.AddField("context[task_ulid]", context.TaskId);

            if (!string.IsNullOrEmpty(context?.Location))
                form.AddField("context[location]", context.Location);

            return _http.PostMultipartAsync<VoiceCommandResult>("voice/audio", form, cancellationToken);
        }

        public Task<ApiResponse<PaginatedResponse<VoiceConversationData>>> ListHistoryAsync(
            int page = 1,
            string sessionId = null,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("session_id", sessionId)
                .Add("per_page", perPage)
                .AppendTo("voice/history");

            return _http.GetAsync<PaginatedResponse<VoiceConversationData>>(path, cancellationToken);
        }

        // oai_rt sessions only. The backend forwards the offer to OpenAI and returns the SDP answer.
        public Task<ApiResponse<VoiceSdpAnswer>> SendOfferAsync(
            VoiceSdpOfferRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostLongRunningAsync<VoiceSdpAnswer>("voice/realtime/webrtc/offer", request, cancellationToken);
        }

        // Runs a function call of an oai_rt session (search_manuals, web_search); a failed search is an error response, never an empty result.
        public Task<ApiResponse<VoiceToolResult>> ExecuteToolAsync(
            VoiceToolCallRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostLongRunningAsync<VoiceToolResult>("voice/realtime/tools/execute", request, cancellationToken);
        }

        public Task<ApiResponse<object>> SendIceCandidateAsync(
            VoiceIceCandidateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostAsync<object>("voice/realtime/webrtc/ice", request, cancellationToken);
        }
    }
}
