using Avalonia.Threading;
using BruTile.Predefined;
using BruTile.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using OmniWatch.Core.Interfaces;
using OmniWatch.Data;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Mapping.OpenSky;
using OmniWatch.Models.OpenSky;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class OpenSkyPageViewModel : PageViewModel, IAsyncPage
    {
        #region Dependencies

        private readonly IOpenSkyService _apiClient;
        private readonly IMessageService _messageService;
        private readonly ISettingsService _settingsService;
        private readonly IOpenSkyTokenManager _tokenManager;

        #endregion

        #region State

        private List<StateVectorDto> _flightStates = new();
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private bool _useRealData = true;

        [ObservableProperty]
        public bool _showRateLimitNonAuthUser = false;

        [ObservableProperty]
        public bool _showRateLimitOverlay = false;

        #endregion

        #region Map

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

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
                    $"Loading error: {ex.Message}",
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
            IOpenSkyTokenManager tokenManager)
        {
            PageName = ApplicationPageNames.OpenSky;

            _settingsService = settingsService;
            _messageService = messageService;
            _tokenManager = tokenManager;
            _apiClient = apiClient;

            Map = new Mapsui.Map();
        }

        #endregion

        #region Load

        public async Task LoadAsync()
        {
            try
            {
                await InitializeMapAsync();
                await ReloadAircraftAsync();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Startup Error: {ex.Message}",
                    MessageDialogType.Error);
            }
        }

        public Task UnloadAsync()
        {
            // Ensures all running loops are stopped when navigating away
            _cts?.Cancel();
            _cts = null;
            return Task.CompletedTask;
        }

        private async Task ReloadAircraftAsync()
        {
            // Cancel any previous loop
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
            var settings = _settingsService.Load();
            UpdateInterval = settings.RefreshInterval;

            ShowRateLimitOverlay = settings.UseOpenSkyCredentials;
            ShowRateLimitNonAuthUser =
                UseRealData &&
                !settings.UseOpenSkyCredentials &&
                settings.RefreshInterval <= 10;

            try
            {
                if (settings.UseOpenSkyCredentials)
                {
                    var auth = await _tokenManager.RefreshTokenAsync();

                    if (auth.Status == OpenSkyAuthStatus.Unauthorized)
                        await _messageService.ShowAsync("Invalid OpenSky credentials.", MessageDialogType.Warning);

                    if (auth.Status == OpenSkyAuthStatus.Error)
                        await _messageService.ShowAsync("OpenSky server error.", MessageDialogType.Error);
                }

                var (raw, rate) = await _apiClient.GetAllFlightStatesAsync();

                if (rate != null)
                {
                    RateLimitRemaining = rate.Remaining;
                    RateLimitTotal = rate.Limit;
                    RateLimitResetAt = rate.ResetAt;
                }

                if (raw?.States == null)
                    return;

                _flightStates = raw.States
                    .Select(x => x.ToDto())
                    .ToList();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Failed to load flight states: {ex.Message}",
                    MessageDialogType.Error);
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
                            $"OpenSky API limit reached.\nNext reset at {rate.ResetAt:HH:mm:ss} UTC.",
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
                    // ignore or log
                }

                // Compensate drift
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
            ApplyMapTheme();

            var center = new MPoint(-770000, 4780000);
            Map.Navigator.CenterOnAndZoomTo(center, Map.Navigator.Resolutions[7]);

            var extent = new MRect(-1500000, 4200000, -300000, 5400000);
            Map.Navigator.OverridePanBounds = extent;

            Map.RefreshGraphics();

            await Task.CompletedTask;
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
            bool usingCircles = false;
            var features = new List<IFeature>();

            foreach (var plane in aircraft)
            {
                if (plane.Latitude == null || plane.Longitude == null)
                    continue;

                if (plane.OnGround == true)
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(
                    plane.Longitude.Value,
                    plane.Latitude.Value);

                var point = new MPoint(x, y);

                var callsign = string.IsNullOrWhiteSpace(plane.Callsign)
                    ? plane.Icao24?.ToUpper()
                    : plane.Callsign.Trim();

                var altitude = plane.Altitude?.ToString("F0") ?? "N/A";
                var velocity = plane.Velocity?.ToString("F0") ?? "N/A";
                var origin = plane.OriginCountry ?? "Unknown";
                var onGround = plane.OnGround == true ? "Yes" : "No";

                var infoText =
                 $"CallSign: {callsign}\n" +
                 $"Origin: {origin}\n" +
                 $"Alt: {altitude}\n" +
                 $"Vel: {velocity}\n" +
                 $"OnGround: {onGround}";

                var color = plane.Altitude switch
                {
                    < 2000 => Color.DodgerBlue,
                    < 8000 => Color.Gold,
                    _ => Color.IndianRed
                };

                var aircraftFeature = new PointFeature(point);
                aircraftFeature.Styles.Clear();

                var imagePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "Images",
                    "OpenSky",
                    "airplane.svg"
                );

                var imageUri = new Uri(imagePath).AbsoluteUri;

                if (plane.TrueTrack != null)
                {
                    aircraftFeature.Styles.Add(new ImageStyle
                    {
                        Image = new Mapsui.Styles.Image
                        {
                            Source = imageUri
                        },
                        SymbolScale = 0.6,
                        SymbolRotation = plane.TrueTrack ?? 0,
                        Offset = new Offset(0, 0)
                    });
                }
                else
                {
                    usingCircles = true;
                    aircraftFeature.Styles.Add(new SymbolStyle
                    {
                        SymbolType = SymbolType.Ellipse,
                        SymbolScale = 0.40f,
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 3)
                    });
                }

                features.Add(aircraftFeature);

                if (plane.TrueTrack != null && usingCircles)
                {
                    var angle = Math.PI * plane.TrueTrack.Value / 180.0;
                    var length = 15000.0;
                    var offset = 3000.0;

                    var startX = x + Math.Sin(angle) * offset;
                    var startY = y + Math.Cos(angle) * offset;

                    var line = new LineString(new[]
                    {
                        new Coordinate(startX, startY),
                        new Coordinate(
                            x + Math.Sin(angle) * length,
                            y + Math.Cos(angle) * length)
                    });

                    var lineFeature = new GeometryFeature
                    {
                        Geometry = line
                    };

                    lineFeature.Styles.Add(new VectorStyle
                    {
                        Line = new Pen(new Color(color.R, color.G, color.B, 255), 1f)
                    });

                    features.Add(lineFeature);
                }

                var label = new PointFeature(point);

                label.Styles.Add(new LabelStyle
                {
                    Text = infoText,

                    BorderThickness = 1,
                    BorderColor = Color.FromArgb(255, 60, 100, 0),

                    ForeColor = Color.FromArgb(255, 25, 16, 0),
                    BackColor = new Brush(Color.FromArgb(191, 143, 170, 0)),

                    Font = new Font
                    {
                        Size = 11,
                        Bold = true
                    },

                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,

                    Offset = new Offset(20, 0),
                    CollisionDetection = true
                });

                features.Add(label);
            }

            var oldLayer = Map.Layers.FirstOrDefault(l => l.Name == "Aircraft");
            if (oldLayer != null)
                Map.Layers.Remove(oldLayer);

            Map.Layers.Add(new MemoryLayer
            {
                Name = "Aircraft",
                Features = features,
                Style = null
            });

            Map.RefreshGraphics();
        }

        #endregion
    }
}
