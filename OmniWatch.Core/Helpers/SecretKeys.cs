using OmniWatch.Core.Enums;
using OmniWatch.Core.Models;

namespace OmniWatch.Core.Helpers
{
    public static class SecretKeys
    {
        public static SecretKey ApiKey(ApiProvider provider) => new(SecretType.ApiKey, provider.ToString());

        public static SecretKey Token(ApiProvider provider) => new(SecretType.Token, provider.ToString());
    }
}
