using OmniWatch.Integrations.Contracts.OpenSky;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IOpenSkyService
    {
        Task<OpenSkyResponse> GetAllFlightStatesAsync();
    }
}
