using OmniWatch.Integrations.Enums;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IApiClient
    {

        Task<T?> GetAsync<T>(string endpoint, ApiType type, string? bearerToken = null, CancellationToken ct = default);
        Task<Stream> GetStreamAsync(string endpoint, ApiType type, CancellationToken ct = default);

    }
}
