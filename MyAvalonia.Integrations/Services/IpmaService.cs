using MyAvalonia.Integrations.Contracts.Awarness;
using MyAvalonia.Integrations.Contracts.Forecast;
using MyAvalonia.Integrations.Contracts.Locations;
using MyAvalonia.Integrations.Contracts.Precipitation;
using MyAvalonia.Integrations.Contracts.Seismic;
using MyAvalonia.Integrations.Contracts.Weather;
using MyAvalonia.Integrations.Contracts.Wind;
using MyAvalonia.Integrations.Exceptions;
using MyAvalonia.Integrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyAvalonia.Integrations.Services
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
				return await _apiClient.GetAsync<LocationsResponse>("distrits-islands.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException("Failed to load locations", ex);
			}
		}

		public async Task<WeatherTypeResponse> GetWeatherTypesAsync()
		{
			try
			{
				return await _apiClient.GetAsync<WeatherTypeResponse>("weather-type-classe.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException("Failed to load weather types", ex);
			}
		}

		public async Task<ForecastResponse> GetForecastByCityAsync(int globalId)
		{
			try
			{
				return await _apiClient.GetAsync<ForecastResponse>(
					$"forecast/meteorology/cities/daily/{globalId}.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException($"Failed to load forecast for city {globalId}", ex);
			}
		}

		public async Task<ForecastByDayResponse> GetForecastByDayAsync(int day)
		{
			try
			{
				return await _apiClient.GetAsync<ForecastByDayResponse>(
					$"forecast/meteorology/cities/daily/hp-daily-forecast-day{day}.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException($"Failed to load forecast for day {day}", ex);
			}
		}

		public async Task<WindSpeedResponse> GetWindAsync()
		{
			try
			{
				return await _apiClient.GetAsync<WindSpeedResponse>("wind-speed-daily-classe.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException("Failed to load wind speed data", ex);
			}
		}

		public async Task<SeismicResponse> GetSeismicAsync(int idArea)
		{
			try
			{
				return await _apiClient.GetAsync<SeismicResponse>($"observation/seismic/{idArea}.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException($"Failed to load seismic data for area {idArea}", ex);
			}
		}

		public async Task<List<AwarenessItem>> GetAwarnessAsync()
		{
			try
			{
				return await _apiClient
					.GetAsync<List<AwarenessItem>>("forecast/warnings/warnings_www.json")
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException("Failed to load warnings", ex);
			}
		}

		public async Task<PrecipitationResponse> GetPrecipitationTypesAsync()
		{
			try
			{
				return await _apiClient.GetAsync<PrecipitationResponse>("precipitation-classe.json").ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new ApiException("Failed to load precepitation data", ex);
			}
		}
	}
}
