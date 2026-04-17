using Microsoft.Extensions.DependencyInjection;
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


            // OpenSky
            services.AddHttpClient(ApiType.OpenSky.ToString(), client =>
            {
                client.BaseAddress = new Uri("https://opensky-network.org/api/");
            });

            services.AddTransient<IOpenSkyService, OpenSkyService>();


            return services;
        }
    }
}
