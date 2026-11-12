using System;
using System.Collections.Generic;
using Technyx.Sdk.Http;

namespace Technyx.Sdk.Models
{
    public sealed class PhotoFilter
    {
        public string Name;

        public string Description;

        public double? GpsLat;
        public double? GpsLng;

        public DateTimeOffset? Timestamp;
        public DateTimeOffset? TimestampFrom;
        public DateTimeOffset? TimestampTo;

        public Dictionary<string, string> Meta;

        internal QueryBuilder AppendTo(QueryBuilder query)
        {
            query
                .Add("name", Name)
                .Add("description", Description)
                .Add("gps_lat", GpsLat)
                .Add("gps_lng", GpsLng)
                .Add("timestamp", Timestamp)
                .Add("timestamp_from", TimestampFrom)
                .Add("timestamp_to", TimestampTo);

            if (Meta != null)
            {
                var keys = new List<string>(Meta.Keys);
                keys.Sort(StringComparer.Ordinal);

                foreach (var key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        throw new ArgumentException("Meta filter keys must not be empty.", nameof(Meta));

                    query.Add("meta[" + key + "]", Meta[key]);
                }
            }

            return query;
        }
    }
}
