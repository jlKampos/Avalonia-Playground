using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Forecast
{
    public class ForecastResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("globalIdLocal")]
        public int GlobalIdLocal { get; set; }

        [JsonPropertyName("dataUpdate")]
        public DateTime DataUpdate { get; set; }

        [JsonPropertyName("data")]
        public List<ForecastItem> Data { get; set; } = new();
    }
}
