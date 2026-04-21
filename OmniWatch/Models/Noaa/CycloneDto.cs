using System.Collections.Generic;

namespace OmniWatch.Models.Noaa
{
    public class CycloneDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int Category { get; set; }
        public int WindSpeed { get; set; }
        public int Pressure { get; set; }

        public List<CycloneForecastDto> Forecast { get; set; } = new();
        public List<CycloneConeDto> Cone { get; set; } = new();
    }
}
