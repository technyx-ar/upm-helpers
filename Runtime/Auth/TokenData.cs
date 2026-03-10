using System;

namespace Technyx.Sdk.Auth
{
    public class TokenData
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
