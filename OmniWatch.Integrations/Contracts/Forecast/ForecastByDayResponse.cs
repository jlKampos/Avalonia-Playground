using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Forecast
{
    public class ForecastByDayResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("forecastDate")]
        public DateTime ForecastDate { get; set; }

        [JsonPropertyName("data")]
        public List<ForecastByDayItem> Data { get; set; } = new();
    }
}
