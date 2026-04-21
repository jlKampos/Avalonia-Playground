using System.Collections.Generic;

namespace OmniWatch.Models.Noaa
{
    public class CycloneConeDto
    {
        public List<(double Lat, double Lon)> Points { get; set; } = new();
    }
}
