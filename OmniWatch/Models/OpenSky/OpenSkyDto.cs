using System.Collections.Generic;

namespace OmniWatch.Models.OpenSky
{
    public class OpenSkyDto
    {
        public long Time { get; set; }
        public List<StateVectorDto> States { get; set; } = new List<StateVectorDto>();
    }
}

