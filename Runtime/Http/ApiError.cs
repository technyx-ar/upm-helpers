using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Http
{
    [Serializable]
    public class ApiError
    {
        [JsonProperty("code")]
        public string Code;

        [JsonProperty("message")]
        public string Message;

        public override string ToString() => $"[{Code}] {Message}";
    }
}
