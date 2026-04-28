using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Services;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Services;

namespace OmniWatch.Integrations
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIntegrations(this IServiceCollection services)
        {
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


            // IBTrACS SERVICE (parsing + domain)
            services.AddTransient<IIbtracsService, IbtracsService>();

            services.AddSingleton<IGlobalProgressService, GlobalProgressService>();

            return services;
        }

    }
}
