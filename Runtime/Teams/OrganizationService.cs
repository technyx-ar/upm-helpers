using System;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Teams
{
    public class OrganizationService
    {
        public const int MaxLogoBytes = 2048 * 1024;

        private readonly ApiClient _http;

        public OrganizationService(ApiClient http)
        {
            _http = http;
        }

        // status is one of OrganizationStatus.
        public Task<ApiResponse<PaginatedResponse<OrganizationData>>> ListAsync(
            string status = null,
            int page = 1,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("per_page", perPage)
                .Add("status", status)
                .AppendTo("organizations");

            return _http.GetAsync<PaginatedResponse<OrganizationData>>(path, cancellationToken);
        }

        public Task<ApiResponse<OrganizationData>> GetAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<OrganizationData>($"organizations/{Guard.Segment(organizationId, nameof(organizationId))}", cancellationToken);
        }

        public Task<ApiResponse<OrganizationData>> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostAsync<OrganizationData>("organizations", request, cancellationToken);
        }

        public Task<ApiResponse<OrganizationData>> UpdateAsync(
            string organizationId,
            UpdateOrganizationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PatchAsync<OrganizationData>(
                $"organizations/{Guard.Segment(organizationId, nameof(organizationId))}", request, cancellationToken);
        }

        // Multipart bodies are only parsed by PHP on POST, so the update is sent as a POST that the
        // backend routes as PATCH through the _method field.
        public Task<ApiResponse<OrganizationData>> UploadLogoAsync(
            string organizationId,
            byte[] image,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var segment = Guard.Segment(organizationId, nameof(organizationId));

            var invalid = UploadValidation.CheckFile<OrganizationData>("logo", image, fileName, contentType, MaxLogoBytes);
            if (invalid != null)
                return Task.FromResult(invalid);

            var form = new MultipartFormBuilder()
                .AddField("_method", "PATCH")
                .AddFile("logo", fileName, contentType, image);

            return _http.PostMultipartAsync<OrganizationData>($"organizations/{segment}", form, cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return _http.DeleteAsync($"organizations/{Guard.Segment(organizationId, nameof(organizationId))}", cancellationToken);
        }
    }
}
