using OmniWatch.Data;
using OmniWatch.ViewModels;

namespace OmniWatch.Interfaces
{
    public interface IPageFactory
    {
        PageViewModel GetPage(ApplicationPageNames pageName);
    }
}
