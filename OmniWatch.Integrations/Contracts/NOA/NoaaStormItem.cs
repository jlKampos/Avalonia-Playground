using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class NoaaStormItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = string.Empty;

        [JsonPropertyName("wind")]
        public int Wind { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        [JsonPropertyName("category")]
        public int Category { get; set; }

        [JsonPropertyName("forecast")]
        public List<NoaaForecastItem> Forecast { get; set; } = new();

        [JsonPropertyName("cone")]
        public List<NoaaConeItem> Cone { get; set; } = new();

        [JsonIgnore]
        public double LatitudeValue => double.TryParse(Latitude, out var v) ? v : 0;

        [JsonIgnore]
        public double LongitudeValue => double.TryParse(Longitude, out var v) ? v : 0;
    }
}
