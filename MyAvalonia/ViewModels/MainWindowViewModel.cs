using AutoMapper;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyAvalonia.Data;
using MyAvalonia.Factory;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Integrations.Services;

namespace MyAvalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private PageFactory _pageFactory;

        [ObservableProperty]
        private bool _sideMenuExpanded = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WeatherPageIsActive))]
        [NotifyPropertyChangedFor(nameof(SeismologyPageIsActive))]
        [NotifyPropertyChangedFor(nameof(OepnSkyPageIsActive))]
        private PageViewModel _currentPage;

        public bool WeatherPageIsActive => CurrentPage.PageName == ApplicationPageNames.WeatherForecast;
        public bool SeismologyPageIsActive => CurrentPage.PageName == ApplicationPageNames.Seismology;
        public bool OepnSkyPageIsActive => CurrentPage.PageName == ApplicationPageNames.OepnSky;
        public bool SettingsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Settings;

        public MainWindowViewModel()
        {
            if (Design.IsDesignMode)
            {
                // Página fake para preview
                CurrentPage = new WeatherForecastPageViewModel();
            }
        }

        public MainWindowViewModel(PageFactory pageFactory)
        {
            _pageFactory = pageFactory;
            GoToWeather();
        }


        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpanded = !SideMenuExpanded;
        }

        [RelayCommand]
        private void GoToWeather()
        {
            CurrentPage = _pageFactory.GetPage(ApplicationPageNames.WeatherForecast);
        }

        [RelayCommand]
        private void GoToSeismology()
        {
            CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Seismology);
        }

        [RelayCommand]
        private void GoToOepnSky()
        {
            CurrentPage = _pageFactory.GetPage(ApplicationPageNames.OepnSky);
        }

        [RelayCommand]
        private void GoToSettings()
        {
            CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Settings);
        }
    }
}
