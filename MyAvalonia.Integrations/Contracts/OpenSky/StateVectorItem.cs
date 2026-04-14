using System.Text.Json.Serialization;

namespace MyAvalonia.Integrations.Contracts.OpenSky
{
    public class StateVectorItem
    {
        [JsonPropertyName("icao24")]
        public string? Icao24 { get; set; }

        [JsonPropertyName("callsign")]
        public string? Callsign { get; set; }

        [JsonPropertyName("origin_country")]
        public string? OriginCountry { get; set; }

        [JsonPropertyName("time_position")]
        public long? TimePosition { get; set; }

        [JsonPropertyName("last_contact")]
        public long? LastContact { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [JsonPropertyName("on_ground")]
        public bool? OnGround { get; set; }

        [JsonPropertyName("velocity")]
        public double? Velocity { get; set; }

        [JsonPropertyName("true_track")]
        public double? TrueTrack { get; set; }

        [JsonPropertyName("vertical_rate")]
        public double? VerticalRate { get; set; }

        [JsonPropertyName("sensors")]
        public int[]? Sensors { get; set; }

        [JsonPropertyName("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [JsonPropertyName("squawk")]
        public string? Squawk { get; set; }

        [JsonPropertyName("spi")]
        public bool? Spi { get; set; }

        [JsonPropertyName("position_source")]
        public int? PositionSource { get; set; }
    }
}
