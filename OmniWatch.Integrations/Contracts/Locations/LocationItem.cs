using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Locations
{
    public class LocationItem
    {
        [JsonPropertyName("idRegiao")]
        public int IdRegiao { get; set; }

        [JsonPropertyName("idAreaAviso")]
        public string IdAreaAviso { get; set; } = String.Empty;

        [JsonPropertyName("idConcelho")]
        public int IdConcelho { get; set; }

        [JsonPropertyName("globalIdLocal")]
        public int GlobalIdLocal { get; set; }

        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = String.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = String.Empty;

        [JsonPropertyName("idDistrito")]
        public int IdDistrito { get; set; }

        [JsonPropertyName("local")]
        public string Local { get; set; } = String.Empty;

        [JsonIgnore]
        public double LatitudeValue => double.TryParse(Latitude, out var v) ? v : 0;

        [JsonIgnore]
        public double LongitudeValue => double.TryParse(Longitude, out var v) ? v : 0;
    }
}
