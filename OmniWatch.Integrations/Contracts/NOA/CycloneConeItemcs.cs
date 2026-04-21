using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class CycloneConeItem
    {
        [JsonPropertyName("points")]
        public List<CycloneConePoint> Points { get; set; } = new();
    }
}
