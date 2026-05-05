using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Seismic
{
    public class SeismicResponse
    {
        [JsonPropertyName("idArea")]
        public int IdArea { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("lastSismicActivityDate")]
        public JsonElement LastSismicActivityDateRaw { get; set; }

        [JsonPropertyName("updateDate")]
        public DateTime UpdateDate { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement DataRaw { get; set; }

        [JsonIgnore]
        public DateTime? LastSismicActivityDate
        {
            get
            {
                if (LastSismicActivityDateRaw.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(LastSismicActivityDateRaw.GetString(), out var dt))
                    return dt;

                return null;
            }
        }

        [JsonIgnore]
        public List<SeismicItem> Data
        {
            get
            {
                if (DataRaw.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<SeismicItem>>(DataRaw.GetRawText())
                           ?? new List<SeismicItem>();
                }

                // {}, null, etc → lista vazia
                return new List<SeismicItem>();
            }
        }
    }
}

