using Avalonia;
using OmniWatch.Logging;
using System;
using System.Threading.Tasks;

namespace OmniWatch
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppLogger.Init();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                AppLogger.Logger.Fatal(e.ExceptionObject as Exception,
                    "UNHANDLED DOMAIN EXCEPTION");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLogger.Logger.Fatal(e.Exception,
                    "UNOBSERVED TASK EXCEPTION");
                e.SetObserved();
            };

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                AppLogger.Logger.Fatal(ex, "FATAL APP CRASH");
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect();
    }
}

