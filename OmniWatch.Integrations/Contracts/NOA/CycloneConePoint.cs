using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class CycloneConePoint
    {
        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = string.Empty;

        [JsonIgnore]
        public double LatitudeValue =>
            double.TryParse(Latitude, out var v) ? v : 0;

        [JsonIgnore]
        public double LongitudeValue =>
            double.TryParse(Longitude, out var v) ? v : 0;
    }
}
