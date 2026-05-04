namespace OmniWatch.Integrations.Contracts.NOA
{
    public class StormTrack
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;

        public int Season { get; set; }
        public List<StormTrackPointItem> Track { get; set; } = new();
    }
}
