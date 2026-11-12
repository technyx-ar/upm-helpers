using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Technyx.Sdk.Http
{
    public sealed class UnityWebRequestTransport : IHttpTransport
    {
        public async Task<HttpResult> SendAsync(HttpRequestSpec spec, CancellationToken cancellationToken, Action<byte[], int> onBody = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return new HttpResult { Cancelled = true };

            using var request = new UnityWebRequest(spec.Url, spec.Method) { timeout = spec.TimeoutSeconds };

            StreamingDownloadHandler streaming = null;
            if (onBody != null)
            {
                streaming = new StreamingDownloadHandler(request, onBody);
                request.downloadHandler = streaming;
            }
            else
            {
                request.downloadHandler = new DownloadHandlerBuffer();
            }

            if (spec.Body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(spec.Body);
                request.SetRequestHeader("Content-Type", spec.ContentType);
            }

            // Laravel only answers validation errors as JSON when the first Accept type is JSON.
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(spec.BearerToken))
                request.SetRequestHeader("Authorization", "Bearer " + spec.BearerToken);

            var operation = request.SendWebRequest();
            var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true);

            using (cancellationToken.Register(request.Abort))
            {
                await completion.Task;
            }

            var failed = request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError;

            if (failed && cancellationToken.IsCancellationRequested)
                return new HttpResult { Cancelled = true };

            return new HttpResult
            {
                StatusCode = (int)request.responseCode,
                Body = streaming != null ? streaming.CapturedText : request.downloadHandler?.text ?? "",
                TransportError = failed ? request.error ?? "The connection failed." : null,
                Headers = CopyHeaders(request.GetResponseHeaders()),
            };
        }

        private static IReadOnlyDictionary<string, string> CopyHeaders(Dictionary<string, string> source)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (source != null)
            {
                foreach (var pair in source)
                    copy[pair.Key] = pair.Value;
            }

            return copy;
        }

        private sealed class StreamingDownloadHandler : DownloadHandlerScript
        {
            private const int CaptureLimit = 64 * 1024;

            private readonly UnityWebRequest _request;
            private readonly Action<byte[], int> _onBody;
            private readonly MemoryStream _captured = new MemoryStream();

            public StreamingDownloadHandler(UnityWebRequest request, Action<byte[], int> onBody)
            {
                _request = request;
                _onBody = onBody;
            }

            public string CapturedText => Encoding.UTF8.GetString(_captured.ToArray());

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength < 1)
                    return true;

                var room = CaptureLimit - (int)_captured.Length;
                if (room > 0)
                    _captured.Write(data, 0, Math.Min(dataLength, room));

                var status = _request.responseCode;
                if (status >= 200 && status < 300)
                    _onBody(data, dataLength);

                return true;
            }
        }
    }
}
