using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Precipitation
{
    public class PrecipitationResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("data")]
        public List<PrecipitationItem> Data { get; set; } = new();
    }
}
