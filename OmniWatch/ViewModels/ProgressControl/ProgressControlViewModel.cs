using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Core.Interfaces;
using OmniWatch.Interfaces;

namespace OmniWatch.ViewModels.ProgressControl
{
    public partial class ProgressControlViewModel : ObservableObject
    {
        private readonly IGlobalProgressService _progressService;

        [ObservableProperty]
        private string _title = "Loading";

        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private bool _isVisible;

        public ProgressControlViewModel(IGlobalProgressService progress)
        {
            progress.ProgressChanged += msg =>
            {
                Message = msg;
            };
        }

    }
}
