using OmniWatch.Integrations.Contracts.Precipitation;
using OmniWatch.Models.Precipitation;

namespace OmniWatch.Mapping.Weather.Precipitation
{
    public static class PrecipitationMapper
    {
        public static PrecipitationDto ToDto(this PrecipitationItem src)
        {
            return new PrecipitationDto
            {
                DescriptionEN = src.DescClassPrecIntEn,
                DescriptionPT = src.DescClassPrecIntPt,
                IntensityLevel = int.TryParse(src.ClassPrecInt, out var v) ? v : -99
            };
        }
    }
}
