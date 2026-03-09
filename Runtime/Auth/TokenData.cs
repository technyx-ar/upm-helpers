using System;

namespace Technyx.One.Auth
{
    public class TokenData
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
