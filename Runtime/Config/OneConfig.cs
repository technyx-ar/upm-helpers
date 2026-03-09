using System;

namespace Technyx.One.Config
{
    [Serializable]
    public class OneConfig
    {
        public string apiBaseUrl = "https://api.technyx.tools/api/v1";
        public int tokenRefreshMarginSeconds = 300;
        public string encryptionSalt = "TnxOneSdkSalt2026";
        public int requestTimeoutSeconds = 30;
    }
}
