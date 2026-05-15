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
        Task<PrecipitationResponse> GetPrecipitationTypesAsync(CancellationToken ct = default);

        Task<List<AwarenessItem>> GetAwarnessAsync(CancellationToken ct = default);

        Task<LocationsResponse> GetLocationsAsync(CancellationToken ct = default);

        Task<WeatherTypeResponse> GetWeatherTypesAsync(CancellationToken ct = default);

        Task<ForecastResponse> GetForecastByCityAsync(int globalId, CancellationToken ct = default);

        Task<ForecastByDayResponse> GetForecastByDayAsync(int day, CancellationToken ct = default);

        Task<WindSpeedResponse> GetWindAsync(CancellationToken ct = default);

        Task<SeismicResponse> GetSeismicAsync(int idArea, CancellationToken ct = default);
    }
}