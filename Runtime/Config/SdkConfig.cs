using System;

namespace Technyx.Sdk.Config
{
    [Serializable]
    public class SdkConfig
    {
        public string apiBaseUrl = "https://api.technyx.tools/api/v1";
        public int tokenRefreshMarginSeconds = 300;
        public string encryptionSalt = "TnxSdkSalt2026";
        public int requestTimeoutSeconds = 30;
    }
}
