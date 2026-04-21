using OmniWatch.Core.Enums;

namespace OmniWatch.Core.Interfaces
{
    public interface ISecretResetService
    {
        Task ResetAsync(ApiProvider provider);
    }
}
