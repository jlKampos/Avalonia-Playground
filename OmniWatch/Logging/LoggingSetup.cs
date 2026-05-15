using Serilog;
using System;
using System.IO;

namespace OmniWatch.Logging
{
    public static class LoggingSetup
    {
        public static ILogger CreateLogger()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logFolder = Path.Combine(baseDir, "OmniWatch", "logs");

            try
            {
                // 1. PRIMEIRO cria a pasta
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                // 2. DEPOIS habilita o SelfLog (agora a pasta existe com certeza)
                Serilog.Debugging.SelfLog.Enable(msg =>
                    File.AppendAllText(Path.Combine(logFolder, "serilog_debug.txt"), msg));

                string logPath = Path.Combine(logFolder, "omniwatch-.log");

                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1)) // Força a escrita a cada segundo
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                // Se falhar aqui, o erro vai para o Console do terminal (se aberto)
                Console.WriteLine($"Erro ao configurar Serilog: {ex.Message}");

                return new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }
    }
}
