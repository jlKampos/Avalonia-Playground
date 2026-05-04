using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.NOA.ActiveStorms
{
    public class StormProductInfoItem
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
