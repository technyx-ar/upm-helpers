using System;
using System.Collections.Generic;

namespace Technyx.Sdk.Config
{
    [Serializable]
    public class SdkConfig
    {
        public string apiBaseUrl = "https://api.prod.technyx.ar/api/v1";
        public int tokenRefreshMarginSeconds = 300;
        public string encryptionSalt = "TnxSdkSalt2026";
        public int requestTimeoutSeconds = 30;
        public int streamTimeoutSeconds = 300;
        public int uploadTimeoutSeconds = 120;
        public int maxRetries = 2;
        public float retryBaseDelaySeconds = 1f;
        public int maxRetryDelaySeconds = 30;
        public float refreshRetryBaseSeconds = 10f;
        public int refreshRetryMaxSeconds = 120;
        public bool autoHeartbeat = false;
        public int heartbeatIntervalSeconds = 300;

        public void Validate()
        {
            var problems = new List<string>();

            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                problems.Add("apiBaseUrl must be an absolute http or https URL");

            if (string.IsNullOrEmpty(encryptionSalt))
                problems.Add("encryptionSalt must not be empty");

            if (tokenRefreshMarginSeconds < 0)
                problems.Add("tokenRefreshMarginSeconds must not be negative");

            if (requestTimeoutSeconds < 1)
                problems.Add("requestTimeoutSeconds must be at least 1");

            if (streamTimeoutSeconds < 1)
                problems.Add("streamTimeoutSeconds must be at least 1");

            if (uploadTimeoutSeconds < 1)
                problems.Add("uploadTimeoutSeconds must be at least 1");

            if (maxRetries < 0)
                problems.Add("maxRetries must not be negative");

            if (retryBaseDelaySeconds < 0)
                problems.Add("retryBaseDelaySeconds must not be negative");

            if (maxRetryDelaySeconds < 0)
                problems.Add("maxRetryDelaySeconds must not be negative");

            if (refreshRetryBaseSeconds <= 0)
                problems.Add("refreshRetryBaseSeconds must be greater than 0");

            if (refreshRetryMaxSeconds < 1)
                problems.Add("refreshRetryMaxSeconds must be at least 1");

            if (heartbeatIntervalSeconds < Devices.HeartbeatService.MinIntervalSeconds
                || heartbeatIntervalSeconds > Devices.HeartbeatService.MaxIntervalSeconds)
                problems.Add($"heartbeatIntervalSeconds must be between {Devices.HeartbeatService.MinIntervalSeconds} and {Devices.HeartbeatService.MaxIntervalSeconds}");

            if (problems.Count > 0)
                throw new InvalidOperationException("Invalid Technyx SDK configuration: " + string.Join("; ", problems) + ".");
        }
    }
}
