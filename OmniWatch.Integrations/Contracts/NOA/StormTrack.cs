using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class StormTrack
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;

        public List<StormTrackPointItem> Track { get; set; } = new();
    }
}
