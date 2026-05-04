using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.NOA.ActiveStorms
{
    public class ActiveStormItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("classification")]
        public string Classification { get; set; }

        // Usamos string para evitar o erro de conversão do JSON
        [JsonPropertyName("intensity")]
        public string Intensity { get; set; }

        [JsonPropertyName("pressure")]
        public string Pressure { get; set; }

        [JsonPropertyName("latitudeNumeric")]
        public double LatitudeNumeric { get; set; }

        [JsonPropertyName("longitudeNumeric")]
        public double LongitudeNumeric { get; set; }

        [JsonPropertyName("movementDir")]
        public int? MovementDir { get; set; }

        [JsonPropertyName("movementSpeed")]
        public int? MovementSpeed { get; set; }

        [JsonPropertyName("lastUpdate")]
        public string LastUpdate { get; set; }

        [JsonPropertyName("publicAdvisory")]
        public StormProductInfoItem PublicAdvisory { get; set; }

        [JsonPropertyName("forecastGraphics")]
        public StormProductInfoItem ForecastGraphics { get; set; }

    }
}
