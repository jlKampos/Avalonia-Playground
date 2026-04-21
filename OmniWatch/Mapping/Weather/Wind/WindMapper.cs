using OmniWatch.Integrations.Contracts.Wind;
using OmniWatch.Models.Wind;

namespace OmniWatch.Mapping.Weather.Wind
{
    public static class WindMapper
    {
        public static WindSpeedDto ToDto(this WindSpeedItem src)
        {
            return new WindSpeedDto
            {
                ClassWindSpeed = src.ClassWindSpeed,
                DescriptionPT = src.DescriptionPT,
                DescriptionEN = src.DescriptionEN
            };
        }
    }
}
