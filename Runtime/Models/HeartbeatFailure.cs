using Technyx.Sdk.Http;

namespace Technyx.Sdk.Models
{
    public sealed class HeartbeatFailure
    {
        public string DeviceId;

        // Code is UNKNOWN with the exception message when the loop itself failed instead of a request.
        public ApiError Error;

        public int StatusCode;

        public int ConsecutiveFailures;

        public bool Stopped;

        public double RetryInSeconds;
    }
}
