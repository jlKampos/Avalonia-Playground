namespace OmniWatch.Models.OpenSky
{
    public class StateVectorDto
    {
        public double? TrueTrack { get; set; }
        public string? Icao24 { get; set; }
        public string? Callsign { get; set; }
        public string? OriginCountry { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public bool? OnGround { get; set; }
        public double? Velocity { get; set; }
        public PositionSource? PositionSource { get; set; }
    }
}
