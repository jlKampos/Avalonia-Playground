using Serilog;

namespace OmniWatch.Logging
{
    public static class AppLogger
    {
        public static ILogger Logger { get; private set; }

        public static void Init()
        {
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    "logs/omniwatch-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            Logger.Information("Logger initialized");
        }
    }
}
