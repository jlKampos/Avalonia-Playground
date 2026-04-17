using OmniWatch.Data;
using OmniWatch.ViewModels;
using System;

namespace OmniWatch.Factory
{
    public class PageFactory
    {
        private readonly Func<ApplicationPageNames, PageViewModel> _factory;

        public PageFactory(Func<ApplicationPageNames, PageViewModel> factory)
        {
            _factory = factory;
        }

        public PageViewModel GetPage(ApplicationPageNames pageName) => _factory.Invoke(pageName);

    }
}
