using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Wind
{
    public class WindSpeedResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("data")]
        public List<WindSpeedItem> Data { get; set; } = new();
    }
}
