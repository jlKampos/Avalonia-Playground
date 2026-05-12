using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Startup;

namespace OmniWatch.Integrations
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIntegrations(this IServiceCollection services)
        {
            // =========================
            // SQLITE PATH
            // =========================
            var dbName = "omniwatch_cache.db";
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmniWatch",
                dbName);

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // =========================
            // DB CONTEXT
            // =========================
            services.AddDbContext<NoaaCacheContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // =========================
            // DB BOOTSTRAP (IMPORTANT)
            // =========================
            services.AddSingleton<DatabaseBootstrap>();

            // =========================
            // SERVICES
            // =========================
            services.AddSingleton<IApiClient, ApiClient>();
            services.AddTransient<INoaaService, NoaaService>();
            services.AddTransient<IIpmaService, IpmaService>();
            services.AddTransient<IOpenSkyService, OpenSkyService>();
            services.AddSingleton<IOpenSkyTokenManager, OpenSkyTokenManager>();
            services.AddSingleton<IGlobalProgressService, GlobalProgressService>();

            // =========================
            // HTTP CLIENTS
            // =========================
            services.AddHttpClient(ApiType.Ipma.ToString(), c =>
                c.BaseAddress = new Uri("https://api.ipma.pt/open-data/"));

            services.AddHttpClient(ApiType.OpenSky.ToString(), c =>
                c.BaseAddress = new Uri("https://opensky-network.org/api/"));

            // OpenSky OAuth2
            services.AddHttpClient(ApiType.OpenSkyAuth.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://auth.opensky-network.org/");
            });

            services.AddHttpClient(ApiType.Noaa.ToString(), c =>
            {
                c.BaseAddress = new Uri("https://www.nhc.noaa.gov/");
                c.Timeout = TimeSpan.FromSeconds(30);
                c.DefaultRequestHeaders.Add("User-Agent", "OmniWatch/1.0");
            });

            services.AddHttpClient<IIbtracsClient, IbtracsClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
                client.DefaultRequestHeaders.Add("User-Agent", "OmniWatch/1.0");

                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            });

            return services;
        }
    }
}