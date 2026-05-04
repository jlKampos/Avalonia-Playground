using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using OmniWatch.Integrations.Services;

namespace OmniWatch.Integrations
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIntegrations(this IServiceCollection services)
        {

            // --- SQLITE ---
            var dbName = "omniwatch_cache.db";
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmniWatch",
                dbName);

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            services.AddDbContext<NoaaCacheContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath}");
            });

            // Temp scope 
            using (var serviceProvider = services.BuildServiceProvider())
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();
                    db.Database.EnsureCreated();
                }
            }
            // ---------------------------

            services.AddSingleton<IApiClient, ApiClient>();


            // NOAA SERVICE (Agora vai receber o contexto via DI)
            services.AddTransient<INoaaService, NoaaService>();

            services.AddSingleton<IApiClient, ApiClient>();

            // IPMA
            services.AddHttpClient(ApiType.Ipma.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://api.ipma.pt/open-data/");
            });

            services.AddTransient<IIpmaService, IpmaService>();


            // OpenSky API
            services.AddHttpClient(ApiType.OpenSky.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://opensky-network.org/api/");
            });

            services.AddTransient<IOpenSkyService, OpenSkyService>();


            // OpenSky OAuth2
            services.AddHttpClient("OpenSkyAuth", client =>
            {
                client.BaseAddress = new Uri("https://auth.opensky-network.org/");
            });

            services.AddSingleton<IOpenSkyTokenManager, OpenSkyTokenManager>();


            // NOAA ACTIVE STORMS (KML)
            services.AddHttpClient(ApiType.Noaa.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://www.nhc.noaa.gov/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "OmniWatch/1.0");
            });

            services.AddTransient<INoaaService, NoaaService>();

            // IBTrACS (HISTORICAL DATA)
            services.AddHttpClient<IIbtracsClient, IbtracsClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
                client.DefaultRequestHeaders.Add("User-Agent", "OmniWatch/1.0");

                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            });

            services.AddSingleton<IGlobalProgressService, GlobalProgressService>();

            return services;
        }

    }
}
