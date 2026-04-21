using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniWatch.Data;
using OmniWatch.Factory;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;
using System.Threading.Tasks;

namespace OmniWatch.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private PageFactory _pageFactory;

        [ObservableProperty]
        private bool _sideMenuExpanded = true;

        [ObservableProperty]
        private bool _isLoadingPage;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WeatherPageIsActive))]
        [NotifyPropertyChangedFor(nameof(SeismologyPageIsActive))]
        [NotifyPropertyChangedFor(nameof(OpenSkyPageIsActive))]
        [NotifyPropertyChangedFor(nameof(NoaaPageIsActive))]
        [NotifyPropertyChangedFor(nameof(SettingsPageIsActive))]
        private PageViewModel _currentPage;

        public bool WeatherPageIsActive => CurrentPage.PageName == ApplicationPageNames.WeatherForecast;
        public bool SeismologyPageIsActive => CurrentPage.PageName == ApplicationPageNames.Seismology;
        public bool OpenSkyPageIsActive => CurrentPage.PageName == ApplicationPageNames.OpenSky;
        public bool NoaaPageIsActive => CurrentPage.PageName == ApplicationPageNames.Noaa;
        public bool SettingsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Settings;

        public MainWindowViewModel()
        {
            if (Design.IsDesignMode)
            {
                // Página fake para preview
                CurrentPage = new WeatherForecastPageViewModel();
            }
        }

        public MainWindowViewModel(PageFactory pageFactory, ProgressControlViewModel progressControl)
        {
            _pageFactory = pageFactory;
            ProgressControl = progressControl;
            _ = LoadInitialPageAsync();

        }

        private async Task LoadInitialPageAsync()
        {
            IsLoadingPage = true;

            var page = _pageFactory.GetPage(ApplicationPageNames.WeatherForecast);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }


        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpanded = !SideMenuExpanded;
        }

        [RelayCommand]
        private async Task GoToWeather()
        {
            IsLoadingPage = true;
            ProgressControl.Message = "Loading Weather Forecast page";

            var page = _pageFactory.GetPage(ApplicationPageNames.WeatherForecast);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }

        [RelayCommand]
        private async Task GoToSeismology()
        {

            IsLoadingPage = true;
            ProgressControl.Message = "Loading Seismology page";

            var page = _pageFactory.GetPage(ApplicationPageNames.Seismology);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }

        [RelayCommand]
        private async Task GoToOpenSky()
        {
            IsLoadingPage = true;
            ProgressControl.Message = "Loading OpenSky page";

            var page = _pageFactory.GetPage(ApplicationPageNames.OpenSky);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }

        [RelayCommand]
        private async Task GoToNoaa()
        {
            IsLoadingPage = true;
            ProgressControl.Message = "Loading NOAA NHC page";

            var page = _pageFactory.GetPage(ApplicationPageNames.Noaa);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }

        [RelayCommand]
        private async Task GoToSettings()
        {
            IsLoadingPage = true;
            ProgressControl.Message = "Loading OpenSKy page";

            var page = _pageFactory.GetPage(ApplicationPageNames.Settings);
            CurrentPage = page;

            if (page is IAsyncPage asyncPage)
                await asyncPage.LoadAsync();

            IsLoadingPage = false;
        }
    }
}
