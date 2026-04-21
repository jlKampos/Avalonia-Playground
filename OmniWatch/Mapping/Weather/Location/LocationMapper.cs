using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Models.Locations;

namespace OmniWatch.Mapping.Weather.Location
{
    public static class LocationMapper
    {
        public static LocationDto ToDto(this LocationItem src)
        {
            return new LocationDto
            {
                GlobalIdLocal = src.GlobalIdLocal,
                Name = src.Local,
                IdAreaAviso = src.IdAreaAviso,

                Latitude = src.Latitude,
                Longitude = src.Longitude
            };
        }
    }
}
