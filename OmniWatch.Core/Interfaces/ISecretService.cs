using OmniWatch.Core.Models;

namespace OmniWatch.Core.Interfaces
{
    public interface ISecretService
    {
        Task SetAsync(SecretKey key, string value);
        Task<string?> GetAsync(SecretKey key);
        Task RemoveAsync(SecretKey key);

    }
}
