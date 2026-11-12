using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

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
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("type", type)
                .Add("status", status)
                .Add("search", search)
                .AppendTo("devices");

            return _http.GetAsync<PaginatedResponse<DeviceData>>(path, cancellationToken);
        }

        public Task<ApiResponse<DeviceData>> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<DeviceData>($"devices/{Guard.Segment(deviceId, nameof(deviceId))}", cancellationToken);
        }

        public Task<ApiResponse<DeviceData>> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default)
        {
            return _http.PostAsync<DeviceData>("devices", request, cancellationToken);
        }

        public Task<ApiResponse<DeviceData>> UpdateAsync(string deviceId, UpdateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            return _http.PatchAsync<DeviceData>($"devices/{Guard.Segment(deviceId, nameof(deviceId))}", request, cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return _http.DeleteAsync($"devices/{Guard.Segment(deviceId, nameof(deviceId))}", cancellationToken);
        }

        public Task<ApiResponse<DeviceHeartbeatData>> SendHeartbeatAsync(
            string deviceId,
            HeartbeatRequest request,
            CancellationToken cancellationToken = default)
        {
            return _http.PostAsync<DeviceHeartbeatData>(
                $"devices/{Guard.Segment(deviceId, nameof(deviceId))}/heartbeat", request, cancellationToken);
        }

        public Task<ApiResponse<DeviceConfig>> GetConfigAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<DeviceConfig>($"devices/{Guard.Segment(deviceId, nameof(deviceId))}/config", cancellationToken);
        }

        public Task<ApiResponse<DeviceData>> UpdateConfigAsync(
            string deviceId,
            UpdateDeviceConfigRequest config,
            CancellationToken cancellationToken = default)
        {
            return _http.PatchAsync<DeviceData>(
                $"devices/{Guard.Segment(deviceId, nameof(deviceId))}/config", config, cancellationToken);
        }
    }
}
