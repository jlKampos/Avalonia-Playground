using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Data;

namespace OmniWatch.ViewModels
{
    public partial class PageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ApplicationPageNames _pageName;
    }
}
