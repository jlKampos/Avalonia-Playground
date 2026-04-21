using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class CycloneItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        // posição atual (NOAA normalmente vem em lat/lon separados)
        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public int Category { get; set; }

        // intensidade (opcional no NOAA, às vezes vento em knots)
        [JsonPropertyName("windSpeed")]
        public int WindSpeed { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        // previsão (lista de pontos futuros)
        [JsonPropertyName("forecast")]
        public List<CycloneForecastItem> Forecast { get; set; } = new();

        // cone de incerteza (polígonos NOAA)
        [JsonPropertyName("cone")]
        public List<CycloneConeItem> Cone { get; set; } = new();

        // helpers (igual ao teu LocationItem)
        [JsonIgnore]
        public double LatitudeValue =>
            double.TryParse(Latitude, out var v) ? v : 0;

        [JsonIgnore]
        public double LongitudeValue =>
            double.TryParse(Longitude, out var v) ? v : 0;
    }
}
