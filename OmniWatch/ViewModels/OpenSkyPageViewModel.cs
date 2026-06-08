using Avalonia.Threading;
using BruTile.Predefined;
using BruTile.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using OmniWatch.Core.Interfaces;
using OmniWatch.Data;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Localization;
using OmniWatch.Mapping.Noaa;
using OmniWatch.Mapping.OpenSky;
using OmniWatch.Models.Noaa.ActiveStorms;
using OmniWatch.Models.OpenSky;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class OpenSkyPageViewModel : PageViewModel, IAsyncPage
    {
        #region Dependencies
        private readonly INoaaService _noaaApiClient;
        private readonly IOpenSkyService _apiClient;
        private readonly IMessageService _messageService;
        private readonly ISettingsService _settingsService;
        private readonly IOpenSkyTokenManager _tokenManager;

        #endregion

        List<ActiveStormDto> ActiveStorms = new List<ActiveStormDto>();

        #region Localization Helper

        private string Translation(string key) =>
            LanguageManager.Instance[key];

        #endregion

        #region State
        private DispatcherTimer? _debounceTimer;
        private List<StateVectorDto> _flightStates = new();
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private bool _useRealData = true;

        [ObservableProperty]
        public bool _showRateLimitNonAuthUser = false;

        [ObservableProperty]
        public bool _showRateLimitOverlay = false;

        [ObservableProperty]
        public string _activeStormsMessage = "";

        [ObservableProperty]
        public bool _anyActiveStorms = false;

        private float _activeRotation = 0;
        private DispatcherTimer? _activeStormsRotationTimer;

        private int _currentStormIndex = -1;

        public ICommand CycleStormCommand { get; }

        public event EventHandler<ActiveStormDto> StormSelected;

        #endregion

        #region Map
        private MemoryLayer? _aircraftLayer;
        private static readonly string PlaneImgPath = new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "OpenSky", "airplane.svg")).AbsoluteUri;
        private static readonly Image PlaneImageSource = new Image { Source = PlaneImgPath };

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl;

        #endregion

        #region Rate Limit

        [ObservableProperty] private int _rateLimitRemaining;
        [ObservableProperty] private int _rateLimitTotal;
        [ObservableProperty] private DateTime _rateLimitResetAt;
        [ObservableProperty] private int _updateInterval;

        #endregion

        #region Map Layers

        private TileLayer? _baseLayer;
        private TileLayer? _labelLayer;

        #endregion

        #region Map theme
        private bool _isDarkTheme = true;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                    ApplyMapTheme();
            }
        }
        #endregion

        #region Settings
        partial void OnUseRealDataChanged(bool value)
        {
            _ = HandleUseRealDataChangedAsync(value);
        }

        private async Task HandleUseRealDataChangedAsync(bool value)
        {
            try
            {
                var settings = _settingsService.Load();
                ShowRateLimitNonAuthUser = value && !settings.UseOpenSkyCredentials && settings.RefreshInterval <= 10;
                ShowRateLimitOverlay = value;

                await ReloadAircraftAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("OpenSky_LoadError"), ex.Message),
                    MessageDialogType.Error);
            }
        }
        #endregion

        #region Constructor

        public OpenSkyPageViewModel(
            ProgressControlViewModel progressControl,
            IOpenSkyService apiClient,
            IMessageService messageService,
            ISettingsService settingsService,
            IOpenSkyTokenManager tokenManager,
            INoaaService noaaApiClient)
        {
            PageName = ApplicationPageNames.OpenSky;
            _progressControl = progressControl;
            _settingsService = settingsService;
            _messageService = messageService;
            _tokenManager = tokenManager;
            _apiClient = apiClient;
            _noaaApiClient = noaaApiClient;

            Map = new Mapsui.Map();

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimer.Tick += async (s, e) =>
            {
                _debounceTimer.Stop();
                await LoadAllFlightStatesAsync();
            };

            Map.Navigator.ViewportChanged += (s, e) =>
            {
                if (UseRealData)
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            };

            CycleStormCommand = new RelayCommand(CycleStorm);
        }

        #endregion

        #region Load

        public async Task LoadAsync()
        {
            try
            {
                await InitializeMapAsync().ConfigureAwait(false);
                await ReloadAircraftAsync().ConfigureAwait(false);
                await CheckActiveStormsAsync().ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("OpenSky_StartupError"), ex.Message),
                    MessageDialogType.Error);
            }
        }

        public Task UnloadAsync()
        {
            _cts?.Cancel();
            _cts = null;
            return Task.CompletedTask;
        }

        private async Task ReloadAircraftAsync()
        {
            _cts?.Cancel();
            _cts = null;

            if (!UseRealData)
            {
                var dummy = GenerateDummyFlights();
                AddAircraftLayerToMap(dummy);
                _ = StartDummyMovementAsync(dummy);
            }
            else
            {
                await LoadAllFlightStatesAsync();
                AddAircraftLayerToMap(_flightStates);
                _ = StartAutoRefreshAsync();
            }
        }

        #endregion

        #region Load Data

        public async Task LoadAllFlightStatesAsync()
        {
            if (Map?.Navigator == null) return;

            var settings = _settingsService.Load();
            UpdateInterval = settings.RefreshInterval;

            ShowRateLimitOverlay = settings.UseOpenSkyCredentials;
            ShowRateLimitNonAuthUser =
                UseRealData &&
                !settings.UseOpenSkyCredentials &&
                settings.RefreshInterval <= 10;

            try
            {
                var extent = Map.Navigator.Viewport.ToExtent();
                if (extent == null) return;
                var bottomLeft = SphericalMercator.ToLonLat(extent.MinX, extent.MinY);
                var topRight = SphericalMercator.ToLonLat(extent.MaxX, extent.MaxY);

                var (raw, rate) = await _apiClient.GetFlightStatesInViewportAsync(
                    bottomLeft.lat, bottomLeft.lon,
                    topRight.lat, topRight.lon);

                if (rate != null)
                {
                    RateLimitRemaining = rate.Remaining;
                    RateLimitTotal = rate.Limit;
                    RateLimitResetAt = rate.ResetAt;
                }

                if (raw?.States != null)
                {
                    _flightStates = raw.States.Select(x => x.ToDto()).ToList();
                    AddAircraftLayerToMap(_flightStates);
                }
                else
                {
                    AddAircraftLayerToMap(new List<StateVectorDto>());
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("OpenSky_FailedLoadStates"), ex.Message),
                    MessageDialogType.Error);
            }
        }

        private async Task CheckActiveStormsAsync()
        {
            ProgressControl.IsVisible = true;
            ProgressControl.Title = Translation("Noaa_Loading");
            ProgressControl.Message = Translation("Noaa_CheckingActiveStorms");

            try
            {
                var result = await _noaaApiClient.GetActiveStormTracksAsync();

                if (result != null)
                {
                    ActiveStorms = ActiveStormMapper.Map(result.ActiveStorms);
                    AnyActiveStorms = ActiveStorms.Any();
                    ActiveStormsMessage = AnyActiveStorms
                        ? string.Format(Translation("Noaa_ActiveStorms"), ActiveStorms.Count)
                        : "";

                    if (AnyActiveStorms)
                    {
                        ShowCurrentActiveStormsOnMap(Map, ActiveStorms);
                    }
                }
            }
            catch (Exception ex)
            {

                await _messageService.ShowAsync(
                    string.Format(Translation("Noaa_Error"), ex.Message),
                    MessageDialogType.Error);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        #endregion

        #region Auto Refresh

        private async Task StartAutoRefreshAsync()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                var start = DateTime.UtcNow;

                try
                {
                    var settings = _settingsService.Load();
                    var (raw, rate) = await _apiClient.GetAllFlightStatesAsync();

                    if (rate != null)
                    {
                        RateLimitRemaining = rate.Remaining;
                        RateLimitTotal = rate.Limit;
                        RateLimitResetAt = rate.ResetAt;
                    }

                    if (rate?.Remaining == 0)
                    {
                        var waitSeconds = (int)Math.Max((rate.ResetAt - DateTime.UtcNow).TotalSeconds, 1);

                        await _messageService.ShowAsync(
                            string.Format(Translation("OpenSky_RateLimitReached"), rate.ResetAt.ToString("HH:mm:ss")),
                            MessageDialogType.Warning);

                        await Task.Delay(waitSeconds * 1000, token);
                        continue;
                    }

                    if (raw?.States != null)
                    {
                        var mapped = raw.States
                            .Select(x => x.ToDto())
                            .ToList();

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            AddAircraftLayerToMap(mapped);
                        });
                    }
                }
                catch
                {
                    // ignore
                }

                var elapsed = DateTime.UtcNow - start;
                var settings2 = _settingsService.Load();
                var delay = TimeSpan.FromSeconds(settings2.RefreshInterval) - elapsed;

                if (delay > TimeSpan.Zero)
                {
                    try { await Task.Delay(delay, token); }
                    catch { break; }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        #endregion

        #region Dummy

        private async Task StartDummyMovementAsync(List<StateVectorDto> aircraft)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var random = new Random();

            while (!token.IsCancellationRequested)
            {
                foreach (var plane in aircraft)
                {
                    if (plane.Latitude == null || plane.Longitude == null)
                        continue;

                    var speed = 0.05;
                    var angle = (plane.TrueTrack ?? 0) * Math.PI / 180.0;

                    plane.Latitude += Math.Cos(angle) * speed;
                    plane.Longitude += Math.Sin(angle) * speed;

                    plane.TrueTrack += random.Next(-5, 5);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddAircraftLayerToMap(aircraft);
                });

                try { await Task.Delay(1000, token); }
                catch { break; }
            }
        }

        private List<StateVectorDto> GenerateDummyFlights()
        {
            var random = new Random();

            return Enumerable.Range(0, 20)
                .Select(i => new StateVectorDto
                {
                    Icao24 = $"DUMMY{i}",
                    Callsign = $"TP{i:000}",
                    Latitude = 36 + random.NextDouble() * 6,
                    Longitude = -10 + random.NextDouble() * 6,
                    Altitude = random.Next(1000, 12000),
                    Velocity = random.Next(100, 250),
                    TrueTrack = random.Next(0, 360),
                    OnGround = false,
                    OriginCountry = "Portugal"
                })
                .ToList();
        }

        #endregion

        #region Map

        private async Task InitializeMapAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Title = Translation("OpenSky_InitMap");
                ProgressControl.Message = Translation("OpenSky_SetupLayers");

                ApplyMapTheme();

                Map.Navigator.OverridePanBounds = null;
                Map.Navigator.OverrideZoomBounds = new MMinMax(0.5, 3500);

                var initialExtent = new MRect(-1100000, 4400000, -700000, 5200000);
                Map.Navigator.ZoomToBox(initialExtent);

                Map.RefreshGraphics();

                await Task.CompletedTask;

            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }


        #region Theme

        private void ApplyMapTheme()
        {
            if (_baseLayer != null) Map.Layers.Remove(_baseLayer);
            if (_labelLayer != null) Map.Layers.Remove(_labelLayer);

            if (_isDarkTheme)
            {
                _baseLayer = new TileLayer(new HttpTileSource(
                    new GlobalSphericalMercator(),
                    "https://basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png"));

                _labelLayer = new TileLayer(new HttpTileSource(
                    new GlobalSphericalMercator(),
                    "https://basemaps.cartocdn.com/dark_only_labels/{z}/{x}/{y}.png"));
            }
            else
            {
                _baseLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
                _labelLayer = null;
            }

            Map.Layers.Insert(0, _baseLayer);

            if (_labelLayer != null)
                Map.Layers.Insert(1, _labelLayer);

            Map.RefreshGraphics();
        }

        #endregion

        #endregion

        #region Rendering

        private void AddAircraftLayerToMap(List<StateVectorDto> aircraft)
        {
            if (Map?.Navigator?.Viewport == null) return;

            var features = new List<IFeature>();

            bool showLabels = Map.Navigator.Viewport.Resolution < 3000;

            foreach (var plane in aircraft)
            {
                if (plane.Latitude == null || plane.Longitude == null || plane.OnGround == true)
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(plane.Longitude.Value, plane.Latitude.Value);
                var point = new MPoint(x, y);

                var color = plane.Altitude switch { < 3000 => Color.DodgerBlue, < 8000 => Color.Gold, _ => Color.IndianRed };
                var aircraftFeature = new PointFeature(point);

                if (plane.TrueTrack != null)
                {
                    aircraftFeature.Styles.Add(new ImageStyle
                    {
                        Image = PlaneImageSource,
                        SymbolScale = 0.6,
                        SymbolRotation = plane.TrueTrack.Value
                    });
                }
                else
                {
                    aircraftFeature.Styles.Add(new SymbolStyle
                    {
                        SymbolType = SymbolType.Ellipse,
                        SymbolScale = 0.40f,
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 2)
                    });
                }
                features.Add(aircraftFeature);

                if (showLabels)
                {
                    var callsign = string.IsNullOrWhiteSpace(plane.Callsign) ? plane.Icao24?.ToUpper() : plane.Callsign.Trim();
                    var infoText = $"CallSign: {callsign}\n" +
                        $"Alt: {plane.Altitude?.ToString("F0") ?? "N/A"}m\n" +
                        $"Vel: {plane.Velocity?.ToString("F0") ?? "N/A"}km/h";

                    var labelFeature = new PointFeature(point);
                    labelFeature.Styles.Add(new LabelStyle
                    {
                        Text = infoText,
                        BorderThickness = 1,
                        BorderColor = Color.FromArgb(255, 60, 100, 0),
                        ForeColor = Color.FromArgb(255, 25, 16, 0),
                        BackColor = new Brush(Color.FromArgb(191, 143, 170, 0)),
                        Font = new Font { Size = 10, Bold = true },
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                        Offset = new Offset(20, 0),
                        CollisionDetection = true
                    });
                    features.Add(labelFeature);
                }
            }

            var aircraftLayer = Map.Layers.FirstOrDefault(l => l.Name == "Aircraft") as MemoryLayer;

            if (aircraftLayer == null)
            {
                aircraftLayer = new MemoryLayer
                {
                    Name = "Aircraft",
                    Style = null
                };
                Map.Layers.Add(aircraftLayer);
            }

            aircraftLayer.Features = features;

            Map.RefreshGraphics();
        }

        private void ShowCurrentActiveStormsOnMap(Map map, List<ActiveStormDto> activeStorms)
        {
            if (map == null || activeStorms == null || !activeStorms.Any()) return;

            var activeLayer = new MemoryLayer
            {
                Name = "Active Storms Layer",
                Features = new List<IFeature>()
            };

            var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "Noaa", "activeHurricane.svg");
            var uriPath = new Uri(imagePath).AbsoluteUri;
            var hurricaneImage = new Mapsui.Styles.Image { Source = uriPath };

            var features = new List<IFeature>();

            foreach (var storm in activeStorms)
            {
                var (x, y) = SphericalMercator.FromLonLat(storm.Longitude, storm.Latitude);
                var stormFeature = new PointFeature(new MPoint(x, y));

                float scale = Math.Clamp(0.5f + (storm.Intensity / 120f), 0.6f, 1.5f);

                var imageStyle = new ImageStyle
                {
                    Image = hurricaneImage,
                    SymbolScale = scale,
                    SymbolRotation = _activeRotation
                };

                var labelStyle = new LabelStyle
                {
                    Text = $"{storm.Name} ({storm.Classification})\n" +
                           $"Wind: {storm.WindSpeedKM:F1} km/h - {storm.Intensity} kt\n" +
                           $"Pres: {storm.Pressure} hPa\n" +
                           $"Mov: {storm.Movement}",

                    BackColor = new Brush(Color.FromArgb(191, 255, 165, 0)),
                    BorderColor = Color.FromArgb(255, 255, 140, 0),
                    BorderThickness = 1,
                    ForeColor = Color.FromArgb(255, 25, 16, 0),
                    Font = new Font { Size = 11, Bold = true },
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset(30, 0),
                    CollisionDetection = false
                };

                stormFeature.Styles.Add(imageStyle);
                stormFeature.Styles.Add(labelStyle);
                features.Add(stormFeature);
            }

            activeLayer.Features = features;
            map.Layers.Add(activeLayer);

            _activeStormsRotationTimer?.Stop();
            _activeStormsRotationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _activeStormsRotationTimer.Tick += (s, e) =>
            {
                _activeRotation = (_activeRotation - 10) % 360;

                var layer = map.Layers.FirstOrDefault(l => l.Name == "Active Storms Layer") as MemoryLayer;
                if (layer != null)
                {
                    foreach (var feature in layer.Features.Cast<PointFeature>())
                    {
                        var style = feature.Styles.OfType<ImageStyle>().FirstOrDefault();
                        if (style != null) style.SymbolRotation = _activeRotation;
                    }
                    map.RefreshGraphics();
                }
            };

            _activeStormsRotationTimer.Start();
        }

        private void CycleStorm()
        {
            if (ActiveStorms == null || ActiveStorms.Count == 0)
                return;

            _currentStormIndex = (_currentStormIndex + 1) % ActiveStorms.Count;

            var storm = ActiveStorms[_currentStormIndex];

            // Evento para a View
            StormSelected?.Invoke(this, storm);
        }

        #endregion
    }
}
