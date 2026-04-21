using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class NoaaConePoint
    {
        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = string.Empty;

        [JsonIgnore]
        public double LatitudeValue => double.TryParse(Latitude, out var v) ? v : 0;

        [JsonIgnore]
        public double LongitudeValue => double.TryParse(Longitude, out var v) ? v : 0;
    }
}
