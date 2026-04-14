using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyAvalonia.Integrations.Contracts.OpenSky
{
    public class OpenSkyResponse
    {
        public long Time { get; set; }
        public List<StateVectorItem> States { get; set; } = new();
    }
}
