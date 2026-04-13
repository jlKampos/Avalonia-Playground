using AutoMapper;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MyAvalonia.Data;
using MyAvalonia.Integrations.Exceptions;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Interfaces;
using MyAvalonia.Models.Awarness;
using MyAvalonia.Models.Forecast;
using MyAvalonia.Models.Locations;
using MyAvalonia.Models.Weather;
using MyAvalonia.Models.Wind;
using MyAvalonia.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static MyAvalonia.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace MyAvalonia.ViewModels
{
	public partial class WeatherForecastPageViewModel : PageViewModel
	{
		private readonly IMapper _mapper;
		private readonly IIpmaService _apiClient;
		private readonly IMessageService _messageService;
		public Window? OwnerWindow { get; set; }

		[ObservableProperty]
		private ProgressControlViewModel _progressControl = new();

		[ObservableProperty]
		private ForecastItemDto? _selectedTab;

		[ObservableProperty]
		private LocationDto _selectedLocation;

		[ObservableProperty]
		private AwarnessItemDto _awarness;

		public ObservableCollection<LocationDto> Locations { get; } = new();

		public ObservableCollection<ForecastItemDto> Forecasts { get; } = new();

		private List<WeatherTypeDto> WeatherTypes { get; set; } = new();
		private List<WindSpeedDto> WindSpeeds { get; set; } = new();

		private List<AwarnessItemDto> AwarnessTypes { get; set; } = new();

		// =========================
		// RUNTIME CONSTRUCTOR
		// =========================
		public WeatherForecastPageViewModel(ProgressControlViewModel progressControl, IMessageService messageService, IIpmaService apiClient, IMapper mapper)
		{
			PageName = ApplicationPageNames.WeatherForecast;
			_progressControl = progressControl;
			_messageService = messageService;
			_mapper = mapper;
			_apiClient = apiClient;
			_ = InitializeAsync();
		}

		private async Task InitializeAsync()
		{
			try
			{
				// 1. Setup and show the progress indicator
				ProgressControl.IsVisible = true;
				ProgressControl.Title = "Loading";
				ProgressControl.Message = "Initialising application data...";

				// 2. Execute all initial data loads in parallel for better performance
				await Task.WhenAll(
					LoadWindAsync(),
					LoadLocationsAsync(),
					LoadWeatherTypesAsync(),
					LoadAwarnessAsync()
				);
			}
			catch (ApiException ex)
			{
				// 3. Catch any Api specific error

				var exMsg = "Error loading data";

				if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.InnerException.Message))
				{
					exMsg = ex.InnerException.InnerException.Message;
				}

				await _messageService.ShowAsync($"Startup Error: {exMsg}", MessageDialogType.Error);
			}
			catch (Exception ex)
			{
				// 3. Catch any error from the parallel tasks and notify the user
				await _messageService.ShowAsync($"Startup Error: {ex.Message}", MessageDialogType.Error);
			}
			finally
			{
				// 4. Ensure the progress overlay is hidden regardless of success or failure
				ProgressControl.IsVisible = false;
			}
		}

		// =========================
		// DESIGN MODE CONSTRUCTOR
		// =========================
		public WeatherForecastPageViewModel()
		{
			if (Design.IsDesignMode)
			{
				PageName = ApplicationPageNames.WeatherForecast;

				Locations.Clear();
				Locations.Add(new LocationDto { GlobalIdLocal = 1, Name = "Braga" });
				Locations.Add(new LocationDto { GlobalIdLocal = 2, Name = "Porto" });

				WeatherTypes = new List<WeatherTypeDto>
				{
					new WeatherTypeDto { IdWeatherType = 1, DescriptionPT = "Limpo"},
					new WeatherTypeDto { IdWeatherType = 10, DescriptionPT = "Chuva" }
				};

				WindSpeeds = new List<WindSpeedDto>
				{
					new WindSpeedDto { ClassWindSpeed = "1", DescriptionPT = "NW" }
				};

				Forecasts.Clear();

				Forecasts.Add(new ForecastItemDto
				{
					DisplayDate = "03/04/2014",
					WeatherTypeId = 1,
					WeatherInformation = WeatherTypes[0]
				});

				Forecasts.Add(new ForecastItemDto
				{
					DisplayDate = "04/04/2014",
					WeatherTypeId = 10,
					WeatherInformation = WeatherTypes[1]
				});

				SelectedLocation = Locations.First();
				SelectedTab = Forecasts.First();

				return;
			}
		}

		// =========================
		// EVENTS
		// =========================
		partial void OnSelectedLocationChanged(LocationDto value)
		{
			if (value == null || Design.IsDesignMode) return;

			_ = LoadDataOrchestratorAsync(value);
		}

		private async Task LoadDataOrchestratorAsync(LocationDto value)
		{
			try
			{
				ProgressControl.IsVisible = true;
				ProgressControl.Title = "Loading";
				ProgressControl.Message = $"Forecast for {value.Name}...";
				// Only the heavy lifting goes to a background thread
				await Task.Run(async () =>
				{
					await Task.Delay(500); // Artificial delay
					await LoadForecastAsync(value.GlobalIdLocal);
				});
			}
			catch (Exception ex)
			{
				await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
			}
			finally
			{
				ProgressControl.IsVisible = false;
			}
		}

		#region API Methods

		private async Task LoadAwarnessAsync()
		{
			AwarnessTypes.Clear();
			var response = await _apiClient.GetAwarnessAsync();

			if (response != null)
			{
				var mapped = _mapper.Map<List<AwarnessItemDto>>(response);
				foreach (var item in mapped) AwarnessTypes.Add(item);
			}
		}

		private async Task LoadWindAsync()
		{
			WindSpeeds.Clear();
			var response = await _apiClient.GetWindAsync();

			if (response?.Data != null)
			{
				var mapped = _mapper.Map<List<WindSpeedDto>>(response.Data);
				foreach (var item in mapped) WindSpeeds.Add(item);
			}
		}

		private async Task LoadLocationsAsync()
		{
			Locations.Clear();
			var response = await _apiClient.GetLocationsAsync();

			if (response?.Data != null)
			{
				var mapped = _mapper.Map<List<LocationDto>>(response.Data);
				foreach (var item in mapped) Locations.Add(item);
			}
		}

		private async Task LoadWeatherTypesAsync()
		{
			WeatherTypes.Clear();
			var response = await _apiClient.GetWeatherTypesAsync();

			if (response?.Data != null)
			{
				var mapped = _mapper.Map<List<WeatherTypeDto>>(response.Data);
				foreach (var item in mapped) WeatherTypes.Add(item);
			}
		}

		private async Task LoadForecastAsync(int locationId)
		{
			Forecasts.Clear();
			var response = await _apiClient.GetForecastByCityAsync(locationId);

			if (response?.Data != null)
			{
				var mapped = _mapper.Map<List<ForecastItemDto>>(response.Data);
				var tempList = new List<ForecastItemDto>();

				foreach (var item in mapped)
				{
					// Map metadata from previously loaded collections
					item.WeatherInformation = WeatherTypes.FirstOrDefault(x => x.IdWeatherType == item.WeatherTypeId);
					item.WindInformation = WindSpeeds.FirstOrDefault(x => x.ClassWindSpeedValue == item.WindSpeedClass);

					// Link awareness alerts to this specific forecast day/area
					item.AwarnessInformation = AwarnessTypes
					   .Where(a => item.Date >= a.StartTime &&
								   item.Date <= a.EndTime &&
								   SelectedLocation?.IdAreaAviso == a.Area)
					   .ToList();

					tempList.Add(item);
				}

				foreach (var item in tempList) Forecasts.Add(item);
				SelectedTab = Forecasts.FirstOrDefault();

			}
		}

		#endregion
	}
}