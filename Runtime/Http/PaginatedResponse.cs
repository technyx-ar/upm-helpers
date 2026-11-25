using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Http
{
    [Serializable]
    public class PaginatedResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data;

        [JsonProperty("links")]
        public PaginationLinks Links;

        [JsonProperty("meta", Required = Required.Always)]
        public PaginationMeta Meta;

        [JsonIgnore]
        public int CurrentPage => Meta.CurrentPage;

        [JsonIgnore]
        public int LastPage => Meta.LastPage;

        [JsonIgnore]
        public int PerPage => Meta.PerPage;

        [JsonIgnore]
        public int Total => Meta.Total;

        [JsonIgnore]
        public int? From => Meta.From;

        [JsonIgnore]
        public int? To => Meta.To;

        [JsonIgnore]
        public bool HasMorePages => Meta.CurrentPage < Meta.LastPage;
    }

    [Serializable]
    public class PaginationMeta
    {
        [JsonProperty("current_page", Required = Required.Always)]
        public int CurrentPage;

        [JsonProperty("last_page", Required = Required.Always)]
        public int LastPage;

        [JsonProperty("per_page", Required = Required.Always)]
        public int PerPage;

        [JsonProperty("total", Required = Required.Always)]
        public int Total;

        [JsonProperty("from")]
        public int? From;

        [JsonProperty("to")]
        public int? To;
    }

    [Serializable]
    public class PaginationLinks
    {
        [JsonProperty("first")]
        public string First;

        [JsonProperty("last")]
        public string Last;

        [JsonProperty("prev")]
        public string Prev;

        [JsonProperty("next")]
        public string Next;
    }
}
