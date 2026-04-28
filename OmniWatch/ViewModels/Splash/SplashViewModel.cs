using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.ViewModels.ProgressControl;

namespace OmniWatch.ViewModels.Splash
{
    public partial class SplashViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ProgressControlViewModel _progressControl;
    }
}
