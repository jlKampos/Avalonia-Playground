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
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using OmniWatch.Data;
using OmniWatch.Helpers;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Mapping.Noaa;
using OmniWatch.Models.Noaa;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class NoaaPageViewModel : PageViewModel, IAsyncPage
    {
        private readonly INoaaService _apiClient;
        private readonly ILogger<NoaaPageViewModel> _logger;
        private readonly IMessageService _messageService;

        // ==============================
        // SMotth animation state
        // ==============================
        private int _segmentIndex = 0;
        private double _t = 0;
        private DispatcherTimer? _animationTimer;
        private static MPoint Lerp(MPoint a, MPoint b, double t)
        {
            return new MPoint(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        private bool _hasInitialZoom = false;

        private List<StormTrackDto> _stormDtos = new List<StormTrackDto>();
        public List<int> Years { get; }

        [ObservableProperty]
        private int? _selectedYear;

        [ObservableProperty]
        private List<HurricaneOption> _hurricanes;

        [ObservableProperty]
        private HurricaneOption? _selectedHurricane;

        // ============================
        // Observable Properties
        // ============================

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl;

        [ObservableProperty]
        private bool _reanimate;

        partial void OnReanimateChanged(bool value)
        {
            // Se o utilizador ativar o toggle depois da animação acabar → reinicia
            if (value && _lastStorm != null)
            {
                StartCycloneAnimation(Map, _lastStorm, () => Reanimate);
            }
        }

        // ============================
        // Settings
        // ============================

        private TileLayer? _baseLayer;
        private TileLayer? _labelLayer;

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

        // ============================
        // Animation State
        // ============================

        private MemoryLayer _stormHeadLayer;
        private int _stormIndex = 0;
        private List<Coordinate> _stormPath;

        private int _currentRotation = 0;


        private StormTrackDto? _lastStorm;

        public NoaaPageViewModel(
            INoaaService noaaService,
            IMessageService messageService,
            ILogger<NoaaPageViewModel> logger,
            ProgressControlViewModel progressControl)
        {
            PageName = ApplicationPageNames.Noaa;
            _apiClient = noaaService;
            _logger = logger;
            _messageService = messageService;
            _progressControl = progressControl;

            int currentYear = DateTime.Now.Year;

            Years = Enumerable.Range(1980, currentYear - 1980 + 1).ToList();
            SelectedYear = currentYear;

            Map = new Mapsui.Map();
        }

        // ============================
        // Initialization
        // ============================

        public async Task LoadAsync()
        {
            try
            {
                await InitializeMapAsync();
                await LoadHistoricalStormsAsync();
            }
            catch (Exception ex)
            {
                var apiEx = ex.FindDeepestInner<ApiException>();
                var exMsg = apiEx?.ResponseContent ?? ex.GetBaseException().Message;

                await _messageService.ShowAsync(
                    $"Startup Error: {exMsg}",
                    MessageDialogType.Error);
            }
        }

        private async Task LoadHistoricalStormsAsync()
        {
            ProgressControl.IsVisible = true;
            ProgressControl.Title = "IBTrACS";
            ProgressControl.Message = "Loading historical storm tracks...";

            try
            {
                SelectedYear = SelectedYear ?? DateTime.Now.Year;

                var storms = await _apiClient.GetHistoricalStormTracksAsync(SelectedYear.Value);

                if (storms == null || storms.Count == 0)
                {
                    await _messageService.ShowAsync(
                        "No historical storm data found.",
                        MessageDialogType.Warning);
                    return;
                }

                _stormDtos = NoaaStormMapper.Map(storms);

                Hurricanes = _stormDtos.Select(s => new HurricaneOption { Name = s.Name, Id = s.Id }).OrderBy(h => h.Name).ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical Load Error");

                await _messageService.ShowAsync(
                    $"Historical Load Error: {ex.Message}",
                    MessageDialogType.Error);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        // ============================
        // Animation
        // ============================
        public void StartCycloneAnimation(Map map, StormTrackDto storm, Func<bool> reanimateProvider)
        {
            if (map == null || storm?.Track == null || storm.Track.Count < 2)
                return;

            _animationTimer?.Stop();
            _animationTimer = null;

            // Limpeza de camadas anteriores
            var layersToRemove = map.Layers
                .Where(l => l.Name == "Storm Track" ||
                            l.Name == "Storm Head" ||
                            l.Name == "Storm Trail")
                .ToList();

            foreach (var layer in layersToRemove)
                map.Layers.Remove(layer);

            var stormData = storm.Track
                .Select(p =>
                {
                    var (x, y) = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                    return new
                    {
                        X = x,
                        Y = y,
                        p.Wind,
                        p.Pressure,
                        p.Category,
                        p.Time,
                        p.Basin,
                        p.Nature
                    };
                })
                .ToList();

            _segmentIndex = 0;
            _t = 0;

            // Layer da linha completa (caminho planeado)
            var fullLine = new LineString(stormData.Select(p => new Coordinate(p.X, p.Y)).ToArray());
            var trackLayer = new MemoryLayer
            {
                Name = "Storm Track",
                Features = new List<IFeature> { new GeometryFeature { Geometry = fullLine } },
                Style = new VectorStyle { Line = new Pen(Color.FromArgb(180, 143, 170, 0), 4) }
            };

            // Layer do rastro (o que já foi percorrido)
            var trailLayer = new MemoryLayer
            {
                Name = "Storm Trail",
                Features = new List<IFeature>(),
                Style = new VectorStyle { Line = new Pen(Color.FromArgb(120, 143, 170, 0), 4) }
            };

            // Layer da "cabeça" (ícone + label)
            _stormHeadLayer = new MemoryLayer
            {
                Name = "Storm Head",
                Features = new List<IFeature>(),
                Style = null // Importante: Garante que a layer não aplique estilos padrão
            };

            map.Layers.Add(trackLayer);
            map.Layers.Add(trailLayer);
            map.Layers.Add(_stormHeadLayer);

            if (!_hasInitialZoom)
            {
                map.Navigator.ZoomToBox(trackLayer.Extent?.Grow(5));
                _hasInitialZoom = true;
            }

            var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "Noaa", "hurricane.svg");
            var hurricaneImage = new Mapsui.Styles.Image { Source = new Uri(imagePath).AbsoluteUri };

            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

            _animationTimer.Tick += (s, e) =>
            {
                if (_segmentIndex >= stormData.Count - 1)
                {
                    _animationTimer.Stop();
                    if (reanimateProvider()) StartCycloneAnimation(map, storm, reanimateProvider);
                    return;
                }

                var a = stormData[_segmentIndex];
                var b = stormData[_segmentIndex + 1];

                _t += 0.08;
                if (_t >= 1)
                {
                    _t = 0;
                    _segmentIndex++;
                    return;
                }

                var pos = Lerp(new MPoint(a.X, a.Y), new MPoint(b.X, b.Y), _t);

                // Cálculo de escala e rotação
                float wind = (float)(a.Wind + (b.Wind - a.Wind) * _t);
                float scale = Math.Clamp(0.5f + (wind / 150f), 0.5f, 1.8f);
                _currentRotation = (_currentRotation + 6) % 360;

                // --- ÚNICO FEATURE PARA ÍCONE E LABEL ---
                var stormFeature = new PointFeature(pos)
                {
                    Styles = new List<IStyle>()
                };

                // 1. Estilo da Imagem (Ícone Verde)
                // Ao definir ImageStyle, o Mapsui ignora o círculo branco padrão
                stormFeature.Styles.Add(new ImageStyle
                {
                    Image = hurricaneImage,
                    SymbolScale = scale,
                    SymbolRotation = _currentRotation
                });

                // 2. Estilo da Label (Texto à direita)
                var item = stormData[_segmentIndex];
                stormFeature.Styles.Add(new LabelStyle
                {
                    Text = $"{item.Time:yyyy-MM-dd HH:mm}\n" +
                           $"Wind: {item.Wind} kt\n" +
                           $"Pressure: {item.Pressure} hPa\n" +
                           $"Cat: {item.Category}\n" +
                           $"Basin: {item.Basin}\n" +
                           $"Nature: {item.Nature}",

                    BackColor = new Brush(Color.FromArgb(191, 143, 170, 0)),
                    BorderColor = Color.FromArgb(255, 60, 100, 0),
                    BorderThickness = 1,
                    ForeColor = Color.FromArgb(255, 25, 16, 0),
                    Font = new Font { Size = 12, Bold = true },

                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,

                    // Deslocamento em PIXELS: 35 para a direita, 0 para cima/baixo
                    Offset = new Offset(35, 0),
                    CollisionDetection = false
                });

                // Atualização do Rastro (Trail)
                var coords = stormData
                    .Take(_segmentIndex + 1)
                    .Select(p => new Coordinate(p.X, p.Y))
                    .ToList();
                coords.Add(new Coordinate(pos.X, pos.Y)); // Adiciona a posição atual suavemente

                if (coords.Count > 1)
                {
                    trailLayer.Features = new List<IFeature>
                    {
                        new GeometryFeature { Geometry = new LineString(coords.ToArray()) }
                    };
                }

                // Atualiza a cabeça da tempestade
                _stormHeadLayer.Features = new List<IFeature> { stormFeature };

                map.RefreshGraphics();
            };

            _animationTimer.Start();
        }

        partial void OnSelectedYearChanged(int? value)
        {
            if (value != null)
            {
                if (Hurricanes != null)
                {
                    Hurricanes.Clear();
                }
                _ = LoadHistoricalStormsAsync();
            }
        }

        partial void OnSelectedHurricaneChanged(HurricaneOption? value)
        {
            if (value == null)
                return;

            var hurricane = _stormDtos.FirstOrDefault(
                x => x.Id.Equals(value.Id, StringComparison.OrdinalIgnoreCase));

            if (hurricane == null)
                return;

            _lastStorm = hurricane;

            ZoomToStormArea(Map, hurricane);

            StartCycloneAnimation(Map, hurricane, () => Reanimate);
        }


        public Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        private void ZoomToStormArea(Map map, StormTrackDto storm)
        {
            var coords = storm.Track
                .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
                .ToList();

            if (coords.Count == 0)
                return;

            var minX = coords.Min(c => c.x);
            var maxX = coords.Max(c => c.x);
            var minY = coords.Min(c => c.y);
            var maxY = coords.Max(c => c.y);

            var box = new MRect(minX, minY, maxX, maxY);

            map.Navigator.ZoomToBox(box.Grow(1.2));
        }


        // ============================
        // Map Initialization
        // ============================

        private async Task InitializeMapAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Title = "Work in progress";
                ProgressControl.Message = "Out for some beer!";

                ApplyMapTheme();

                var worldExtent = new MRect(
                 -20037508, -20037508,
                  20037508, 20037508);

                Map.Navigator.ZoomToBox(worldExtent);

                Map.Navigator.OverridePanBounds = worldExtent;

                Map.RefreshGraphics();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync($"Map Error: {ex.Message}", MessageDialogType.Error);
                _logger.LogError(ex, "Map Error");
            }
        }


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
    }
}
