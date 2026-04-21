using OmniWatch.Core.Enums;
using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;

namespace OmniWatch.Core.Services
{
    public class SecretResetService : ISecretResetService
    {
        private readonly ISecretService _secrets;

        public SecretResetService(ISecretService secrets)
        {
            _secrets = secrets;
        }

        public Task ResetAsync(ApiProvider provider)
        {
            return Task.WhenAll(
                _secrets.RemoveAsync(SecretKeys.ApiKey(provider)),
                _secrets.RemoveAsync(SecretKeys.Token(provider))
            );
        }
    }
}
