using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Data;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Localization;
using OmniWatch.Mapping.Weather.Awarness;
using OmniWatch.Mapping.Weather.Forecast;
using OmniWatch.Mapping.Weather.Location;
using OmniWatch.Mapping.Weather.Precipitation;
using OmniWatch.Mapping.Weather.WeatherTypes;
using OmniWatch.Mapping.Weather.Wind;
using OmniWatch.Models.IPMA.Awarness;
using OmniWatch.Models.IPMA.Forecast;
using OmniWatch.Models.IPMA.Locations;
using OmniWatch.Models.IPMA.Precipitation;
using OmniWatch.Models.IPMA.Weather;
using OmniWatch.Models.IPMA.Wind;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class WeatherForecastPageViewModel : PageViewModel, IAsyncPage, IDisposable
    {
        private readonly IIpmaService _apiClient;
        private readonly IMessageService _messageService;
        private CancellationTokenSource? _cts;

        private List<WeatherTypeDto> WeatherTypes { get; set; } = new();
        private List<WindSpeedDto> WindSpeeds { get; set; } = new();
        private List<AwarnessItemDto> AwarnessTypes { get; set; } = new();
        private List<PrecipitationDto> PrecepitationTypes { get; set; } = new();

        [ObservableProperty]
        private ProgressControlViewModel _progressControl;

        [ObservableProperty]
        private ForecastItemDto? _selectedTab;

        [ObservableProperty]
        private LocationDto? _selectedLocation;

        [ObservableProperty]
        private AwarnessItemDto _awarness;

        public Window? OwnerWindow { get; set; }

        public ObservableCollection<LocationDto> Locations { get; } = new();
        public ObservableCollection<ForecastItemDto> Forecasts { get; } = new();

        private static string Translation(string key) => LanguageManager.Instance[key];

        public static string WeatherForecastTitle => Translation("Weather_Title");
        public static string TemperatureLabel => Translation("Weather_Temperature");
        public static string MinimumLabel => Translation("Weather_Minimum");
        public static string MaximumLabel => Translation("Weather_Maximum");
        public static string WindLabel => Translation("Weather_Wind");
        public static string IntensityLabel => Translation("Weather_Intensity");
        public static string DirectionLabel => Translation("Weather_Direction");
        public static string PrecipitationLabel => Translation("Weather_Precipitation");
        public static string ProbabilityLabel => Translation("Weather_Probability");
        public static string WarningsLabel => Translation("Weather_Warnings");

        // =========================
        // RUNTIME CONSTRUCTOR
        // =========================
        public WeatherForecastPageViewModel(ProgressControlViewModel progressControl, IMessageService messageService, IIpmaService apiClient)
        {
            PageName = ApplicationPageNames.WeatherForecast;
            _progressControl = progressControl;
            _messageService = messageService;
            _apiClient = apiClient;

            LanguageManager.Instance.PropertyChanged += (_, __) =>
            {
                foreach (var f in Forecasts)
                {
                    f.OnLanguageChanged();
                    foreach (var a in f.AwarnessInformation)
                        a.OnLanguageChanged();
                }
            };
        }

        public async Task LoadAsync()
        {
            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                await Task.WhenAll(
                    LoadPrecipitationAsync(_cts.Token),
                    LoadWindAsync(_cts.Token),
                    LoadLocationsAsync(_cts.Token),
                    LoadWeatherTypesAsync(_cts.Token),
                    LoadAwarnessAsync(_cts.Token)
                );
            }
            catch (OperationCanceledException) { }
            catch (ApiException ex)
            {
                var exMsg = Translation("Weather_ErrorLoadingData");
                if (ex.InnerException?.InnerException != null)
                    exMsg = ex.InnerException.InnerException.Message;

                await _messageService.ShowAsync(string.Format(Translation("Weather_StartupError"), exMsg), MessageDialogType.Error);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(string.Format(Translation("Weather_StartupError"), ex.Message), MessageDialogType.Error);
            }
        }

        public Task UnloadAsync()
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
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
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                _ = LoadDataOrchestratorAsync(value, _cts.Token);
            }
        }

        private async Task LoadDataOrchestratorAsync(LocationDto value, CancellationToken ct)
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Title = Translation("Weather_Loading");
                ProgressControl.Message = string.Format(Translation("Weather_ForecastFor"), value.Name);

                await Task.Delay(10, ct);
                await LoadForecastAsync(value.GlobalIdLocal, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(ex.Message, MessageDialogType.Error);
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                    ProgressControl.IsVisible = false;
            }
        }

        #region API Methods

        private async Task LoadAwarnessAsync(CancellationToken ct = default)
        {
            AwarnessTypes.Clear();
            var response = await _apiClient.GetAwarnessAsync(ct);

            if (response == null || response.Count == 0) return;

            var mapped = response.Select(x => x.ToDto()).ToList();
            foreach (var item in mapped) AwarnessTypes.Add(item);
        }

        public static SolidColorBrush GetLevelBrush(string level)
        {
            return level?.ToLower() switch
            {
                "green" => new SolidColorBrush(Colors.Green),
                "yellow" => new SolidColorBrush(Colors.Yellow),
                "orange" => new SolidColorBrush(Colors.Orange),
                "red" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        private async Task LoadWindAsync(CancellationToken ct = default)
        {
            WindSpeeds.Clear();
            var response = await _apiClient.GetWindAsync(ct);

            if (response?.Data == null) return;

            var mapped = response.Data.Select(x => x.ToDto()).ToList();
            foreach (var item in mapped) WindSpeeds.Add(item);
        }

        private async Task LoadPrecipitationAsync(CancellationToken ct = default)
        {
            PrecepitationTypes.Clear();
            var response = await _apiClient.GetPrecipitationTypesAsync(ct);

            if (response?.Data == null) return;

            var mapped = response.Data.Select(x => x.ToDto()).ToList();
            foreach (var item in mapped) PrecepitationTypes.Add(item);
        }

        private async Task LoadLocationsAsync(CancellationToken ct = default)
        {
            Locations.Clear();
            var response = await _apiClient.GetLocationsAsync(ct);

            if (response?.Data == null) return;

            var mapped = response.Data.Select(x => x.ToDto()).OrderBy(x => x.Name).ToList();
            foreach (var item in mapped) Locations.Add(item);
        }

        private async Task LoadWeatherTypesAsync(CancellationToken ct = default)
        {
            WeatherTypes.Clear();
            var response = await _apiClient.GetWeatherTypesAsync(ct);

            if (response?.Data == null) return;

            var mapped = response.Data.Select(x => x.ToDto()).ToList();
            foreach (var item in mapped) WeatherTypes.Add(item);
        }

        private async Task LoadForecastAsync(int locationId, CancellationToken ct = default)
        {
            var response = await _apiClient.GetForecastByCityAsync(locationId, ct);

            if (response?.Data == null) return;

            var mapped = response.Data.Select(x => x.ToDto()).ToList();
            var tempList = new List<ForecastItemDto>();

            foreach (var item in mapped)
            {
                ct.ThrowIfCancellationRequested();

                item.WeatherInformation = WeatherTypes.FirstOrDefault(x => x.IdWeatherType == item.WeatherTypeId)
                    ?? new WeatherTypeDto { DescriptionPT = "Unknown" };

                item.WindInformation = WindSpeeds.FirstOrDefault(x => x.ClassWindSpeedValue == item.WindSpeedClass)
                    ?? new WindSpeedDto { DescriptionPT = "N/A" };

                item.PrecipitationInformation = PrecepitationTypes.FirstOrDefault(x => x.IntensityLevel == item.PrecipitationIntensityClass)
                    ?? new PrecipitationDto { DescriptionPT = "---", IntensityLevel = -99 };

                var dayStart = item.Date.Date;
                var dayEnd = item.Date.Date.AddDays(1).AddTicks(-1);

                item.AwarnessInformation = AwarnessTypes
                    .Where(a => SelectedLocation != null && a.StartTime <= dayEnd && a.EndTime >= dayStart && SelectedLocation.IdAreaAviso == a.Area)
                    .ToList();

                tempList.Add(item);
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested) return;

                Forecasts.Clear();
                foreach (var item in tempList)
                {
                    foreach (var a in item.AwarnessInformation)
                        a.LevelBrush = GetLevelBrush(a.Level);

                    Forecasts.Add(item);
                }
                SelectedTab = Forecasts.FirstOrDefault();
            });
        }
        #endregion
    }
}