using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAvalonia.Models.OpenSky
{
    public class OpenSkyDto
    {
        public long Time { get; set; }
        public List<StateVectorDto> States { get; set; } = new List<StateVectorDto>();
    }
}

