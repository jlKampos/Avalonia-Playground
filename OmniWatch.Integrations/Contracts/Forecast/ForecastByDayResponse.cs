using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Forecast
{
    public class ForecastByDayResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("forecastDate")]
        public DateTime ForecastDate { get; set; }

        [JsonPropertyName("data")]
        public JsonElement DataRaw { get; set; }

        [JsonIgnore]
        public List<ForecastByDayItem> Data
        {
            get
            {
                if (DataRaw.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<ForecastByDayItem>>(DataRaw.GetRawText())
                           ?? new List<ForecastByDayItem>();
                }

                return new List<ForecastByDayItem>();
            }
        }
    }
}