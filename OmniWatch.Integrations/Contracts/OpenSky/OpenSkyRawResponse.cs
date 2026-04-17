using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.OpenSky
{
    public class OpenSkyRawResponse
    {
        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("states")]
        public List<List<JsonElement>>? States { get; set; }
    }
}

