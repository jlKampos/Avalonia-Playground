namespace OmniWatch.Integrations.Contracts.NOA
{
    public class NoaaStormResponse
    {
        public List<NoaaStormItem> Storms { get; set; } = new();
    }
}
