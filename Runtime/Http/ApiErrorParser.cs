using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Http
{
    // The backend answers errors in three shapes: {error:{code,message,details,retry_after}},
    // {success:false,message} and the Laravel default {message,errors}.
    internal static class ApiErrorParser
    {
        internal static ApiError Parse(int statusCode, string body, string retryAfterHeader)
        {
            var error = TryParseBody(statusCode, body) ?? new ApiError
            {
                Code = CodeForStatus(statusCode),
                Message = $"The server returned HTTP {statusCode} without a JSON error body.",
            };

            if (error.RetryAfterSeconds == null)
                error.RetryAfterSeconds = ParseRetryAfter(retryAfterHeader);

            return error;
        }

        internal static string CodeForStatus(int statusCode)
        {
            switch (statusCode)
            {
                case 400: return ApiErrorCodes.BadRequest;
                case 401: return ApiErrorCodes.Unauthenticated;
                case 403: return ApiErrorCodes.Forbidden;
                case 404: return ApiErrorCodes.NotFound;
                case 409: return ApiErrorCodes.Conflict;
                case 413: return ApiErrorCodes.PayloadTooLarge;
                case 422: return ApiErrorCodes.ValidationError;
                case 429: return ApiErrorCodes.RateLimited;
                default: return statusCode >= 500 ? ApiErrorCodes.ServerError : ApiErrorCodes.Unknown;
            }
        }

        // Only the delta-seconds form is read; an HTTP-date value is ignored.
        internal static int? ParseRetryAfter(string header)
        {
            if (!string.IsNullOrWhiteSpace(header)
                && int.TryParse(header.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                return seconds;

            return null;
        }

        private static ApiError TryParseBody(int statusCode, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            JObject root;
            try
            {
                root = JsonParser.Parse(body) as JObject;
            }
            catch (JsonReaderException)
            {
                return null;
            }

            if (root == null)
                return null;

            var error = new ApiError { Code = CodeForStatus(statusCode) };
            var topMessage = AsString(root["message"]);

            var errorToken = root["error"];
            if (errorToken is JObject errorObject)
            {
                error.Code = AsString(errorObject["code"]) ?? error.Code;
                error.Message = AsString(errorObject["message"]) ?? topMessage;
                error.Details = errorObject["details"];
                error.RetryAfterSeconds = AsInt(errorObject["retry_after"]);
            }
            else if (errorToken != null && errorToken.Type == JTokenType.String)
            {
                error.Message = AsString(errorToken);
            }
            else
            {
                error.Message = topMessage;
            }

            error.ValidationErrors = ParseValidationErrors(root["errors"]);
            error.Message ??= $"Request failed with HTTP {statusCode}.";
            return error;
        }

        private static Dictionary<string, List<string>> ParseValidationErrors(JToken token)
        {
            if (!(token is JObject errors) || errors.Count == 0)
                return null;

            var result = new Dictionary<string, List<string>>();
            foreach (var property in errors.Properties())
            {
                var messages = new List<string>();
                if (property.Value is JArray array)
                {
                    foreach (var item in array)
                        messages.Add(AsString(item) ?? item.ToString(Formatting.None));
                }
                else
                {
                    messages.Add(AsString(property.Value) ?? property.Value.ToString(Formatting.None));
                }

                result[property.Name] = messages;
            }

            return result;
        }

        private static string AsString(JToken token)
        {
            return token != null && token.Type == JTokenType.String ? (string)token : null;
        }

        private static int? AsInt(JToken token)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Integer)
                return (int)token;

            if (token.Type == JTokenType.String
                && int.TryParse((string)token, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            return null;
        }
    }
}
