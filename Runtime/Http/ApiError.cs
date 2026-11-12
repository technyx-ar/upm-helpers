using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Http
{
    public static class ApiErrorCodes
    {
        public const string NetworkError = "NETWORK_ERROR";
        public const string ParseError = "PARSE_ERROR";
        public const string Cancelled = "CANCELLED";
        public const string NoSession = "NO_SESSION";
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthenticated = "UNAUTHENTICATED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string RateLimited = "RATE_LIMITED";
        public const string ServerError = "SERVER_ERROR";
        public const string StreamError = "STREAM_ERROR";
        public const string StreamInterrupted = "STREAM_INTERRUPTED";
        public const string Unknown = "UNKNOWN";
    }

    [Serializable]
    public class ApiError
    {
        [JsonProperty("code")]
        public string Code;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("details")]
        public JToken Details;

        [JsonProperty("retry_after")]
        public int? RetryAfterSeconds;

        [JsonIgnore]
        public Dictionary<string, List<string>> ValidationErrors;

        [JsonIgnore]
        public bool HasValidationErrors => ValidationErrors != null && ValidationErrors.Count > 0;

        public IReadOnlyList<string> GetFieldErrors(string field)
        {
            if (ValidationErrors != null && field != null && ValidationErrors.TryGetValue(field, out var messages))
                return messages;

            return Array.Empty<string>();
        }

        public override string ToString() => $"[{Code}] {Message}";
    }
}
