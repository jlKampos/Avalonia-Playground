using OmniWatch.Integrations.Contracts.Weather;
using OmniWatch.Models.Weather;

namespace OmniWatch.Mapping.Weather.WeatherTypes
{
    public static class WeatherTypeMapper
    {
        public static WeatherTypeDto ToDto(this WeatherTypeItem src)
        {
            return new WeatherTypeDto
            {
                IdWeatherType = src.IdWeatherType,
                DescriptionPT = src.DescriptionPT,
                DescriptionEN = src.DescriptionEN
            };
        }
    }
}
