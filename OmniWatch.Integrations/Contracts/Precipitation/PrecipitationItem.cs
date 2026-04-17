using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Precipitation
{
    public class PrecipitationItem
    {
        [JsonPropertyName("descClassPrecIntEN")]
        public string DescClassPrecIntEn { get; set; }

        [JsonPropertyName("descClassPrecIntPT")]
        public string DescClassPrecIntPt { get; set; }

        [JsonPropertyName("classPrecInt")]
        public string ClassPrecInt { get; set; }
    }
}
