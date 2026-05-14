using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OmniWatch.Interfaces;
using OmniWatch.Localization;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.Views.MessageDialog;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Services
{
    public class MessageService : IMessageService
    {
        private string Translation(string key) =>
         LanguageManager.Instance[key];

        public async Task<MessageDialogResult> ShowAsync(string message, MessageDialogType type)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var title = type switch
                {
                    MessageDialogType.Unknown => Translation("MessageDialog_Unknown"),
                    MessageDialogType.Warning => Translation("MessageDialog_Warning"),
                    MessageDialogType.Error => Translation("MessageDialog_Error"),
                    MessageDialogType.Success => Translation("MessageDialog_Success"),
                    MessageDialogType.Information => Translation("MessageDialog_Information"),
                    _ => Translation("MessageDialog_Message")
                };

                var vm = new MessageDialogBoxViewModel(title, message, type);
                var dialog = new MessageDialogBox
                {
                    DataContext = vm
                };

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var owner = desktop.MainWindow;

                    if (owner != null)
                    {
                        return await dialog.ShowDialog<MessageDialogResult>(owner);
                    }
                }

                return MessageDialogResult.Cancel;
            });
        }
    }
}
