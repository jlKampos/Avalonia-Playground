using MyAvalonia.Integrations.Contracts.Awarness;
using MyAvalonia.Integrations.Contracts.Forecast;
using MyAvalonia.Integrations.Contracts.Locations;
using MyAvalonia.Integrations.Contracts.Seismic;
using MyAvalonia.Integrations.Contracts.Weather;
using MyAvalonia.Integrations.Contracts.Wind;

namespace MyAvalonia.Integrations.Interfaces
{
	public interface IIpmaService
	{
		Task<List<AwarenessItem>> GetAwarnessAsync();
		Task<LocationsResponse> GetLocationsAsync();
		Task<WeatherTypeResponse> GetWeatherTypesAsync();
		Task<ForecastResponse> GetForecastByCityAsync(int globalId);
		Task<ForecastByDayResponse> GetForecastByDayAsync(int day);
		Task<WindSpeedResponse> GetWindAsync();
		Task<SeismicResponse> GetSeismicAsync(string date);
	}
}
