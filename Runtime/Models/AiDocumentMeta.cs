using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiDocumentMeta
    {
        [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)]
        public string Filename;

        [JsonProperty("file_url", NullValueHandling = NullValueHandling.Ignore)]
        public string FileUrl;

        [JsonProperty("pages", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Pages;

        [JsonProperty("document_type", NullValueHandling = NullValueHandling.Ignore)]
        public string DocumentType;

        [JsonProperty("chapter", NullValueHandling = NullValueHandling.Ignore)]
        public string Chapter;
    }
}
