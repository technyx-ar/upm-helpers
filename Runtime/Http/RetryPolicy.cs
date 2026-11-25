using System;
using Technyx.Sdk.Config;

namespace Technyx.Sdk.Http
{
    internal static class RetryPolicy
    {
        internal static bool IsIdempotent(string method)
        {
            switch (method.ToUpperInvariant())
            {
                case "GET":
                case "HEAD":
                case "PUT":
                case "PATCH":
                case "DELETE":
                    return true;
                default:
                    return false;
            }
        }

        // A 429 is rejected before the request is handled, so every method may be repeated after it.
        // Transport failures and 502/503/504 may have been handled, so only idempotent methods are repeated.
        // A stream is never repeated because its body may already have been delivered.
        internal static TimeSpan? NextDelay(
            string method,
            int retriesDone,
            int statusCode,
            bool transportFailed,
            int? retryAfterSeconds,
            bool isStream,
            SdkConfig config)
        {
            if (retriesDone >= config.maxRetries)
                return null;

            if (statusCode == 429)
            {
                if (retryAfterSeconds == null)
                    return Backoff(retriesDone, config);

                if (retryAfterSeconds.Value > config.maxRetryDelaySeconds)
                    return null;

                return TimeSpan.FromSeconds(retryAfterSeconds.Value);
            }

            if (isStream)
                return null;

            var transient = transportFailed || statusCode == 502 || statusCode == 503 || statusCode == 504;
            if (transient && IsIdempotent(method))
                return Backoff(retriesDone, config);

            return null;
        }

        internal static TimeSpan Backoff(int retriesDone, SdkConfig config)
        {
            var seconds = config.retryBaseDelaySeconds * Math.Pow(2, retriesDone);
            return TimeSpan.FromSeconds(Math.Min(seconds, config.maxRetryDelaySeconds));
        }
    }
}
