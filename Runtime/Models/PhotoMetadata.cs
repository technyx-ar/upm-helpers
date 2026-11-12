using System;
using System.Collections.Generic;
using Technyx.Sdk.Http;
using Technyx.Sdk.Photos;

namespace Technyx.Sdk.Models
{
    public sealed class PhotoMetadata
    {
        public string Name;
        public string Description;
        public DateTimeOffset? Timestamp;
        public double? GpsLat;
        public double? GpsLng;
        public List<string> Tags;

        // Free-form keys stored in the photo's meta; they cannot reuse the names of the fields above.
        public Dictionary<string, object> Extra;

        internal Dictionary<string, object> ToPayload()
        {
            var payload = new Dictionary<string, object>();

            if (Name != null)
                payload["name"] = Name;

            if (Description != null)
                payload["description"] = Description;

            if (Timestamp != null)
                payload["timestamp"] = QueryBuilder.FormatInstant(Timestamp.Value);

            if (GpsLat != null)
                payload["gps_lat"] = GpsLat.Value;

            if (GpsLng != null)
                payload["gps_lng"] = GpsLng.Value;

            if (Tags != null)
                payload["tags"] = Tags;

            PhotoPayload.AddExtra(payload, Extra);
            return payload;
        }
    }
}
