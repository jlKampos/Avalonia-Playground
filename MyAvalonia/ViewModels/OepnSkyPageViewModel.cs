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
using MyAvalonia.Integrations.Exceptions;
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

                var color = plane.Altitude switch
                {
                    < 2000 => Color.Blue,
                    < 8000 => Color.Gold,
                    _ => Color.Red
                };

                var infoText =
                    $"{callsign}\nAlt: {altitude}\nVel: {velocity}";

                if (plane.TrueTrack != null)
                {
                    var size = 25000.0;
                    var angle = Math.PI * plane.TrueTrack.Value / 180.0;

                    var front = new Coordinate(
                        x + Math.Sin(angle) * size,
                        y + Math.Cos(angle) * size);

                    var left = new Coordinate(
                        x + Math.Sin(angle + Math.PI - 0.3) * (size * 0.6),
                        y + Math.Cos(angle + Math.PI - 0.3) * (size * 0.6));

                    var right = new Coordinate(
                        x + Math.Sin(angle + Math.PI + 0.3) * (size * 0.6),
                        y + Math.Cos(angle + Math.PI + 0.3) * (size * 0.6));

                    var geometry = new Polygon(new LinearRing(new[]
                    {
                        front, left, right, front
                    }));

                    var feature = new GeometryFeature { Geometry = geometry };

                    feature.Styles.Add(new VectorStyle
                    {
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 1)
                    });

                    features.Add(feature);
                }
                else
                {
                    var feature = new PointFeature(point);

                    feature.Styles.Add(new SymbolStyle
                    {
                        Fill = new Brush(color),
                        Outline = new Pen(Color.White, 2),
                        SymbolScale = 0.7f
                    });

                    features.Add(feature);
                }

                var label = new PointFeature(point);

                label.Styles.Add(new LabelStyle
                {
                    Text = infoText,
                    ForeColor = Color.Black,
                    BackColor = new Brush(Color.FromArgb(200, 255, 255, 255)),
                    Font = new Font { Size = 11 },
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset(20, 0),
                    CollisionDetection = true
                });

                features.Add(label);
            }

            if (_aircraftLayer == null)
            {
                _aircraftLayer = new MemoryLayer
                {
                    Name = "Aircraft",
                    Features = features
                };

                Map.Layers.Add(_aircraftLayer);
            }
            else
            {
                _aircraftLayer.Features = features;
            }

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

                    if (response?.States == null)
                        continue;

                    var data = response.States?.Select(x => _mapper.Map<StateVectorDto>(x)).ToList() ?? new List<StateVectorDto>();

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AddAircraftLayerToMap(data);
                    });
                }
                catch
                {
                    // opcional: log
                }

                try
                {
                    await Task.Delay(5000, token);
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