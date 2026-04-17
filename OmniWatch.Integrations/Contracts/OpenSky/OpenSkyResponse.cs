namespace OmniWatch.Integrations.Contracts.OpenSky
{
    public class OpenSkyResponse
    {
        public long Time { get; set; }
        public List<StateVectorItem> States { get; set; } = new();
    }
}
