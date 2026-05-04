using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IOpenSkyService
    {
        Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetAllFlightStatesAsync();
        Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetFlightStatesInViewportAsync(double lamin, double lomin, double lamax, double lomax);
    }
}
