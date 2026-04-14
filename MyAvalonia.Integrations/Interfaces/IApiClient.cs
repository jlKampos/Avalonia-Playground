using MyAvalonia.Integrations.Enums;

namespace MyAvalonia.Integrations.Interfaces
{
    public interface IApiClient
    {
        Task<T> GetAsync<T>(string endpoint, ApiType api);
    }
}
