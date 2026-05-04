using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;

namespace OmniWatch.Integrations.Interfaces
{
    public interface INoaaService
    {
        Task<NhcActiveStormResponse> GetActiveStormTracksAsync();

        Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, CancellationToken cancellationToken, IProgress<string>? progress = null);
    }
}
