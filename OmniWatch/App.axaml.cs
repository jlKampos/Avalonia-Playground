using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
using Serilog;
using System;
using System.Threading.Tasks;

namespace OmniWatch;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    internal IServiceProvider Services => _serviceProvider;

    public static App Current { get; private set; } = null!;

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
        var services = new ServiceCollection();

        // Logging
        services.AddLoggingServices();

        // Core
        services.AddApplicationServices();
        services.AddIntegrations();

        services.AddSingleton<AppInitializer>();
        services.AddSingleton<ISecretService, SecretService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISecretResetService, SecretResetService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddTransient<WeatherForecastPageViewModel>();
        services.AddTransient<SeismologyPageViewModel>();
        services.AddTransient<OpenSkyPageViewModel>();
        services.AddTransient<NoaaPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<ProgressControlViewModel>();

        services.AddSingleton<IPageFactory, PageFactory>();

        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<Serilog.ILogger>();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            logger.Fatal(e.ExceptionObject as Exception, "UI DOMAIN ERROR");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            logger.Fatal(e.Exception, "UNOBSERVED TASK ERROR");
            e.SetObserved();
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            splash.Show();

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    _serviceProvider.GetRequiredService<AppInitializer>().Initialize();

                    var main = new MainWindow
                    {
                        DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
                    };

                    desktop.MainWindow = main;
                    main.Show();

                    await Task.Delay(50);
                    splash.Close();
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "APP INITIALIZATION FAILED");
                    throw;
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}