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
            // 1. Inicializa o Logger globalmente
            Log.Logger = LoggingSetup.CreateLogger();

            try
            {
                Log.Information("OmniWatch está iniciando...");

                // 2. Inicia o app
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "O aplicativo encerrou inesperadamente.");
                throw;
            }
            finally
            {
                // 3. OBRIGATÓRIO: Descarrega os logs e fecha o arquivo corretamente
                Log.CloseAndFlush();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace(); // Ajuda a ver logs do Avalonia no console
    }
}


