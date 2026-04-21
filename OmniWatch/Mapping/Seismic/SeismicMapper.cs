using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Models.Seismic;

namespace OmniWatch.Mapping.Seismic
{
    internal static class SeismicMapper
    {
        public static SeismicActivityDto ToDto(this SeismicItem src)
        {
            return new SeismicActivityDto
            {
                GoogleMapRef = src.GoogleMapRef,
                Degree = src.Degree,
                SismoId = src.SismoId,
                MagnitudeType = src.MagnitudeType,
                ObservedRegion = src.ObservedRegion,
                Source = src.Source,
                TensorRef = src.TensorRef,
                ShakemapId = src.ShakemapId,
                ShakemapRef = src.ShakemapRef,
                Local = src.Local,

                DataUpdate = src.DataUpdate,
                Time = src.Time,
                Depth = src.Depth,

                Longitude = src.Longitude,
                Latitude = src.Latitude,
                Magnitude = src.Magnitude,

                // importante: conversão segura do "object"
                Sensed = src.Sensed?.ToString()
            };
        }
    }
}
