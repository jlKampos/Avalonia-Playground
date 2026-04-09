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
		[NotifyPropertyChangedFor(nameof(HomePageIsActive))]
		[NotifyPropertyChangedFor(nameof(ProcessPageIsActive))]
		[NotifyPropertyChangedFor(nameof(MacrosPageIsActive))]
		[NotifyPropertyChangedFor(nameof(ActionsPageIsActive))]
		[NotifyPropertyChangedFor(nameof(ReporterPageIsActive))]
		[NotifyPropertyChangedFor(nameof(HistoryPageIsActive))]
		[NotifyPropertyChangedFor(nameof(SettingsPageIsActive))]
		private PageViewModel _currentPage;

		public bool WeatherPageIsActive => CurrentPage.PageName == ApplicationPageNames.WeatherForecast;
		public bool HomePageIsActive => CurrentPage.PageName == ApplicationPageNames.Home;
		public bool ProcessPageIsActive => CurrentPage.PageName == ApplicationPageNames.Process;
		public bool MacrosPageIsActive => CurrentPage.PageName == ApplicationPageNames.Macros;
		public bool ActionsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Actions;
		public bool ReporterPageIsActive => CurrentPage.PageName == ApplicationPageNames.Reporter;
		public bool HistoryPageIsActive => CurrentPage.PageName == ApplicationPageNames.History;
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
		private void GoToHome()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Home);
		}

		[RelayCommand]
		private void GoToProcess()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Process);
		}

		[RelayCommand]
		private void GoToMacros()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Macros);
		}

		[RelayCommand]
		private void GoToActions()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Actions);
		}

		[RelayCommand]
		private void GoToReporter()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Reporter);
		}

		[RelayCommand]
		private void GoToHistory()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.History);
		}

		[RelayCommand]
		private void GoToSettings()
		{
			CurrentPage = _pageFactory.GetPage(ApplicationPageNames.Settings);
		}
	}
}
