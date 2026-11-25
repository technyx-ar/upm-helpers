using System;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiStreamChunk
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("index")]
        public int Index;
    }
}
