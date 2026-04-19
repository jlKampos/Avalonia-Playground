using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IOpenSkyService
    {
        Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetAllFlightStatesAsync();
    }
}
