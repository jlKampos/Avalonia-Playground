using OmniWatch.Integrations.Enums;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IApiClient
    {
        Task<T> GetAsync<T>(string endpoint, ApiType api);
    }
}
