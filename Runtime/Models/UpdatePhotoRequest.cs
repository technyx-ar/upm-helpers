using System;
using System.Collections.Generic;
using Technyx.Sdk.Photos;

namespace Technyx.Sdk.Models
{
    // A field is sent only after it has been assigned. Description, GpsLat and GpsLng accept an explicit
    // null, which clears the stored value; leave them unassigned to keep it.
    public sealed class UpdatePhotoRequest
    {
        private string _description;
        private bool _descriptionSet;
        private double? _gpsLat;
        private bool _gpsLatSet;
        private double? _gpsLng;
        private bool _gpsLngSet;

        // null or empty keeps the current name, the backend cannot clear it.
        public string Name;

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                _descriptionSet = true;
            }
        }

        public double? GpsLat
        {
            get => _gpsLat;
            set
            {
                _gpsLat = value;
                _gpsLatSet = true;
            }
        }

        public double? GpsLng
        {
            get => _gpsLng;
            set
            {
                _gpsLng = value;
                _gpsLngSet = true;
            }
        }

        // null keeps the current tags, an empty list removes them all.
        public List<string> Tags;

        // Merged into the stored meta: unsent keys stay, a null value removes that key.
        public Dictionary<string, object> Extra;

        internal Dictionary<string, object> ToPayload()
        {
            var metadata = new Dictionary<string, object>();

            if (Name != null)
                metadata["name"] = Name;

            if (_descriptionSet)
                metadata["description"] = _description;

            if (_gpsLatSet)
                metadata["gps_lat"] = _gpsLat;

            if (_gpsLngSet)
                metadata["gps_lng"] = _gpsLng;

            if (Tags != null)
                metadata["tags"] = Tags;

            PhotoPayload.AddExtra(metadata, Extra);
            return new Dictionary<string, object> { { "metadata", metadata } };
        }
    }
}
