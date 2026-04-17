using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.Views.MessageDialog;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Services
{
    public class MessageService : IMessageService
    {
        public async Task ShowAsync(string message, MessageDialogType type)
        {
            var title = type switch
            {
                MessageDialogType.Unknown => "Unknown",
                MessageDialogType.Warning => "Warning",
                MessageDialogType.Error => "Error",
                MessageDialogType.Success => "Success",
                MessageDialogType.Information => "Information",
                _ => "Message"
            };

            var vm = new MessageDialogBoxViewModel(title, message, type);
            var dialog = new MessageDialogBox { DataContext = vm };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await dialog.ShowDialog(desktop.MainWindow);
            }
        }
    }
}
