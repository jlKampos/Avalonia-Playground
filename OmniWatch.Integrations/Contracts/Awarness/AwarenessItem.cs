using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Awarness
{
    public class AwarenessItem
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("awarenessTypeName")]
        public string AwarenessTypeName { get; set; } = string.Empty;

        [JsonPropertyName("idAreaAviso")]
        public string IdAreaAviso { get; set; } = string.Empty;

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("awarenessLevelID")]
        public string AwarenessLevelID { get; set; } = string.Empty;

        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }
    }
}
