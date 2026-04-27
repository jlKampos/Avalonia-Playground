using OmniWatch.ViewModels.ProgressControl;
using System.Threading.Tasks;

namespace OmniWatch.Interfaces
{
    public interface IAsyncPage
    {
        Task LoadAsync();
        Task UnloadAsync();
    }
}
