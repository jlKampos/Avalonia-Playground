using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Precipitation
{
    public class PrecipitationResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement DataRaw { get; set; }

        [JsonIgnore]
        public List<PrecipitationItem> Data
        {
            get
            {
                if (DataRaw.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<PrecipitationItem>>(DataRaw.GetRawText())
                           ?? new List<PrecipitationItem>();
                }

                return new List<PrecipitationItem>();
            }
            set;
        }
    }
}