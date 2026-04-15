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
using MyAvalonia.Data;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Interfaces;
using MyAvalonia.Models.OpenSky;
using MyAvalonia.ViewModels.ProgressControl;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MyAvalonia.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace MyAvalonia.ViewModels
{
    public partial class OepnSkyPageViewModel : PageViewModel
    {
        #region Dependencies

        private readonly IMapper _mapper;
        private readonly IOpenSkyService _apiClient;
        private readonly IMessageService _messageService;

        #endregion

        #region State

        private OpenSkyDto FlightStates { get; set; } = new();
        private CancellationTokenSource? _cts;

        #endregion

        #region Map Layers

        private TileLayer? _baseLayer;
        private TileLayer? _labelLayer;
        private MemoryLayer? _aircraftLayer;

        private int? _aircraftBitmapId;
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

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        #endregion

        #region Constructor

        public OepnSkyPageViewModel(
            ProgressControlViewModel progressControl,
            IOpenSkyService apiClient,
            IMessageService messageService,
            IMapper mapper)
        {
            PageName = ApplicationPageNames.OepnSky;

            _messageService = messageService;
            _mapper = mapper;
            _apiClient = apiClient;

            Map = new Mapsui.Map();
            _ = InitializeAsync();
        }

        public OepnSkyPageViewModel()
        {
            if (Design.IsDesignMode)
            {
                Map = new Mapsui.Map();
                PageName = ApplicationPageNames.OepnSky;
            }
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Title = "Loading";
                ProgressControl.Message = "Loading flight data...";

                await InitializeMapAsync();

                await LoadAllFlightStatesAsync();

                _ = StartAutoRefreshAsync();

                //// Testes
                //var dummy = GenerateDummyFlights();
                //AddAircraftLayerToMap(dummy);
                //_ = StartDummyMovementAsync(dummy);
                //// end Testes
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync($"Startup Error: {ex.Message}", MessageDialogType.Error);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

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

                    // mudar ligeiramente direção
                    plane.TrueTrack += random.Next(-5, 5);

                    if (plane.TrueTrack < 0) plane.TrueTrack += 360;
                    if (plane.TrueTrack > 360) plane.TrueTrack -= 360;
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AddAircraftLayerToMap(aircraft);
                });

                await Task.Delay(1000, token); // suave 🔥
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
                    Latitude = 36 + random.NextDouble() * 6,   // Portugal-ish
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

                // =========================
                // AIRCRAFT
                // =========================
                var aircraftFeature = new PointFeature(point);
                aircraftFeature.Styles.Clear();

                if (plane.TrueTrack != null)
                {

                    aircraftFeature.Styles.Add(new SymbolStyle
                    {
                        SymbolType = SymbolType.Triangle,
                        SymbolRotation = (float)plane.TrueTrack.Value,
                        SymbolScale = 0.55f,
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 3)
                    });
                }
                else
                {

                    aircraftFeature.Styles.Add(new SymbolStyle
                    {
                        SymbolType = SymbolType.Ellipse,
                        SymbolScale = 0.40f,
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 3)
                    });
                }

                features.Add(aircraftFeature);

                // =========================
                // DIREÇÃO
                // =========================
                if (plane.TrueTrack != null)
                {
                    var angle = Math.PI * plane.TrueTrack.Value / 180.0;
                    var length = 25000.0;

                    var line = new LineString(new[]
                    {
                        new Coordinate(x, y),
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
                        Line = new Pen(new Color(color.R, color.G, color.B, 140), 2)
                    });

                    features.Add(lineFeature);
                }

                // =========================
                // LABEL
                // =========================
                var label = new PointFeature(point);

                label.Styles.Add(new LabelStyle
                {
                    Text = infoText,
                    ForeColor = Color.Black,
                    BackColor = new Brush(Color.FromArgb(220, 255, 255, 255)),
                    Font = new Font { Size = 11 },

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
            FlightStates = new OpenSkyDto();

            var response = await _apiClient.GetAllFlightStatesAsync();

            if (response != null)
                FlightStates = _mapper.Map<OpenSkyDto>(response);
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
                    var response = await _apiClient.GetAllFlightStatesAsync();

                    if (response?.States != null)
                    {
                        var data = response.States
                            .Select(x => _mapper.Map<StateVectorDto>(x))
                            .ToList();

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            AddAircraftLayerToMap(data);
                        });
                    }
                }
                catch
                {
                    // opcional: log
                }

                try
                {
                    // IMPORTANTE: >= 10 to avoid 429
                    await Task.Delay(10000, token);
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