using System;

namespace OmniWatch.Models.IPMA.Weather
{
    public class WeatherTypeDto
    {
        public int IdWeatherType { get; set; }

        public string DescriptionPT { get; set; } = String.Empty;

        public string DescriptionEN { get; set; } = String.Empty;

        public bool IsValid => IdWeatherType >= 0;

        public string WeatherIconPath => $"avares://OmniWatch/Assets/Images/Weather/w_ic_d_{IdWeatherType:D2}.svg";

    }
}
