using OmniWatch.Integrations.Enums;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IApiClient
    {
        Task<Stream> GetStreamAsync(string endpoint, ApiType type);

        Task<T> GetAsync<T>(string endpoint, ApiType type, string? bearerToken = null);
    }
}
