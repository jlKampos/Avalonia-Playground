using System;

namespace OmniWatch.Models.Noaa
{
    public class CycloneForecastDto
    {
        public DateTime Time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
