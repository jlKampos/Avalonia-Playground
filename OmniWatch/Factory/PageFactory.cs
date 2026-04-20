using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Data;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.Settings;
using OmniWatch.Views.Settings;
using System;

namespace OmniWatch.Factory
{
    public class PageFactory
    {
        private readonly IServiceProvider _provider;

        public PageFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public PageViewModel GetPage(ApplicationPageNames pageName)
        {
            return pageName switch
            {
                ApplicationPageNames.WeatherForecast =>
                    _provider.GetRequiredService<WeatherForecastPageViewModel>(),

                ApplicationPageNames.Seismology =>
                    _provider.GetRequiredService<SeismologyPageViewModel>(),

                ApplicationPageNames.OpenSky =>
                    _provider.GetRequiredService<OpenSkyPageViewModel>(),

                ApplicationPageNames.Noaa =>
               _provider.GetRequiredService<NoaaPageViewModel>(),

                ApplicationPageNames.Settings =>
                    _provider.GetRequiredService<SettingsPageViewModel>(),

                _ => throw new ArgumentOutOfRangeException(nameof(pageName))
            };
        }
    }
}