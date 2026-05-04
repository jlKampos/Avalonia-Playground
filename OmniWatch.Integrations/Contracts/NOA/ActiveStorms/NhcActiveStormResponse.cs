using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.NOA.ActiveStorms
{
    public class NhcActiveStormResponse
    {
        [JsonPropertyName("activeStorms")]
        public List<ActiveStormItem> ActiveStorms { get; set; }
    }
}
