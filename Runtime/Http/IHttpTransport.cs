using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Technyx.Sdk.Http
{
    public sealed class HttpRequestSpec
    {
        public string Method;
        public string Url;
        public byte[] Body;
        public string ContentType;
        public string BearerToken;
        public int TimeoutSeconds;
    }

    public sealed class HttpResult
    {
        public int StatusCode;
        public string Body = "";
        public string TransportError;
        public bool Cancelled;
        public IReadOnlyDictionary<string, string> Headers;

        public string GetHeader(string name)
        {
            if (Headers == null)
                return null;

            foreach (var pair in Headers)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return null;
        }
    }

    public interface IHttpTransport
    {
        // onBody receives the body as it arrives, but only for 2xx responses. Body of the result then
        // holds at most the first 64 KB, which is enough to read an error document.
        Task<HttpResult> SendAsync(HttpRequestSpec spec, CancellationToken cancellationToken, Action<byte[], int> onBody = null);
    }
}
