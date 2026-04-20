using AutoMapper;
using BruTile.Predefined;
using BruTile.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Tiling.Layers;
using OmniWatch.Data;
using OmniWatch.Helpers;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels
{
    public partial class NoaaPageViewModel : PageViewModel, IAsyncPage
    {
        private readonly IMapper _mapper;
        private readonly INoaaService _apiClient;
        private readonly IMessageService _messageService;

        #region Observable Properties

        [ObservableProperty]
        private Map _map;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        #endregion

        #region Settings

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
        #endregion


        public NoaaPageViewModel(IMapper mapper, INoaaService noaaService, IMessageService messageService)
        {
            PageName = ApplicationPageNames.Noaa;
            _mapper = mapper;
            _apiClient = noaaService;
            _messageService = messageService;
            Map = new Mapsui.Map();

        }

        #region Initialization

        public async Task LoadAsync()
        {
            try
            {
                await InitializeMapAsync();
                //await ReloadAircraftAsync();
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

        private async Task InitializeMapAsync()
        {
            try
            {
                ApplyMapTheme();

                // Center on Portugal
                var portugalCenter = new Mapsui.MPoint(-770000, 4780000);
                Map.Navigator.CenterOnAndZoomTo(portugalCenter, Map.Navigator.Resolutions[7]);

                // Pan limits
                var portugalExtent = new Mapsui.MRect(-1500000, 4200000, -300000, 5400000);
                Map.Navigator.OverridePanBounds = portugalExtent;

                Map.RefreshGraphics();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync($"Map Error: {ex.Message}", MessageDialogType.Error);
            }
        }
        #endregion

        #region Private Methods
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
    }
}
