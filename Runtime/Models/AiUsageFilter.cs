using System;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Models
{
    // From and To are sent as UTC calendar dates and both days are included. Without them the backend
    // reports the current month up to today.
    public sealed class AiUsageFilter
    {
        public DateTimeOffset? From;
        public DateTimeOffset? To;
        public string Provider;

        // One of the AiAuditAction constants.
        public string Action;

        // ULID of the user whose requests are counted.
        public string UserId;

        internal QueryBuilder AppendTo(QueryBuilder query)
        {
            return query
                .AddUtcDate("from", From)
                .AddUtcDate("to", To)
                .Add("provider", Provider)
                .Add("action", Action)
                .Add("user_ulid", UserId);
        }
    }
}
