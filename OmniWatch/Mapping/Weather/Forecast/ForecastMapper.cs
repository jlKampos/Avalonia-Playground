using OmniWatch.Integrations.Contracts.Forecast;
using OmniWatch.Models.Forecast;

namespace OmniWatch.Mapping.Weather.Forecast
{
    public static class ForecastMapper
    {
        public static ForecastItemDto ToDto(this ForecastItem src)
        {
            return new ForecastItemDto
            {
                Date = src.ForecastDate,
                DisplayDate = src.ForecastDate.ToString("dd/MM/yyyy"),
                WeekDay = src.ForecastDate.ToString("dddd"),
                DayOfYear = src.ForecastDate.DayOfYear,

                PrecipitationProbability = src.PrecipitationProbability,
                TemperatureMin = src.TemperatureMin,
                TemperatureMax = src.TemperatureMax,

                PredictedWindDirection = src.PredictedWindDirection,

                WeatherTypeId = src.WeatherTypeId,
                WindSpeedClass = src.WindSpeedClass,
                PrecipitationIntensityClass = src.PrecipitationIntensityClass,

                Latitude = src.Latitude,
                Longitude = src.Longitude
            };
        }
    }
}

