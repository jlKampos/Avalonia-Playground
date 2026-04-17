using System;

namespace OmniWatch.Models.Wind
{
    public class WindSpeedDto
    {
        public string ClassWindSpeed { get; set; } = String.Empty;

        public string DescriptionPT { get; set; } = String.Empty;

        public string DescriptionEN { get; set; } = String.Empty;

        public int ClassWindSpeedValue => int.TryParse(ClassWindSpeed, out var v) ? v : -99;
    }
}
