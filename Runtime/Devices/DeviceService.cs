using System.Text;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;
using UnityEngine.Networking;

namespace Technyx.Sdk.Devices
{
    public class DeviceService
    {
        private readonly ApiClient _http;

        public DeviceService(ApiClient http)
        {
            _http = http;
        }

        public Task<ApiResponse<PaginatedResponse<DeviceData>>> ListAsync(
            string type = null,
            string status = null,
            string search = null,
            int page = 1)
        {
            var query = new StringBuilder("devices?page=");
            query.Append(page);

            if (!string.IsNullOrEmpty(type))
                query.Append("&type=").Append(type);

            if (!string.IsNullOrEmpty(status))
                query.Append("&status=").Append(status);

            if (!string.IsNullOrEmpty(search))
                query.Append("&search=").Append(UnityWebRequest.EscapeURL(search));

            return _http.GetAsync<PaginatedResponse<DeviceData>>(query.ToString());
        }

        public Task<ApiResponse<DeviceData>> GetAsync(string deviceId)
        {
            return _http.GetAsync<DeviceData>($"devices/{deviceId}");
        }

        public Task<ApiResponse<DeviceData>> RegisterAsync(RegisterDeviceRequest request)
        {
            return _http.PostAsync<DeviceData>("devices", request);
        }

        public Task<ApiResponse<DeviceData>> UpdateAsync(string deviceId, UpdateDeviceRequest request)
        {
            return _http.PatchAsync<DeviceData>($"devices/{deviceId}", request);
        }

        public Task<ApiResponse<object>> DeleteAsync(string deviceId)
        {
            return _http.DeleteAsync($"devices/{deviceId}");
        }

        public Task<ApiResponse<DeviceHeartbeatData>> SendHeartbeatAsync(string deviceId, HeartbeatRequest request)
        {
            return _http.PostAsync<DeviceHeartbeatData>($"devices/{deviceId}/heartbeat", request);
        }

        public Task<ApiResponse<DeviceConfig>> GetConfigAsync(string deviceId)
        {
            return _http.GetAsync<DeviceConfig>($"devices/{deviceId}/config");
        }

        public Task<ApiResponse<DeviceData>> UpdateConfigAsync(string deviceId, DeviceConfig config)
        {
            return _http.PatchAsync<DeviceData>($"devices/{deviceId}/config", config);
        }
    }
}
