using System;
using System.Threading;
using System.Threading.Tasks;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Teams
{
    // The backend limits team access by policy, so a signed-in user without the right gets 403.
    public class TeamService
    {
        private readonly ApiClient _http;

        public TeamService(ApiClient http)
        {
            _http = http;
        }

        public Task<ApiResponse<PaginatedResponse<TeamData>>> ListAsync(
            int page = 1,
            int? perPage = null,
            CancellationToken cancellationToken = default)
        {
            var path = new QueryBuilder()
                .Add("page", page)
                .Add("per_page", perPage)
                .AppendTo("teams");

            return _http.GetAsync<PaginatedResponse<TeamData>>(path, cancellationToken);
        }

        public Task<ApiResponse<TeamData>> GetAsync(string teamId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<TeamData>($"teams/{Guard.Segment(teamId, nameof(teamId))}", cancellationToken);
        }

        public Task<ApiResponse<TeamData>> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PostAsync<TeamData>("teams", request, cancellationToken);
        }

        public Task<ApiResponse<TeamData>> UpdateAsync(string teamId, UpdateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PatchAsync<TeamData>($"teams/{Guard.Segment(teamId, nameof(teamId))}", request.ToPayload(), cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteAsync(string teamId, CancellationToken cancellationToken = default)
        {
            return _http.DeleteAsync($"teams/{Guard.Segment(teamId, nameof(teamId))}", cancellationToken);
        }
    }
}
