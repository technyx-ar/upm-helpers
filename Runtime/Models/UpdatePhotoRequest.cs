using System;
using System.Collections.Generic;
using Technyx.Sdk.Photos;

namespace Technyx.Sdk.Models
{
    // The backend replaces the stored meta with whatever extra keys the update carries, so a request
    // built with `new` and no Extra empties it. ForPhoto starts from the current meta to avoid that.
    public sealed class UpdatePhotoRequest
    {
        public string Name;

        // An empty string leaves the current description in place, the backend cannot clear it.
        public string Description;

        // null keeps the current tags, an empty list removes them all.
        public List<string> Tags;

        public Dictionary<string, object> Extra;

        public static UpdatePhotoRequest ForPhoto(PhotoData photo)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));

            var request = new UpdatePhotoRequest();

            if (photo.Meta != null)
            {
                request.Extra = new Dictionary<string, object>();
                foreach (var pair in photo.Meta)
                    request.Extra[pair.Key] = pair.Value;
            }

            return request;
        }

        internal Dictionary<string, object> ToPayload()
        {
            var metadata = new Dictionary<string, object>();

            if (Name != null)
                metadata["name"] = Name;

            if (Description != null)
                metadata["description"] = Description;

            if (Tags != null)
                metadata["tags"] = Tags;

            PhotoPayload.AddExtra(metadata, Extra);
            return new Dictionary<string, object> { { "metadata", metadata } };
        }
    }
}
