using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Locations
{
    public class LocationsResponse
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement DataRaw { get; set; }

        [JsonIgnore]
        public List<LocationItem> Data
        {
            get
            {
                if (DataRaw.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<LocationItem>>(DataRaw.GetRawText())
                           ?? new List<LocationItem>();
                }

                return new List<LocationItem>();
            }
            set;
        }
    }
}