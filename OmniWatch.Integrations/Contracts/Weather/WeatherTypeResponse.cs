using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Weather
{
    public class WeatherTypeResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("data")]
        public List<WeatherTypeItem> Data { get; set; } = new();
    }
}
