using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using UnityEngine;

namespace Technyx.Sdk.Ai
{
    public class AiService
    {
        private readonly ApiClient _http;

        public AiService(ApiClient http)
        {
            _http = http;
        }

        public Task<ApiResponse<AiChatResult>> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostLongRunningAsync<AiChatResult>("ai/chat", request, cancellationToken);
        }

        // Succeeds only when the backend sent its closing event; chunks delivered before a failure stay delivered.
        // An exception thrown by a callback is logged and does not stop the stream.
        public Task<ApiResponse<AiStreamDone>> ChatStreamAsync(
            AiChatRequest request,
            Action<AiStreamChunk> onChunk,
            Action<AiStreamSource> onSource = null,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (onChunk == null)
                throw new ArgumentNullException(nameof(onChunk));

            return StreamAsync(request, onChunk, onSource, cancellationToken);
        }

        private async Task<ApiResponse<AiStreamDone>> StreamAsync(
            AiChatRequest request,
            Action<AiStreamChunk> onChunk,
            Action<AiStreamSource> onSource,
            CancellationToken cancellationToken)
        {
            var state = new StreamState();

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var parser = new SseParser(evt => HandleEvent(evt, state, onChunk, onSource));

                void OnBody(byte[] buffer, int count)
                {
                    if (state.Fault != null)
                        return;

                    try
                    {
                        parser.Feed(buffer, count);
                    }
                    catch (Exception e)
                    {
                        state.Fault = e;
                        linked.Cancel();
                    }
                }

                var response = await _http.PostStreamAsync("ai/chat/stream", request, OnBody, linked.Token);

                if (state.Fault == null && response.IsSuccess)
                {
                    try
                    {
                        parser.Complete();
                    }
                    catch (Exception e)
                    {
                        state.Fault = e;
                    }
                }

                if (state.Fault != null)
                {
                    var fault = new ApiError { Code = ApiErrorCodes.ParseError, Message = state.Fault.Message };
                    return ApiResponse<AiStreamDone>.Fail(fault, response.StatusCode, response.RawBody);
                }

                if (!response.IsSuccess)
                    return ApiResponse<AiStreamDone>.Fail(response.Error, response.StatusCode, response.RawBody);

                if (state.Error != null)
                    return ApiResponse<AiStreamDone>.Fail(state.Error, response.StatusCode, response.RawBody);

                if (state.Done == null)
                {
                    var interrupted = new ApiError
                    {
                        Code = ApiErrorCodes.StreamInterrupted,
                        Message = "The stream ended before the answer was complete.",
                    };
                    return ApiResponse<AiStreamDone>.Fail(interrupted, response.StatusCode, response.RawBody);
                }

                return ApiResponse<AiStreamDone>.Success(state.Done, response.StatusCode, response.RawBody);
            }
        }

        public Task<ApiResponse<PaginatedResponse<AiConversationData>>> ListConversationsAsync(
            string status = null,
            DateTimeOffset? createdFrom = null,
            DateTimeOffset? createdTo = null,
            int page = 1,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("per_page", perPage)
                .Add("status", status)
                .AddUtcDateTime("from", createdFrom)
                .AddUtcDateTime("to", createdTo)
                .AppendTo("ai/conversations");

            return _http.GetAsync<PaginatedResponse<AiConversationData>>(path, cancellationToken);
        }

        public Task<ApiResponse<AiConversationDetail>> GetConversationAsync(
            string conversationId,
            int page = 1,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("per_page", perPage)
                .AppendTo($"ai/conversations/{Guard.Segment(conversationId, nameof(conversationId))}");

            return _http.GetAsync<AiConversationDetail>(path, cancellationToken);
        }

        public Task<ApiResponse<AiConversationData>> UpdateConversationAsync(
            string conversationId,
            UpdateAiConversationRequest request,
            CancellationToken cancellationToken = default)
        {
            var segment = Guard.Segment(conversationId, nameof(conversationId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PatchAsync<AiConversationData>($"ai/conversations/{segment}", request, cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            return _http.DeleteAsync($"ai/conversations/{Guard.Segment(conversationId, nameof(conversationId))}", cancellationToken);
        }

        private sealed class StreamState
        {
            public AiStreamDone Done;
            public ApiError Error;
            public Exception Fault;
        }

        private static void HandleEvent(
            SseEvent evt,
            StreamState state,
            Action<AiStreamChunk> onChunk,
            Action<AiStreamSource> onSource)
        {
            switch (evt.Name)
            {
                case "chunk":
                    Invoke(onChunk, ReadObject<AiStreamChunk>(evt));
                    break;
                case "source":
                    Invoke(onSource, ReadObject<AiStreamSource>(evt));
                    break;
                case "done":
                    state.Done = ReadObject<AiStreamDone>(evt);
                    break;
                case "error":
                    state.Error = ReadError(evt);
                    break;
            }
        }

        private static T ReadObject<T>(SseEvent evt)
        {
            var obj = JsonParser.Parse(evt.Data) as JObject;
            if (obj == null)
                throw new InvalidDataException($"The '{evt.Name}' event does not carry an object.");

            return obj.ToObject<T>();
        }

        private static ApiError ReadError(SseEvent evt)
        {
            var obj = ReadObject<JObject>(evt);
            return new ApiError
            {
                Code = (string)obj["code"] ?? ApiErrorCodes.StreamError,
                Message = (string)obj["message"] ?? "The backend reported an error while streaming.",
            };
        }

        private static void Invoke<T>(Action<T> callback, T value)
        {
            if (callback == null)
                return;

            try
            {
                callback(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
