using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniWatch.Data;
using OmniWatch.Factory;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.Views.Progress;
using System.Threading.Tasks;

namespace OmniWatch.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private IPageFactory _pageFactory;

        [ObservableProperty]
        private bool _sideMenuExpanded = true;

        [ObservableProperty]
        private bool _isLoadingPage;

        [ObservableProperty]
        private ProgressControlViewModel _progressControl;


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

        public MainWindowViewModel(ProgressControlViewModel progressControl)
        {
            ProgressControl = progressControl;
            if (Design.IsDesignMode)
            {
                // Fake page for preview
                CurrentPage = new WeatherForecastPageViewModel();
            }
        }

        public MainWindowViewModel(IPageFactory pageFactory, ProgressControlViewModel progressControl)
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

        // Centralized navigation logic (fixes loops not stopping)
        private async Task NavigateToAsync(ApplicationPageNames pageName, string loadingMessage)
        {
            IsLoadingPage = true;
            ProgressControl.Message = loadingMessage;

            // 1. Unload previous page (IMPORTANT)
            if (CurrentPage is IAsyncPage oldPage)
                await oldPage.UnloadAsync();

            // 2. Load new page
            var page = _pageFactory.GetPage(pageName);
            CurrentPage = page;

            if (page is IAsyncPage newPage)
                await newPage.LoadAsync();

            IsLoadingPage = false;
        }

        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpanded = !SideMenuExpanded;
        }

        // All navigation commands now use NavigateToAsync()

        [RelayCommand]
        private Task GoToWeather() =>
            NavigateToAsync(ApplicationPageNames.WeatherForecast, "Loading Weather Forecast page");

        [RelayCommand]
        private Task GoToSeismology() =>
            NavigateToAsync(ApplicationPageNames.Seismology, "Loading Seismology page");

        [RelayCommand]
        private Task GoToOpenSky() =>
            NavigateToAsync(ApplicationPageNames.OpenSky, "Loading OpenSky page");

        [RelayCommand]
        private Task GoToNoaa() =>
            NavigateToAsync(ApplicationPageNames.Noaa, "Loading NOAA NHC page");

        [RelayCommand]
        private Task GoToSettings() =>
            NavigateToAsync(ApplicationPageNames.Settings, "Loading Settings page");
    }
}
