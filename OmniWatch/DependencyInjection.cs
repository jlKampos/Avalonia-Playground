using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Logging;
using OmniWatch.Mapping;
using Serilog;
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
                cfg.AddProfile<OmniWatchMappingProfile>();
            }, loggerFactory);

            var mapper = config.CreateMapper();
            services.AddSingleton<IMapper>(mapper);
            services.AddSingleton(mapper);
            services.AddTransient<ApiExceptionHandler>();
            return services;
        }

        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            var logger = LoggingSetup.CreateLogger();

            services.AddSingleton<Serilog.ILogger>(logger);

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(logger, dispose: true);
            });

            return services;
        }
    }
}
