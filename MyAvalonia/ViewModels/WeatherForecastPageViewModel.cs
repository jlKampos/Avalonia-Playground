using AutoMapper;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyAvalonia.Data;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Models.Forecast;
using MyAvalonia.Models.Locations;
using MyAvalonia.Models.Weather;
using MyAvalonia.Models.Wind;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
namespace MyAvalonia.ViewModels
{
	public partial class WeatherForecastPageViewModel : PageViewModel
	{
		private readonly IMapper _mapper;
		private readonly IIpmaService _apiClient;

		public ForecastItemDto? SelectedTab { get; set; }

		[ObservableProperty]
		private string _locationName = string.Empty;

		[ObservableProperty]
		private LocationDto _selectedLocation;

		public ObservableCollection<LocationDto> Locations { get; set; } = new();
		public ObservableCollection<ForecastItemDto> Forecasts { get; } = new();

		private List<WeatherTypeDto> WeatherTypes { get; set; } = new();

		private List<WindSpeedDto> WindSpeeds { get; set; } = new();


		public WeatherForecastPageViewModel(IIpmaService apiClient, IMapper mapper)
		{
			PageName = Data.ApplicationPageNames.WeatherForecast;
			_mapper = mapper;
			_apiClient = apiClient;

			_ = LoadWindAsync();
			_ = LoadLocationsAsync();
			_ = LoadWeatherTypesAsync();

		}

		public WeatherForecastPageViewModel()
		{
			if (Design.IsDesignMode)
			{
				PageName = ApplicationPageNames.WeatherForecast;

				Locations = new ObservableCollection<LocationDto>
				{
					new LocationDto { GlobalIdLocal = 1, Name = "Braga" },
					new LocationDto { GlobalIdLocal = 2, Name = "Porto" }
				};

				WeatherTypes = new List<WeatherTypeDto>
				{
					new WeatherTypeDto { IdWeatherType = 1, DescriptionEN = "Clear", DescriptionPT = "Limpo" },
					new WeatherTypeDto { IdWeatherType = 2, DescriptionEN = "Rain", DescriptionPT = "Chuva" }
				};

				WindSpeeds = new List<WindSpeedDto>
				{
					new WindSpeedDto { ClassWindSpeed = "1", DescriptionEN = "NW", DescriptionPT = "NW" }
				};
				return;
			}
		}

		partial void OnSelectedLocationChanged(LocationDto value)
		{
			if (value != null)
			{
				LocationName = value.Name;
				_ = LoadForecastAsync(value.GlobalIdLocal);
			}
		}


		#region API Methods

		private async Task LoadWindAsync()
		{
			try
			{
				var response = await _apiClient.GetWindAsync();
				if (response.Data != null)
				{
					var mapped = _mapper.Map<List<WindSpeedDto>>(response.Data);
					foreach (var item in mapped)
					{
						WindSpeeds.Add(item);
					}
				}
			}
			catch (Exception ex)
			{
				App.Current.HandleException(ex);
			}
		}

		private async Task LoadLocationsAsync()
		{
			try
			{
				var response = await _apiClient.GetLocationsAsync();

				if (response.Data != null)
				{
					var mapped = _mapper.Map<List<LocationDto>>(response.Data);

					foreach (var item in mapped)
					{
						Locations.Add(item);
					}
				}
			}
			catch (Exception ex)
			{

				App.Current.HandleException(ex);
			}
		}


		private async Task LoadWeatherTypesAsync()
		{
			try
			{
				var response = await _apiClient.GetWeatherTypesAsync();
				if (response.Data != null)
				{
					var mapped = _mapper.Map<List<WeatherTypeDto>>(response.Data);
					foreach (var item in mapped)
					{
						WeatherTypes.Add(item);
					}
				}

			}
			catch (Exception ex)
			{
				App.Current.HandleException(ex);
			}
		}

		private async Task LoadForecastAsync(int locationId)
		{
			try
			{
				Forecasts.Clear();

				var response = await _apiClient.GetForecastByCityAsync(locationId);

				if (response.Data != null)
				{
					var mapped = _mapper.Map<List<ForecastItemDto>>(response.Data);

					foreach (var item in mapped)
					{
						item.WeatherInformation = WeatherTypes.FirstOrDefault(x => x.IdWeatherType == item.WeatherTypeId);
						item.WindInformation = WindSpeeds.FirstOrDefault(x => x.ClassWindSpeedValue == item.WindSpeedClass);
						Forecasts.Add(item);
					}
				}

			}
			catch (Exception ex)
			{
				App.Current.HandleException(ex);
			}
		}
		#endregion
	}
}
