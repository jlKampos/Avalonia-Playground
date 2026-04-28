using OmniWatch.Integrations.Contracts.NOA;

namespace OmniWatch.Integrations.Interfaces
{
    public interface INoaaService
    {
        Task<List<StormTrack>> GetActiveStormTracksAsync();

        Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, IProgress<string>? progress = null);
    }
}
