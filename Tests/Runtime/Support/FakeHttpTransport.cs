using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Tests
{
    public sealed class FakeHttpTransport : IHttpTransport
    {
        private sealed class Scripted
        {
            public HttpResult Result;
            public List<byte[]> Chunks;
            public Task Gate;
        }

        private readonly Queue<Scripted> _responses = new Queue<Scripted>();

        public List<HttpRequestSpec> Requests { get; } = new List<HttpRequestSpec>();

        // When set, answers requests instead of the queue.
        public Func<HttpRequestSpec, Task<HttpResult>> Handler { get; set; }

        public FakeHttpTransport Enqueue(int status, string body, params string[] headerPairs)
        {
            _responses.Enqueue(new Scripted { Result = Result(status, body, headerPairs) });
            return this;
        }

        public FakeHttpTransport EnqueueTransportError(string message)
        {
            _responses.Enqueue(new Scripted { Result = new HttpResult { TransportError = message } });
            return this;
        }

        public FakeHttpTransport EnqueueStream(int status, params string[] chunks)
        {
            var bytes = new List<byte[]>();
            foreach (var chunk in chunks)
                bytes.Add(Encoding.UTF8.GetBytes(chunk));

            _responses.Enqueue(new Scripted { Result = Result(status, string.Concat(chunks)), Chunks = bytes });
            return this;
        }

        public FakeHttpTransport EnqueueStreamBytes(int status, params byte[][] chunks)
        {
            _responses.Enqueue(new Scripted { Result = Result(status, ""), Chunks = new List<byte[]>(chunks) });
            return this;
        }

        public FakeHttpTransport EnqueueGated(int status, string body, Task gate)
        {
            _responses.Enqueue(new Scripted { Result = Result(status, body), Gate = gate });
            return this;
        }

        public async Task<HttpResult> SendAsync(HttpRequestSpec spec, CancellationToken cancellationToken, Action<byte[], int> onBody = null)
        {
            Requests.Add(spec);

            if (cancellationToken.IsCancellationRequested)
                return new HttpResult { Cancelled = true };

            if (Handler != null)
                return await Handler(spec);

            if (_responses.Count == 0)
                throw new InvalidOperationException("No scripted response left for " + spec.Method + " " + spec.Url);

            var scripted = _responses.Dequeue();

            if (scripted.Gate != null)
                await scripted.Gate;

            if (onBody != null && scripted.Chunks != null && scripted.Result.StatusCode >= 200 && scripted.Result.StatusCode < 300)
            {
                foreach (var chunk in scripted.Chunks)
                    onBody(chunk, chunk.Length);
            }

            return scripted.Result;
        }

        public static HttpResult Result(int status, string body, params string[] headerPairs)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i + 1 < headerPairs.Length; i += 2)
                headers[headerPairs[i]] = headerPairs[i + 1];

            return new HttpResult { StatusCode = status, Body = body ?? "", Headers = headers };
        }
    }
}
