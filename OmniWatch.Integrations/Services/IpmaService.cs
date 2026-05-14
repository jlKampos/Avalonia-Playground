using OmniWatch.Integrations.Contracts.Awarness;
using OmniWatch.Integrations.Contracts.Forecast;
using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Integrations.Contracts.Precipitation;
using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Integrations.Contracts.Weather;
using OmniWatch.Integrations.Contracts.Wind;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Localization;

namespace OmniWatch.Integrations.Services
{
    public class IpmaService : IIpmaService
    {
        private readonly IApiClient _apiClient;

        public IpmaService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<LocationsResponse> GetLocationsAsync()
        {
            try
            {
                return await _apiClient.GetAsync<LocationsResponse>(
                    "distrits-islands.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(IL.Translation("Ipma_LoadLocationsFailed"), ex);
            }
        }

        public async Task<WeatherTypeResponse> GetWeatherTypesAsync()
        {
            try
            {
                return await _apiClient.GetAsync<WeatherTypeResponse>(
                    "weather-type-classe.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(IL.Translation("Ipma_LoadWeatherTypesFailed"), ex);
            }
        }

        public async Task<ForecastResponse> GetForecastByCityAsync(int globalId)
        {
            try
            {
                return await _apiClient.GetAsync<ForecastResponse>(
                    $"forecast/meteorology/cities/daily/{globalId}.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(
                    string.Format(IL.Translation("Ipma_LoadForecastCityFailed"), globalId), ex);
            }
        }

        public async Task<ForecastByDayResponse> GetForecastByDayAsync(int day)
        {
            try
            {
                return await _apiClient.GetAsync<ForecastByDayResponse>(
                    $"forecast/meteorology/cities/daily/hp-daily-forecast-day{day}.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(
                    string.Format(IL.Translation("Ipma_LoadForecastDayFailed"), day), ex);
            }
        }

        public async Task<WindSpeedResponse> GetWindAsync()
        {
            try
            {
                return await _apiClient.GetAsync<WindSpeedResponse>(
                    "wind-speed-daily-classe.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(IL.Translation("Ipma_LoadWindFailed"), ex);
            }
        }

        public async Task<SeismicResponse> GetSeismicAsync(int idArea)
        {
            try
            {
                return await _apiClient.GetAsync<SeismicResponse>(
                    $"observation/seismic/{idArea}.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(
                    string.Format(IL.Translation("Ipma_LoadSeismicFailed"), idArea), ex);
            }
        }

        public async Task<List<AwarenessItem>> GetAwarnessAsync()
        {
            try
            {
                return await _apiClient.GetAsync<List<AwarenessItem>>(
                    "forecast/warnings/warnings_www.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(IL.Translation("Ipma_LoadWarningsFailed"), ex);
            }
        }

        public async Task<PrecipitationResponse> GetPrecipitationTypesAsync()
        {
            try
            {
                return await _apiClient.GetAsync<PrecipitationResponse>(
                    "precipitation-classe.json", ApiType.Ipma).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException(IL.Translation("Ipma_LoadPrecipitationFailed"), ex);
            }
        }
    }
}
