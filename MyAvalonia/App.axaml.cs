using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MyAvalonia.Data;
using MyAvalonia.Factory;
using MyAvalonia.Integrations;
using MyAvalonia.ViewModels;
using MyAvalonia.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyAvalonia
{
	public partial class App : Application
	{
		private IServiceProvider _serviceProvider;
		private bool _isErrorWindowOpen;

		internal IServiceProvider ServiceProvider => _serviceProvider;

		internal static App Current { get; private set; }

		public App()
		{
			Current = this;
		}

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			// GLOBAL EXCEPTION HANDLERS
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				if (e.ExceptionObject is Exception ex)
					HandleException(ex);
			};

			TaskScheduler.UnobservedTaskException += (s, e) =>
			{
				HandleException(e.Exception);
				e.SetObserved();
			};

			Dispatcher.UIThread.UnhandledException += (s, e) =>
			{
				HandleException(e.Exception);
				e.Handled = true;
			};

			var collection = new ServiceCollection();

			// IPMA
			collection.AddApplicationServices();
			collection.AddIntegrations();

			// ViewModels
			collection.AddSingleton<MainWindowViewModel>();
			collection.AddTransient<WeatherForecastPageViewModel>();
			collection.AddTransient<HomePageViewModel>();
			collection.AddTransient<ProcessPageViewModel>();
			collection.AddTransient<MacrosPageViewModel>();
			collection.AddTransient<ActionsPageViewModel>();
			collection.AddTransient<ReporterPageViewModel>();
			collection.AddTransient<HistoryPageViewModel>();
			collection.AddTransient<SettingsPageViewModel>();

			// Factory
			collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(provider => name => name switch
			{
				ApplicationPageNames.WeatherForecast => provider.GetRequiredService<WeatherForecastPageViewModel>(),
				ApplicationPageNames.Home => provider.GetRequiredService<HomePageViewModel>(),
				ApplicationPageNames.Process => provider.GetRequiredService<ProcessPageViewModel>(),
				ApplicationPageNames.Macros => provider.GetRequiredService<MacrosPageViewModel>(),
				ApplicationPageNames.Actions => provider.GetRequiredService<ActionsPageViewModel>(),
				ApplicationPageNames.Reporter => provider.GetRequiredService<ReporterPageViewModel>(),
				ApplicationPageNames.History => provider.GetRequiredService<HistoryPageViewModel>(),
				ApplicationPageNames.Settings => provider.GetRequiredService<SettingsPageViewModel>(),
				_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
			});

			// PageFactory
			collection.AddSingleton<PageFactory>();

			_serviceProvider = collection.BuildServiceProvider();

			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				DisableAvaloniaDataAnnotationValidation();

				desktop.MainWindow = new MainWindow
				{
					DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
				};
			}

			base.OnFrameworkInitializationCompleted();
		}


		internal void HandleException(Exception ex)
		{
			if (_isErrorWindowOpen)
				return;

			_isErrorWindowOpen = true;

			Dispatcher.UIThread.Post(() =>
			{
				var window = new ErrorWindow
				{
					DataContext = new ErrorWindowViewModel(ex.ToString())
				};

				window.Closed += (_, __) => _isErrorWindowOpen = false;

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
					desktop.MainWindow != null)
				{
					window.Show(desktop.MainWindow);
				}
			});
		}

		private void DisableAvaloniaDataAnnotationValidation()
		{
			// Get an array of plugins to remove
			var dataValidationPluginsToRemove =
				BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

			// remove each entry found
			foreach (var plugin in dataValidationPluginsToRemove)
			{
				BindingPlugins.DataValidators.Remove(plugin);
			}
		}
	}
}