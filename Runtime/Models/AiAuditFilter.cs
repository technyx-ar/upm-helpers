using System;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Models
{
    // From and To are sent as UTC calendar dates and both days are included.
    public sealed class AiAuditFilter
    {
        public DateTimeOffset? From;
        public DateTimeOffset? To;
        public string Provider;

        // One of the AiAuditAction constants.
        public string Action;

        // ULID of the user who made the request.
        public string UserId;

        // 0 to 1; entries without a grounding analysis never match a ratio bound.
        public double? GroundedRatioMin;
        public double? GroundedRatioMax;

        internal QueryBuilder AppendTo(QueryBuilder query)
        {
            return query
                .AddUtcDate("from", From)
                .AddUtcDate("to", To)
                .Add("provider", Provider)
                .Add("action", Action)
                .Add("user_ulid", UserId)
                .Add("grounded_ratio_min", GroundedRatioMin)
                .Add("grounded_ratio_max", GroundedRatioMax);
        }
    }
}
