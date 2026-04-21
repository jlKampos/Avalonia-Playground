using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Logging;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OmniWatch
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect();
    }
}

