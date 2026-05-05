using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Weather
{
    public class WeatherTypeResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement DataRaw { get; set; }

        [JsonIgnore]
        public List<WeatherTypeItem> Data
        {
            get
            {
                if (DataRaw.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<WeatherTypeItem>>(DataRaw.GetRawText())
                           ?? new List<WeatherTypeItem>();
                }

                // {}, null, etc
                return new List<WeatherTypeItem>();
            }
        }
    }
}