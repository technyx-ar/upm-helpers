using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Technyx.One.Auth;
using Technyx.One.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace Technyx.One.Http
{
    public class ApiClient
    {
        private readonly OneConfig _config;
        private readonly TokenStorage _tokenStorage;

        private Func<Task<bool>> _onUnauthorized;

        public ApiClient(OneConfig config, TokenStorage tokenStorage)
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

            request.Dispose();

            if (statusCode == 401 && !isRetry && _onUnauthorized != null)
            {
                var refreshed = await _onUnauthorized();
                if (refreshed)
                    return await SendAsync<T>(method, path, body, isRetry: true);
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                if (string.IsNullOrEmpty(rawBody) || statusCode == 204)
                    return ApiResponse<T>.Success(default, statusCode, rawBody);

                var data = DeserializeData<T>(rawBody);
                return ApiResponse<T>.Success(data, statusCode, rawBody);
            }

            var error = DeserializeError(rawBody);
            return ApiResponse<T>.Fail(error, statusCode, rawBody);
        }

        private static T DeserializeData<T>(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var dataToken = obj["data"];
                if (dataToken != null)
                    return dataToken.ToObject<T>();

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Technyx.One] Failed to deserialize response: {e.Message}");
                return default;
            }
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
