using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class PhotoData
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("user")]
        public string User;

        [JsonProperty("task")]
        public string Task;

        [JsonProperty("tags")]
        public List<PhotoTagData> Tags;

        [JsonProperty("url")]
        public string Url;

        [JsonProperty("preview")]
        public string Preview;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("gps_lat")]
        public double? GpsLat;

        [JsonProperty("gps_lng")]
        public double? GpsLng;

        [JsonProperty("timestamp")]
        public string Timestamp;

        [JsonProperty("meta")]
        [JsonConverter(typeof(EmptyArrayAsObjectConverter))]
        public Dictionary<string, JToken> Meta;

        [JsonProperty("created_at")]
        public string CreatedAt;

        [JsonProperty("updated_at")]
        public string UpdatedAt;
    }
}
