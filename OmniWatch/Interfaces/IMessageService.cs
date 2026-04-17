using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Interfaces
{
    public interface IMessageService
    {
        Task ShowAsync(string message, MessageDialogType type);
    }
}
