namespace OmniWatch.Integrations.Contracts.Seismic
{
    using System.Text.Json.Serialization;

    public class SeismicItem
    {
        [JsonPropertyName("googlemapref")]
        public string? GoogleMapRef { get; set; }

        [JsonPropertyName("degree")]
        public string? Degree { get; set; }

        [JsonPropertyName("sismoId")]
        public string? SismoId { get; set; }

        [JsonPropertyName("dataUpdate")]
        public DateTime DataUpdate { get; set; }

        [JsonPropertyName("magType")]
        public string? MagnitudeType { get; set; }

        [JsonPropertyName("obsRegion")]
        public string? ObservedRegion { get; set; }

        [JsonPropertyName("lon")]
        public string Longitude { get; set; } = "0";

        [JsonPropertyName("lat")]
        public string Latitude { get; set; } = "0";

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("depth")]
        public int Depth { get; set; }

        [JsonPropertyName("tensorRef")]
        public string? TensorRef { get; set; }

        // Alterado para object? porque o IPMA por vezes envia bool ou null
        [JsonPropertyName("sensed")]
        public object? Sensed { get; set; }

        [JsonPropertyName("shakemapid")]
        public string? ShakemapId { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("shakemapref")]
        public string? ShakemapRef { get; set; }

        [JsonPropertyName("local")]
        public string? Local { get; set; }

        [JsonPropertyName("magnitud")]
        public string Magnitude { get; set; } = "0";

    }
}
