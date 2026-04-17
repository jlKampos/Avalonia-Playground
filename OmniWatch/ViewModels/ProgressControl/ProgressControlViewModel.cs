using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniWatch.ViewModels.ProgressControl
{
    public partial class ProgressControlViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Loading";

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private bool _isVisible;

        public ProgressControlViewModel()
        {
            if (Design.IsDesignMode)
            {
                Title = "Loading";
                Message = "Loading data!";
            }
        }
    }
}
