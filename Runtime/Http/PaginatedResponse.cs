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

        [JsonProperty("current_page")]
        public int CurrentPage;

        [JsonProperty("last_page")]
        public int LastPage;

        [JsonProperty("per_page")]
        public int PerPage;

        [JsonProperty("total")]
        public int Total;

        [JsonProperty("from")]
        public int? From;

        [JsonProperty("to")]
        public int? To;

        public bool HasMorePages => CurrentPage < LastPage;
    }
}
