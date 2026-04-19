using AutoMapper;
using Avalonia.Controls;
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
using OmniWatch.Helpers;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Helpers;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
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

        private readonly IMapper _mapper;
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

        #region Map Layers

        private TileLayer? _baseLayer;
        private TileLayer? _labelLayer;

        #endregion

        #region Settings

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

        [ObservableProperty]
        private int _rateLimitRemaining;
        [ObservableProperty]
        private int _rateLimitTotal;
        [ObservableProperty]
        private DateTime _rateLimitResetAt;
        [ObservableProperty]
        private int _updateInterval;
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

        #region Observable Properties

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        #endregion

        #region Constructor

        public OpenSkyPageViewModel(
            ProgressControlViewModel progressControl,
            IOpenSkyService apiClient,
            IMessageService messageService,
            IMapper mapper,
            ISettingsService settingsService,
            IOpenSkyTokenManager tokenManager)
        {
            PageName = ApplicationPageNames.OpenSky;

            _settingsService = settingsService;
            _messageService = messageService;
            _tokenManager = tokenManager;
            _mapper = mapper;
            _apiClient = apiClient;

            Map = new Mapsui.Map();
        }

        public OpenSkyPageViewModel()
        {
            if (!Design.IsDesignMode)
                return;

            PageName = ApplicationPageNames.OpenSky;

            Map = new Mapsui.Map
            {
                CRS = "EPSG:3857"
            };

            Map.Layers.Add(new MemoryLayer
            {
                Name = "Preview",
                Features = new List<IFeature>()
            });

            var previewFeatures = new List<IFeature>
            {
                CreatePreviewLabel("TP001", 0, 0),
                CreatePreviewLabel("TP002", 0, 50),
                CreatePreviewLabel("TP003", 0, -50)
            };

            Map.Layers.Add(new MemoryLayer
            {
                Name = "PreviewPlanes",
                Features = previewFeatures
            });

            UseRealData = false;
        }

        private IFeature CreatePreviewLabel(string text, double x, double y)
        {
            var f = new PointFeature(new MPoint(x, y));
            f.Styles.Add(new LabelStyle
            {
                Text = text,
                ForeColor = Color.Black,
                BackColor = new Brush(Color.White),
                Font = new Font { Size = 14 }
            });
            return f;
        }

        #endregion

        #region Initialization

        public async Task LoadAsync()
        {
            try
            {
                await InitializeMapAsync();
                await ReloadAircraftAsync();
            }
            catch (Exception ex)
            {
                var apiEx = ex.FindDeepestInner<ApiException>();

                var exMsg = apiEx?.ResponseContent
                            ?? ex.GetBaseException().Message;

                await _messageService.ShowAsync(
                    $"Startup Error: {exMsg}",
                    MessageDialogType.Error);
            }
        }

        private async Task ReloadAircraftAsync()
        {
            _cts?.Cancel();

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

        #region Dummy Movement

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

                    var speedFactor = 0.05;
                    var angleRad = (plane.TrueTrack ?? 0) * Math.PI / 180.0;

                    plane.Latitude += Math.Cos(angleRad) * speedFactor;
                    plane.Longitude += Math.Sin(angleRad) * speedFactor;

                    plane.TrueTrack += random.Next(-5, 5);

                    if (plane.TrueTrack < 0) plane.TrueTrack += 360;
                    if (plane.TrueTrack > 360) plane.TrueTrack -= 360;
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddAircraftLayerToMap(aircraft);
                });

                await Task.Delay(1000, token);
            }
        }

        private List<StateVectorDto> GenerateDummyFlights()
        {
            var random = new Random();
            var list = new List<StateVectorDto>();

            for (int i = 0; i < 20; i++)
            {
                list.Add(new StateVectorDto
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
                });
            }

            return list;
        }

        #endregion

        #region Map Setup

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

        #endregion

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

        #region Aircraft Rendering

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

        #region Data Loading

        public async Task LoadAllFlightStatesAsync()
        {
            var settings = _settingsService.Load();
            UpdateInterval = settings.RefreshInterval;

            ShowRateLimitOverlay = settings.UseOpenSkyCredentials;
            ShowRateLimitNonAuthUser = UseRealData && !settings.UseOpenSkyCredentials && settings.RefreshInterval <= 10;

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

            if (raw?.States != null)
            {
                _flightStates = raw.States
                 .Select(OpenSkyRawConverter.ConvertRaw)
                 .Select(x => _mapper.Map<StateVectorDto>(x))
                 .ToList();

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
                try
                {
                    var settings = _settingsService.Load();
                    var delaySeconds = settings.RefreshInterval;

                    var (raw, rate) = await _apiClient.GetAllFlightStatesAsync();

                    if (rate != null)
                    {
                        RateLimitRemaining = rate.Remaining;
                        RateLimitTotal = rate.Limit;
                        RateLimitResetAt = rate.ResetAt;
                    }

                    if (rate?.Remaining == 0)
                    {
                        var waitSeconds = (int)(rate.ResetAt - DateTime.UtcNow).TotalSeconds;
                        if (waitSeconds < 1)
                            waitSeconds = 1;

                        await _messageService.ShowAsync(
                            $"OpenSky API limit reached.\nNext reset at {rate.ResetAt:HH:mm:ss} UTC.",
                            MessageDialogType.Warning);

                        await Task.Delay(waitSeconds * 1000, token);
                        continue;
                    }

                    if (raw?.States != null)
                    {
                        var mapped = raw.States
                            .Select(OpenSkyRawConverter.ConvertRaw)
                            .ToList();

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var mapped = raw.States
                            .Select(OpenSkyRawConverter.ConvertRaw)
                            .Select(x => _mapper.Map<StateVectorDto>(x))
                            .ToList();

                            AddAircraftLayerToMap(mapped);

                        });
                    }
                }
                catch
                {
                }

                try
                {
                    var settings = _settingsService.Load();
                    await Task.Delay(settings.RefreshInterval * 1000, token);
                }
                catch
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        #endregion
    }
}
