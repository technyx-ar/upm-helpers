using System;
using System.Collections.Generic;

namespace Technyx.Sdk.Photos
{
    internal static class PhotoPayload
    {
        // The backend reads these keys as the named fields, everything else becomes the photo's meta.
        private static readonly HashSet<string> NamedKeys = new HashSet<string>
        {
            "name", "description", "tags", "timestamp", "gps_lat", "gps_lng", "task_ulid",
        };

        internal static void AddExtra(Dictionary<string, object> payload, Dictionary<string, object> extra)
        {
            if (extra == null)
                return;

            foreach (var pair in extra)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Metadata keys must not be empty.", nameof(extra));

                if (NamedKeys.Contains(pair.Key))
                    throw new ArgumentException($"'{pair.Key}' is a reserved metadata key.", nameof(extra));

                payload[pair.Key] = pair.Value;
            }
        }
    }
}
