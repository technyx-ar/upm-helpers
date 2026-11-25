using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Technyx.Sdk.Models
{
    [Serializable]
    public class AiGroundingData
    {
        [JsonProperty("grounded_ratio")]
        public double? GroundedRatio;

        [JsonProperty("grounded_segments")]
        public List<AiGroundedSegment> GroundedSegments;

        [JsonProperty("generated_segments")]
        public List<AiGeneratedSegment> GeneratedSegments;
    }
}
