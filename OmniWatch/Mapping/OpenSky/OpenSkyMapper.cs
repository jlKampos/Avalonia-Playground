using NetTopologySuite.Index.HPRtree;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Models.OpenSky;

namespace OmniWatch.Mapping.OpenSky
{
    public static class OpenSkyMapper
    {
        public static StateVectorDto ToDto(this StateVectorItem src)
        {
            return new StateVectorDto
            {
                Icao24 = src.Icao24,
                TrueTrack = src.TrueTrack,
                Callsign = src.Callsign,
                OriginCountry = src.OriginCountry,
                Latitude = src.Latitude,
                Longitude = src.Longitude,
                Altitude = src.BaroAltitude,
                OnGround = src.OnGround,
                Velocity = src.Velocity.HasValue ? src.Velocity.Value * 3.6 : 0,
                PositionSource = src.PositionSource.HasValue
                    ? (PositionSource?)src.PositionSource.Value
                    : null
            };
        }

    }
}