using MyAvalonia.Integrations.Contracts.OpenSky;

namespace MyAvalonia.Integrations.Interfaces
{
    public interface IOpenSkyService
    {
        Task<OpenSkyResponse> GetAllFlightStatesAsync();
    }
}
