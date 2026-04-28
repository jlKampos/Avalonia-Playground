using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Data;
using OmniWatch.Factory;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IPageFactory> _factory = new();
    private readonly Mock<PageViewModel> _page = new();
    private readonly Mock<IGlobalProgressService> _globalProgress = new();

    private MainWindowViewModel CreateVM()
    {
        var progress = new ProgressControlViewModel(_globalProgress.Object);
        return new MainWindowViewModel(_factory.Object, progress);
    }

    // =========================
    // Constructor initial load
    // =========================

    [Fact]
    public async Task Constructor_Should_Load_Initial_Page_Eventually()
    {
        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.WeatherForecast))
            .Returns(_page.Object);

        var vm = CreateVM();

        await Task.Delay(100);

        Assert.Equal(_page.Object, vm.CurrentPage);
    }

    // =========================
    // Side menu toggle
    // =========================

    [Fact]
    public void SideMenuResize_Should_Toggle_State()
    {
        var vm = CreateVM();

        var initial = vm.SideMenuExpanded;

        vm.SideMenuResizeCommand.Execute(null);

        Assert.NotEqual(initial, vm.SideMenuExpanded);
    }

    // =========================
    // Navigation - Weather
    // =========================

    [Fact]
    public async Task GoToWeather_Should_Set_Page()
    {
        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.WeatherForecast))
            .Returns(_page.Object);

        var vm = CreateVM();

        await vm.GoToWeatherCommand.ExecuteAsync(null);

        Assert.Equal(_page.Object, vm.CurrentPage);
        Assert.False(vm.IsLoadingPage);
    }

    // =========================
    // Navigation - Seismology
    // =========================

    [Fact]
    public async Task GoToSeismology_Should_Set_Page()
    {
        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.Seismology))
            .Returns(_page.Object);

        var vm = CreateVM();

        await vm.GoToSeismologyCommand.ExecuteAsync(null);

        Assert.Equal(_page.Object, vm.CurrentPage);
    }

    // =========================
    // Navigation - OpenSky
    // =========================

    [Fact]
    public async Task GoToOpenSky_Should_Set_Page()
    {
        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.OpenSky))
            .Returns(_page.Object);

        var vm = CreateVM();

        await vm.GoToOpenSkyCommand.ExecuteAsync(null);

        Assert.Equal(_page.Object, vm.CurrentPage);
    }

    // =========================
    // Navigation - Settings
    // =========================

    [Fact]
    public async Task GoToSettings_Should_Set_Page()
    {
        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.Settings))
            .Returns(_page.Object);

        var vm = CreateVM();

        await vm.GoToSettingsCommand.ExecuteAsync(null);

        Assert.Equal(_page.Object, vm.CurrentPage);
    }

    // =========================
    // Async page load behavior
    // =========================

    [Fact]
    public async Task Navigation_Should_Call_LoadAsync_When_Page_Is_IAsyncPage()
    {
        var fakePage = new FakeAsyncPage
        {
            PageName = ApplicationPageNames.WeatherForecast
        };

        _factory
            .Setup(x => x.GetPage(ApplicationPageNames.WeatherForecast))
            .Returns(fakePage);

        var vm = CreateVM();

        await vm.GoToWeatherCommand.ExecuteAsync(null);

        Assert.True(fakePage.Loaded);
    }

    // =========================
    // UnloadAsync on previous page
    // =========================

    [Fact]
    public async Task Navigation_Should_Call_UnloadAsync_On_Previous_Page()
    {
        var oldPage = new FakeAsyncPage { PageName = ApplicationPageNames.WeatherForecast };
        var newPage = new FakeAsyncPage { PageName = ApplicationPageNames.Seismology };

        _factory.Setup(x => x.GetPage(ApplicationPageNames.WeatherForecast))
                .Returns(oldPage);

        _factory.Setup(x => x.GetPage(ApplicationPageNames.Seismology))
                .Returns(newPage);

        var vm = CreateVM();

        await vm.GoToWeatherCommand.ExecuteAsync(null);
        await vm.GoToSeismologyCommand.ExecuteAsync(null);

        Assert.True(oldPage.Unloaded);
    }

    private class FakeAsyncPage : PageViewModel, IAsyncPage
    {
        public bool Loaded { get; private set; }
        public bool Unloaded { get; private set; }

        public Task LoadAsync()
        {
            Loaded = true;
            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            Unloaded = true;
            return Task.CompletedTask;
        }
    }
}
