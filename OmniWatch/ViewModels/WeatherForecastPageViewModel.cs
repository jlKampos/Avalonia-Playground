using AutoMapper;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Data;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Models.Awarness;
using OmniWatch.Models.Forecast;
using OmniWatch.Models.Locations;
using OmniWatch.Models.Precipitation;
using OmniWatch.Models.Weather;
using OmniWatch.Models.Wind;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class WeatherForecastPageViewModel : PageViewModel, IAsyncPage
    {
        private readonly IMapper _mapper;
        private readonly IIpmaService _apiClient;
        private readonly IMessageService _messageService;

        private List<WeatherTypeDto> WeatherTypes { get; set; } = new();
        private List<WindSpeedDto> WindSpeeds { get; set; } = new();
        private List<AwarnessItemDto> AwarnessTypes { get; set; } = new();
        private List<PrecipitationDto> PrecepitationTypes { get; set; } = new();

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        [ObservableProperty]
        private ForecastItemDto? _selectedTab;

        [ObservableProperty]
        private LocationDto? _selectedLocation;

        [ObservableProperty]
        private AwarnessItemDto _awarness;


        public Window? OwnerWindow { get; set; }

        public ObservableCollection<LocationDto> Locations { get; } = new();

        public ObservableCollection<ForecastItemDto> Forecasts { get; } = new();


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
        }

        public async Task LoadAsync()
        {
            try
            {
                await Task.WhenAll(
                 LoadPrecipitationAsync(),
                 LoadWindAsync(),
                 LoadLocationsAsync(),
                 LoadWeatherTypesAsync(),
                 LoadAwarnessAsync()
             );
            }
            catch (ApiException ex)
            {
                var exMsg = "Error loading data";

                if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.InnerException.Message))
                {
                    exMsg = ex.InnerException.InnerException.Message;
                }

                await _messageService.ShowAsync($"Startup Error: {exMsg}", MessageDialogType.Error);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync($"Startup Error: {ex.Message}", MessageDialogType.Error);
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

                AwarnessTypes = new List<AwarnessItemDto>
                {
                    new AwarnessItemDto
                    {
                        Area = "Braga",
                        Type = "Wind",
                        Level = "Yellow",
                        StartTime = DateTime.Now.AddHours(-1),
                        EndTime = DateTime.Now.AddHours(5),
                        Text = "Strong winds expected in the area."
                    },

                    new AwarnessItemDto
                    {
                        Area = "Braga",
                        Type = "Wind",
                        Level = "Red",
                        StartTime = DateTime.Now.AddHours(-1),
                        EndTime = DateTime.Now.AddHours(5),
                        Text = "Strong winds expected in the area."
                    }
                };

                Forecasts[0].AwarnessInformation.AddRange(AwarnessTypes);
                SelectedLocation = Locations.First();
                SelectedTab = Forecasts.First();

                return;
            }
        }

        // =========================
        // EVENTS
        // =========================
        partial void OnSelectedLocationChanged(LocationDto? value)
        {
            if (value != null)
            {
                _ = LoadDataOrchestratorAsync(value);
            }
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

        private async Task LoadPrecipitationAsync()
        {
            WindSpeeds.Clear();
            var response = await _apiClient.GetPrecipitationTypesAsync();

            if (response?.Data != null)
            {
                var mapped = _mapper.Map<List<PrecipitationDto>>(response.Data);
                foreach (var item in mapped) PrecepitationTypes.Add(item);
            }
        }

        private async Task LoadLocationsAsync()
        {
            Locations.Clear();
            var response = await _apiClient.GetLocationsAsync();

            if (response?.Data != null)
            {
                var mapped = _mapper.Map<List<LocationDto>>(response.Data);

                // Ordena apenas por nome para ser fácil de encontrar
                var ordered = mapped.OrderBy(x => x.Name).ToList();

                foreach (var item in ordered)
                {
                    Locations.Add(item);
                }
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
                    // Weather
                    item.WeatherInformation = WeatherTypes.FirstOrDefault(x => x.IdWeatherType == item.WeatherTypeId)
                        ?? new WeatherTypeDto { DescriptionPT = "Unknown" };

                    // Wind
                    item.WindInformation = WindSpeeds.FirstOrDefault(x => x.ClassWindSpeedValue == item.WindSpeedClass)
                        ?? new WindSpeedDto { DescriptionPT = "N/A" };

                    // Precipitation
                    item.PrecipitationInformation = PrecepitationTypes.FirstOrDefault(x => x.IntensityLevel == item.PrecipitationIntensityClass)
                        ?? new PrecipitationDto { DescriptionPT = "---", IntensityLevel = -99 };

                    // Link awareness alerts to this specific forecast day/area
                    item.AwarnessInformation = AwarnessTypes
                       .Where(a =>
                       {
                           if (SelectedLocation is LocationDto selectedCity)
                           {
                               return item.Date.Date >= a.StartTime.Date &&
                                      item.Date.Date <= a.EndTime.Date &&
                                      selectedCity.IdAreaAviso == a.Area;
                           }
                           return false;
                       })
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