using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Ai
{
    // Needs the view-ai-audit permission; every call is scoped to the caller's organization.
    public class AiAdminService
    {
        private readonly ApiClient _http;

        public AiAdminService(ApiClient http)
        {
            _http = http;
        }

        public Task<ApiResponse<AiUsageStats>> GetUsageAsync(AiUsageFilter filter = null, CancellationToken cancellationToken = default)
        {
            var query = new QueryBuilder();
            filter?.AppendTo(query);

            return _http.GetAsync<AiUsageStats>(query.AppendTo("admin/ai/usage"), cancellationToken);
        }

        public Task<ApiResponse<PaginatedResponse<AiAuditLogData>>> ListAuditAsync(
            AiAuditFilter filter = null,
            int page = 1,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var query = new QueryBuilder()
                .Add("page", page)
                .Add("per_page", perPage);
            filter?.AppendTo(query);

            return _http.GetAsync<PaginatedResponse<AiAuditLogData>>(query.AppendTo("admin/ai/audit"), cancellationToken);
        }

        public Task<ApiResponse<AiAuditLogData>> GetAuditAsync(string auditLogId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<AiAuditLogData>($"admin/ai/audit/{Guard.Segment(auditLogId, nameof(auditLogId))}", cancellationToken);
        }
    }
}
