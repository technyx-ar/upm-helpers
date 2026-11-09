using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Config;
using UnityEngine.Networking;

namespace Technyx.Sdk.Http
{
    public class ApiClient
    {
        private readonly SdkConfig _config;
        private readonly TokenStorage _tokenStorage;

        private Func<Task<bool>> _onUnauthorized;

        public ApiClient(SdkConfig config, TokenStorage tokenStorage)
        {
            _config = config;
            _tokenStorage = tokenStorage;
        }

        internal void SetUnauthorizedHandler(Func<Task<bool>> handler)
        {
            _onUnauthorized = handler;
        }

        public Task<ApiResponse<T>> GetAsync<T>(string path)
        {
            return SendAsync<T>("GET", path, null);
        }

        public Task<ApiResponse<T>> PostAsync<T>(string path, object body = null)
        {
            return SendAsync<T>("POST", path, body);
        }

        public Task<ApiResponse<T>> PutAsync<T>(string path, object body = null)
        {
            return SendAsync<T>("PUT", path, body);
        }

        public Task<ApiResponse<T>> PatchAsync<T>(string path, object body = null)
        {
            return SendAsync<T>("PATCH", path, body);
        }

        public Task<ApiResponse<object>> DeleteAsync(string path)
        {
            return SendAsync<object>("DELETE", path, null);
        }

        private async Task<ApiResponse<T>> SendAsync<T>(string method, string path, object body, bool isRetry = false)
        {
            var url = _config.apiBaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');

            var request = new UnityWebRequest(url, method)
            {
                timeout = _config.requestTimeoutSeconds,
                downloadHandler = new DownloadHandlerBuffer(),
            };

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            }

            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            var token = _tokenStorage.Load();
            if (token != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + token.Token);
            }

            var operation = request.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            operation.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            var statusCode = (int)request.responseCode;
            var rawBody = request.downloadHandler?.text ?? "";
            var transportFailed = request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError;
            var transportMessage = request.error;

            request.Dispose();

            if (transportFailed)
            {
                return ApiResponse<T>.Fail(
                    new ApiError { Code = "NETWORK_ERROR", Message = transportMessage },
                    statusCode,
                    rawBody);
            }

            if (statusCode == 401 && !isRetry && _onUnauthorized != null && ShouldRefreshOn401(path, token != null))
            {
                var refreshed = await _onUnauthorized();
                if (refreshed)
                    return await SendAsync<T>(method, path, body, isRetry: true);
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                if (string.IsNullOrEmpty(rawBody) || statusCode == 204)
                    return ApiResponse<T>.Success(default, statusCode, rawBody);

                try
                {
                    return ApiResponse<T>.Success(DeserializeData<T>(rawBody), statusCode, rawBody);
                }
                catch (Exception e)
                {
                    return ApiResponse<T>.Fail(
                        new ApiError { Code = "PARSE_ERROR", Message = e.Message },
                        statusCode,
                        rawBody);
                }
            }

            var error = DeserializeError(rawBody);
            return ApiResponse<T>.Fail(error, statusCode, rawBody);
        }

        // A 401 from login or refresh is a credential failure, not an expired session.
        internal static bool ShouldRefreshOn401(string path, bool hasToken)
        {
            if (!hasToken)
                return false;

            var normalized = path.TrimStart('/');
            return !normalized.StartsWith("auth/login", StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith("auth/refresh", StringComparison.OrdinalIgnoreCase);
        }

        internal static T DeserializeData<T>(string json)
        {
            var obj = JObject.Parse(json);
            var dataToken = obj["data"];
            if (dataToken != null)
                return dataToken.ToObject<T>();

            return JsonConvert.DeserializeObject<T>(json);
        }

        private static ApiError DeserializeError(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var errorToken = obj["error"];
                if (errorToken != null)
                    return errorToken.ToObject<ApiError>();

                return new ApiError
                {
                    Code = "UNKNOWN",
                    Message = obj["message"]?.ToString() ?? "Request failed.",
                };
            }
            catch
            {
                return new ApiError { Code = "PARSE_ERROR", Message = json };
            }
        }
    }
}
