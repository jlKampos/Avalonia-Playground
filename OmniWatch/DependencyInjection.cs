using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Mapping;
namespace OmniWatch
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<IpmaMappingProfile>();
            }, loggerFactory);

            var mapper = config.CreateMapper();

            services.AddSingleton(mapper);
            services.AddTransient<ApiExceptionHandler>();
            return services;
        }
    }
}
