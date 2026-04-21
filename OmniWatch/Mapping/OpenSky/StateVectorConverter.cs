//using OmniWatch.Integrations.Contracts.OpenSky;
//using OmniWatch.Models.OpenSky;
//using System;

//namespace OmniWatch.Mapping.OpenSky
//{
//    public static class StateVectorConverter
//    {
//        public static StateVectorDto ToDto(this StateVectorItem src)
//        {
//            return new StateVectorDto
//            {
//                TrueTrack = src.TrueTrack,
//                Icao24 = src.Icao24,
//                Callsign = src.Callsign,
//                OriginCountry = src.OriginCountry,
//                Latitude = src.Latitude,
//                Longitude = src.Longitude,
//                Altitude = src.BaroAltitude,
//                OnGround = src.OnGround,
//                Velocity = src.Velocity,
//                PositionSource = Enum.IsDefined(typeof(PositionSource), src.PositionSource ?? -1)
//                    ? (PositionSource?)src.PositionSource
//                    : null
//            };
//        }
//    }
//}
