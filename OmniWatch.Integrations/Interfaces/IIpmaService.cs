using OmniWatch.Integrations.Contracts.Awarness;
using OmniWatch.Integrations.Contracts.Forecast;
using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Integrations.Contracts.Precipitation;
using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Integrations.Contracts.Weather;
using OmniWatch.Integrations.Contracts.Wind;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IIpmaService
    {

        Task<PrecipitationResponse> GetPrecipitationTypesAsync();
        Task<List<AwarenessItem>> GetAwarnessAsync();
        Task<LocationsResponse> GetLocationsAsync();
        Task<WeatherTypeResponse> GetWeatherTypesAsync();
        Task<ForecastResponse> GetForecastByCityAsync(int globalId);
        Task<ForecastByDayResponse> GetForecastByDayAsync(int day);
        Task<WindSpeedResponse> GetWindAsync();
        Task<SeismicResponse> GetSeismicAsync(int idArea);
    }
}
