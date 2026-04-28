using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Core.Startup;
using OmniWatch.Factory;
using OmniWatch.Integrations;
using OmniWatch.Interfaces;
using OmniWatch.Services;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.ViewModels.Settings;
using OmniWatch.Views;
using OmniWatch.Views.Splash;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;

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
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLoggingServices();

                services.AddApplicationServices();
                services.AddIntegrations();

                services.AddSingleton<AppInitializer>();

                services.AddDataProtection()
                    .PersistKeysToFileSystem(
                        new DirectoryInfo(
                            Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "OmniWatch-Keys")));

                services.AddSingleton<ISecretService, SecretService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ISecretResetService, SecretResetService>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<IMessageService, MessageService>();

                services.AddTransient<WeatherForecastPageViewModel>();
                services.AddTransient<SeismologyPageViewModel>();
                services.AddTransient<OpenSkyPageViewModel>();
                services.AddTransient<NoaaPageViewModel>();
                services.AddTransient<SettingsPageViewModel>();
                services.AddTransient<ProgressControlViewModel>();

                services.AddSingleton<IPageFactory, PageFactory>();
            })
            .UseSerilog()
            .Build();

        _serviceProvider = host.Services;
        var serviceProvider = _serviceProvider;

        var logger = serviceProvider.GetRequiredService<ILogger<App>>();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            logger.LogCritical(e.ExceptionObject as Exception, "UI DOMAIN ERROR");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            logger.LogCritical(e.Exception, "UNOBSERVED TASK ERROR");
            e.SetObserved();
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            splash.Show();

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    serviceProvider.GetRequiredService<AppInitializer>().Initialize();

                    var main = new MainWindow
                    {
                        DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
                    };

                    desktop.MainWindow = main;
                    main.Show();

                    await Task.Delay(50);
                    splash.Close();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "APP INITIALIZATION FAILED");
                    throw;
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}