using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Wind
{
    public class WindSpeedItem
    {
        [JsonPropertyName("classWindSpeed")]
        public string ClassWindSpeed { get; set; } = String.Empty;

        [JsonPropertyName("descClassWindSpeedDailyPT")]
        public string DescriptionPT { get; set; } = String.Empty;

        [JsonPropertyName("descClassWindSpeedDailyEN")]
        public string DescriptionEN { get; set; } = String.Empty;

    }
}
