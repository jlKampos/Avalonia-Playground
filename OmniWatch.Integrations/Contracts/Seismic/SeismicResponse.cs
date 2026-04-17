using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Seismic
{
    public class SeismicResponse
    {
        [JsonPropertyName("idArea")]
        public int IdArea { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; } = String.Empty;

        [JsonPropertyName("lastSismicActivityDate")]
        public DateTime LastSismicActivityDate { get; set; }

        [JsonPropertyName("updateDate")]
        public DateTime UpdateDate { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; } = String.Empty;

        [JsonPropertyName("data")]
        public List<SeismicItem> Data { get; set; } = new();
    }
}
