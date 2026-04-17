using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;

namespace OmniWatch.ViewModels
{
    public partial class SettingsPageViewModel : PageViewModel
    {
        private ProgressControlViewModel _progressControl;
        private IMessageService _messageService;

        public SettingsPageViewModel(ProgressControlViewModel progressControl, IMessageService messageService)
        {
            PageName = Data.ApplicationPageNames.Seismology;
            _progressControl = progressControl;
            _messageService = messageService;

            _messageService.ShowAsync("Computer says no!\nCof cof", MessageDialog.MessageDialogBoxViewModel.MessageDialogType.Information);
        }
    }
}
