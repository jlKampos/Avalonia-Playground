using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.Views.MessageDialog;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Services
{
    public class MessageService : IMessageService
    {
        public async Task<MessageDialogResult> ShowAsync(string message, MessageDialogType type)
        {
            // O InvokeAsync garante que TODO o bloco de criação e exibição corre na UI Thread
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var title = type switch
                {
                    MessageDialogType.Unknown => "Unknown",
                    MessageDialogType.Warning => "Aviso",
                    MessageDialogType.Error => "Erro",
                    MessageDialogType.Success => "Sucesso",
                    MessageDialogType.Information => "Informação",
                    _ => "Mensagem"
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
