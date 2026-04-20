using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class NoaaConeItem
    {
        [JsonPropertyName("points")]
        public List<NoaaConePoint> Points { get; set; } = new();
    }
}
