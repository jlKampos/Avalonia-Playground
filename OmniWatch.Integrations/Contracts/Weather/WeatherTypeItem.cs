using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Weather
{
    public class WeatherTypeItem
    {
        [JsonPropertyName("idWeatherType")]
        public int IdWeatherType { get; set; }

        [JsonPropertyName("descWeatherTypePT")]
        public string DescriptionPT { get; set; } = String.Empty;

        [JsonPropertyName("descWeatherTypeEN")]
        public string DescriptionEN { get; set; } = String.Empty;

    }
}
