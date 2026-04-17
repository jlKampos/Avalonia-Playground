using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Core.Startup;
using OmniWatch.Data;
using OmniWatch.Factory;
using OmniWatch.Integrations;
using OmniWatch.Interfaces;
using OmniWatch.Services;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.ViewModels.Settings;
using OmniWatch.Views;
using System;
using System.Linq;

namespace OmniWatch
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

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

            var collection = new ServiceCollection();

            // IPMA
            collection.AddApplicationServices();
            collection.AddIntegrations();

            // Settings 
            collection.AddSingleton<AppInitializer>();
            collection.AddSingleton<ISecretService, SecretService>();
            collection.AddSingleton<ISettingsService, SettingsService>();



            // ViewModels
            collection.AddSingleton<MainWindowViewModel>();
            collection.AddSingleton<IMessageService, MessageService>();
            collection.AddTransient<WeatherForecastPageViewModel>();
            collection.AddTransient<SeismologyPageViewModel>();
            collection.AddTransient<OepnSkyPageViewModel>();
            collection.AddTransient<SettingsPageViewModel>();
            collection.AddTransient<ProgressControlViewModel>();

            //// Factory
            //collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(provider => name => name switch
            //{
            //    ApplicationPageNames.WeatherForecast => provider.GetRequiredService<WeatherForecastPageViewModel>(),
            //    ApplicationPageNames.Seismology => provider.GetRequiredService<SeismologyPageViewModel>(),
            //    ApplicationPageNames.OepnSky => provider.GetRequiredService<OepnSkyPageViewModel>(),
            //    ApplicationPageNames.Settings => provider.GetRequiredService<SettingsPageViewModel>(),
            //    _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            //});

            // PageFactory
            collection.AddSingleton<PageFactory>();

            _serviceProvider = collection.BuildServiceProvider();

            // Settings initialization
            var initializer = _serviceProvider.GetRequiredService<AppInitializer>();
            initializer.Initialize();

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