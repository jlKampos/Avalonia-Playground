using AutoMapper;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyAvalonia.Data;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Interfaces;
using MyAvalonia.Models.Forecast;
using MyAvalonia.Models.Locations;
using MyAvalonia.Models.Weather;
using MyAvalonia.Models.Wind;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static MyAvalonia.ViewModels.MessageDialog.MessageDialogBoxViewModel;
namespace MyAvalonia.ViewModels
{
	public partial class HomePageViewModel : PageViewModel
	{
		private readonly IMapper _mapper;
		private readonly IIpmaService _apiClient;
		private readonly IMessageService _messageService;
		[ObservableProperty]
		private bool _scrollerWeatherForecastVisible = false;

		[ObservableProperty]
		private LocationDto _selectedLocation;

		[ObservableProperty]
		private string _locationName = string.Empty;

		public ObservableCollection<LocationDto> Locations { get; set; } = new();
		public ObservableCollection<ForecastItemDto> Forecasts { get; } = new();

		private List<WeatherTypeDto> WeatherTypes { get; set; } = new();

		private List<WindSpeedDto> WindSpeeds { get; set; } = new();


		public HomePageViewModel(IMessageService messageService, IIpmaService apiClient, IMapper mapper)
		{
			PageName = Data.ApplicationPageNames.Home;
			_messageService = messageService;
			_mapper = mapper;
			_apiClient = apiClient;

			_ = LoadWindAsync();
			_ = LoadLocationsAsync();
			_ = LoadWeatherTypesAsync();

		}

		public HomePageViewModel()
		{
			if (Design.IsDesignMode)
			{
				PageName = ApplicationPageNames.Home;

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

		[RelayCommand]
		private async Task SelectStuff(ForecastItemDto item)
		{
			if (item == null)
				return;

			//// chamada à API
			//var details = await _apiClient.GetForecastByDayAsync(item.Date);

			//// guardar resultado
			//SelectedForecastDetails = details;
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
				await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
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

				await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
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
				await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
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
					ScrollerWeatherForecastVisible = true;
				}
				else
				{
					ScrollerWeatherForecastVisible = false;
				}

			}
			catch (Exception ex)
			{
				ScrollerWeatherForecastVisible = false;
				await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
			}
		}
		#endregion
	}
}
