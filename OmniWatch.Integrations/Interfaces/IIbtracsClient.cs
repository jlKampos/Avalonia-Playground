namespace OmniWatch.Integrations.Interfaces
{
    public interface IIbtracsClient
    {
        Task<string> GetLocalCsvPathAsync();
    }
}
