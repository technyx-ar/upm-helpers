using System;
using System.Globalization;
using System.Text;

namespace Technyx.Sdk.Http
{
    internal static class Guard
    {
        internal static string Segment(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An identifier is required.", name);

            return Uri.EscapeDataString(id);
        }
    }

    internal sealed class QueryBuilder
    {
        private readonly StringBuilder _query = new StringBuilder();

        internal QueryBuilder Add(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                return this;

            _query.Append(_query.Length == 0 ? '?' : '&')
                .Append(Uri.EscapeDataString(name))
                .Append('=')
                .Append(Uri.EscapeDataString(value));
            return this;
        }

        internal QueryBuilder Add(string name, int? value)
        {
            return value == null ? this : Add(name, value.Value.ToString(CultureInfo.InvariantCulture));
        }

        internal QueryBuilder Add(string name, double? value)
        {
            return value == null ? this : Add(name, value.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal QueryBuilder Add(string name, DateTimeOffset? value)
        {
            return value == null ? this : Add(name, FormatInstant(value.Value));
        }

        internal static string FormatInstant(DateTimeOffset value)
        {
            return value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        internal string AppendTo(string path) => path + _query;
    }
}
