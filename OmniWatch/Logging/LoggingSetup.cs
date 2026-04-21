using Serilog;
using System.IO;

namespace OmniWatch.Logging
{
    public static class LoggingSetup
    {
        public static ILogger CreateLogger()
        {
            Directory.CreateDirectory("logs");

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/omniwatch-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();
        }
    }
}
