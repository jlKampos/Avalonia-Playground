using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Core.Startup;
using OmniWatch.Data;
using OmniWatch.Factory;
using OmniWatch.Integrations;
using OmniWatch.Interfaces;
using OmniWatch.Logging;
using OmniWatch.Services;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.ViewModels.Settings;
using OmniWatch.Views;
using OmniWatch.Views.Splash;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                AppLogger.Logger.Fatal(e.ExceptionObject as Exception, "UI DOMAIN ERROR");
            };
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var collection = new ServiceCollection();

            // IPMA
            collection.AddApplicationServices();
            collection.AddIntegrations();

            // Settings / Core
            collection.AddSingleton<AppInitializer>();
            collection.AddSingleton<ISecretService, SecretService>();
            collection.AddSingleton<ISettingsService, SettingsService>();

            // ViewModels
            collection.AddSingleton<MainWindowViewModel>();
            collection.AddSingleton<IMessageService, MessageService>();
            collection.AddTransient<WeatherForecastPageViewModel>();
            collection.AddTransient<SeismologyPageViewModel>();
            collection.AddTransient<OpenSkyPageViewModel>();
            collection.AddTransient<NoaaPageViewModel>();
            collection.AddTransient<SettingsPageViewModel>();
            collection.AddTransient<ProgressControlViewModel>();

            // PageFactory
            collection.AddSingleton<PageFactory>();

            _serviceProvider = collection.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var splash = new SplashWindow();
                splash.Show();

                Dispatcher.UIThread.Post(async () =>
                {
                    _serviceProvider.GetRequiredService<AppInitializer>().Initialize();

                    var main = new MainWindow
                    {
                        DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
                    };

                    main.Show();
                    desktop.MainWindow = main;

                    await Task.Delay(50);
                    splash.Close();
                });
            }

            base.OnFrameworkInitializationCompleted();
        }
        //private void DisableAvaloniaDataAnnotationValidation()
        //{
        //    var plugins = BindingPlugins.DataValidators
        //        .OfType<DataAnnotationsValidationPlugin>()
        //        .ToArray();

        //    foreach (var plugin in plugins)
        //    {
        //        BindingPlugins.DataValidators.Remove(plugin);
        //    }
        //}
    }
}