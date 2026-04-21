using OmniWatch.Integrations.Contracts.NOA;

namespace OmniWatch.Integrations.Interfaces
{
    public interface INoaaService
    {
        Task<List<CycloneItem>> GetActiveCyclonesAsync();

        Task<CycloneItem?> GetCycloneByIdAsync(string id);
    }
}
