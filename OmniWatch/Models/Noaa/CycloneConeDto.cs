using System.Collections.Generic;

namespace OmniWatch.Models.Noaa
{
    public class CycloneConeDto
    {
        public List<CyclonePointDto> Points { get; set; } = new();
    }
}
