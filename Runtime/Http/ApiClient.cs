using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;

namespace Technyx.Sdk.Http
{
    public class ApiClient
    {
        private const string JsonContentType = "application/json";

        private static readonly Random Jitter = new Random();

        private readonly SdkConfig _config;
        private readonly TokenStorage _tokenStorage;
        private readonly IHttpTransport _transport;

        private Func<string, Task<bool>> _onUnauthorized;

        public ApiClient(SdkConfig config, TokenStorage tokenStorage)
            : this(config, tokenStorage, new UnityWebRequestTransport())
        {
        }

        public ApiClient(SdkConfig config, TokenStorage tokenStorage, IHttpTransport transport)
        {
            _config = config;
            _tokenStorage = tokenStorage;
            _transport = transport;
        }

        internal void SetUnauthorizedHandler(Func<string, Task<bool>> handler)
        {
            _onUnauthorized = handler;
        }

        public Task<ApiResponse<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync<T>("GET", path, null, cancellationToken);
        }

        public Task<ApiResponse<T>> PostAsync<T>(string path, object body = null, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync<T>("POST", path, body, cancellationToken);
        }

        public Task<ApiResponse<T>> PutAsync<T>(string path, object body = null, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync<T>("PUT", path, body, cancellationToken);
        }

        public Task<ApiResponse<T>> PatchAsync<T>(string path, object body = null, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync<T>("PATCH", path, body, cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync<object>("DELETE", path, null, cancellationToken);
        }

        public Task<ApiResponse<T>> PostMultipartAsync<T>(string path, MultipartFormBuilder form, CancellationToken cancellationToken = default)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            return SendAsync<T>("POST", path, form.Build(), form.ContentType, _config.uploadTimeoutSeconds, cancellationToken);
        }

        // onBody is called with each piece of a successful response while it downloads and must not throw.
        public async Task<ApiResponse<object>> PostStreamAsync(
            string path,
            object body,
            Action<byte[], int> onBody,
            CancellationToken cancellationToken = default)
        {
            if (onBody == null)
                throw new ArgumentNullException(nameof(onBody));

            var bytes = Serialize(body);
            var raw = await ExecuteAsync(
                "POST", path, bytes, bytes != null ? JsonContentType : null, _config.streamTimeoutSeconds, cancellationToken, onBody);

            return raw.Error != null
                ? ApiResponse<object>.Fail(raw.Error, raw.StatusCode, raw.Body)
                : ApiResponse<object>.Success(null, raw.StatusCode, raw.Body);
        }

        private Task<ApiResponse<T>> SendJsonAsync<T>(string method, string path, object body, CancellationToken cancellationToken)
        {
            var bytes = Serialize(body);
            return SendAsync<T>(method, path, bytes, bytes != null ? JsonContentType : null, _config.requestTimeoutSeconds, cancellationToken);
        }

        private static byte[] Serialize(object body)
        {
            return body == null ? null : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
        }

        private async Task<ApiResponse<T>> SendAsync<T>(
            string method,
            string path,
            byte[] body,
            string contentType,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var raw = await ExecuteAsync(method, path, body, contentType, timeoutSeconds, cancellationToken, null);

            if (raw.Error != null)
                return ApiResponse<T>.Fail(raw.Error, raw.StatusCode, raw.Body);

            if (string.IsNullOrEmpty(raw.Body) || raw.StatusCode == 204)
                return ApiResponse<T>.Success(default, raw.StatusCode, raw.Body);

            try
            {
                return ApiResponse<T>.Success(DeserializeData<T>(raw.Body), raw.StatusCode, raw.Body);
            }
            catch (Exception e)
            {
                return ApiResponse<T>.Fail(
                    new ApiError { Code = ApiErrorCodes.ParseError, Message = e.Message },
                    raw.StatusCode,
                    raw.Body);
            }
        }

        private async Task<RawResponse> ExecuteAsync(
            string method,
            string path,
            byte[] body,
            string contentType,
            int timeoutSeconds,
            CancellationToken cancellationToken,
            Action<byte[], int> onBody)
        {
            var url = _config.apiBaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
            var isStream = onBody != null;
            var retries = 0;
            var refreshed = false;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return RawResponse.Cancelled();

                var token = _tokenStorage.Load();
                var spec = new HttpRequestSpec
                {
                    Method = method,
                    Url = url,
                    Body = body,
                    ContentType = contentType,
                    BearerToken = token?.Token,
                    TimeoutSeconds = timeoutSeconds,
                };

                var result = await _transport.SendAsync(spec, cancellationToken, onBody);

                if (result.Cancelled)
                    return RawResponse.Cancelled();

                if (result.TransportError != null)
                {
                    var transportDelay = RetryPolicy.NextDelay(method, retries, 0, true, null, isStream, _config);
                    if (transportDelay != null)
                    {
                        if (!await WaitAsync(transportDelay.Value, cancellationToken))
                            return RawResponse.Cancelled();

                        retries++;
                        continue;
                    }

                    return new RawResponse
                    {
                        StatusCode = result.StatusCode,
                        Body = result.Body,
                        Error = new ApiError { Code = ApiErrorCodes.NetworkError, Message = result.TransportError },
                    };
                }

                if (result.StatusCode >= 200 && result.StatusCode < 300)
                    return new RawResponse { StatusCode = result.StatusCode, Body = result.Body };

                var error = ApiErrorParser.Parse(result.StatusCode, result.Body, result.GetHeader("Retry-After"));

                if (result.StatusCode == 401 && !refreshed && _onUnauthorized != null && ShouldRefreshOn401(path, token != null))
                {
                    refreshed = true;
                    if (await _onUnauthorized(token.Token))
                        continue;
                }

                var delay = RetryPolicy.NextDelay(method, retries, result.StatusCode, false, error.RetryAfterSeconds, isStream, _config);
                if (delay != null)
                {
                    if (!await WaitAsync(delay.Value, cancellationToken))
                        return RawResponse.Cancelled();

                    retries++;
                    continue;
                }

                return new RawResponse { StatusCode = result.StatusCode, Body = result.Body, Error = error };
            }
        }

        // Task.Delay needs threads, so the SDK does not support WebGL.
        private static async Task<bool> WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            var jittered = TimeSpan.FromSeconds(delay.TotalSeconds * (1 + 0.2 * Jitter.NextDouble()));
            try
            {
                await Task.Delay(jittered, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        // A 401 from login or refresh is a credential failure, not an expired session, and
        // logging out with a dead token needs no new one.
        internal static bool ShouldRefreshOn401(string path, bool hasToken)
        {
            if (!hasToken)
                return false;

            var normalized = path.TrimStart('/');
            return !normalized.StartsWith("auth/login", StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith("auth/refresh", StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith("auth/logout", StringComparison.OrdinalIgnoreCase);
        }

        // Resource collections of the Team module are returned without the {data: ...} envelope,
        // so a root object that carries both a data array and meta is the paginated payload itself.
        internal static T DeserializeData<T>(string json)
        {
            var token = JsonParser.Parse(json);

            if (token is JObject obj)
            {
                var data = obj["data"];

                if (IsPaginated(typeof(T)) && data is JArray && obj["meta"] != null)
                    return obj.ToObject<T>();

                if (data != null)
                    return data.ToObject<T>();
            }

            return token.ToObject<T>();
        }

        private static bool IsPaginated(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PaginatedResponse<>);
        }

        private sealed class RawResponse
        {
            public int StatusCode;
            public string Body;
            public ApiError Error;

            public static RawResponse Cancelled() => new RawResponse
            {
                Body = "",
                Error = new ApiError { Code = ApiErrorCodes.Cancelled, Message = "The request was cancelled." },
            };
        }
    }
}
