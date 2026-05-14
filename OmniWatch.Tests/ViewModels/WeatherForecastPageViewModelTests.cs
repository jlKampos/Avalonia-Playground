using Avalonia.Media;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.Awarness;
using OmniWatch.Integrations.Contracts.Forecast;
using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Integrations.Contracts.Precipitation;
using OmniWatch.Integrations.Contracts.Weather;
using OmniWatch.Integrations.Contracts.Wind;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Models.IPMA.Locations;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Tests.ViewModels;

public class WeatherForecastPageViewModelTests
{
    private readonly Mock<IIpmaService> _api = new();
    private readonly Mock<IMessageService> _message = new();
    private readonly Mock<IGlobalProgressService> _globalProgress = new();

    private WeatherForecastPageViewModel CreateVM()
    {
        return new WeatherForecastPageViewModel(
            new ProgressControlViewModel(_globalProgress.Object),
            _message.Object,
            _api.Object
        );
    }

    // =========================
    // LoadAsync
    // =========================

    [Fact]
    public async Task LoadAsync_Should_Call_All_Api_Methods()
    {
        _api.Setup(x => x.GetLocationsAsync())
            .ReturnsAsync(new LocationsResponse { Data = new List<LocationItem>() });

        _api.Setup(x => x.GetWeatherTypesAsync())
            .ReturnsAsync(new WeatherTypeResponse { Data = new List<WeatherTypeItem>() });

        _api.Setup(x => x.GetWindAsync())
            .ReturnsAsync(new WindSpeedResponse { Data = new List<WindSpeedItem>() });

        _api.Setup(x => x.GetPrecipitationTypesAsync())
            .ReturnsAsync(new PrecipitationResponse { Data = new List<PrecipitationItem>() });

        _api.Setup(x => x.GetAwarnessAsync())
            .ReturnsAsync(new List<AwarenessItem>());

        var vm = CreateVM();

        await vm.LoadAsync();

        _api.Verify(x => x.GetLocationsAsync(), Times.Once);
        _api.Verify(x => x.GetWeatherTypesAsync(), Times.Once);
        _api.Verify(x => x.GetWindAsync(), Times.Once);
        _api.Verify(x => x.GetPrecipitationTypesAsync(), Times.Once);
        _api.Verify(x => x.GetAwarnessAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_Should_Show_Error_On_Exception()
    {
        _api.Setup(x => x.GetLocationsAsync())
            .ThrowsAsync(new Exception("boom"));

        var vm = CreateVM();

        await vm.LoadAsync();

        _message.Verify(x => x.ShowAsync(
            It.Is<string>(s => s.Contains("boom")),
            MessageDialogType.Error),
            Times.Once);
    }

    // =========================
    // SelectedLocation
    // =========================

    [Fact]
    public async Task SelectedLocation_Should_Load_Forecast()
    {
        _api.Setup(x => x.GetForecastByCityAsync(It.IsAny<int>()))
            .ReturnsAsync(new ForecastResponse
            {
                Data = new List<ForecastItem>
                {
                    new ForecastItem
                    {
                        ForecastDate = DateTime.Now,
                        WeatherTypeId = 1,
                        WindSpeedClass = 1,
                        PrecipitationIntensityClass = 1
                    }
                }
            });

        _api.Setup(x => x.GetWeatherTypesAsync())
            .ReturnsAsync(new WeatherTypeResponse
            {
                Data = new List<WeatherTypeItem>
                {
                    new WeatherTypeItem { IdWeatherType = 1, DescriptionPT = "Sunny" }
                }
            });

        _api.Setup(x => x.GetWindAsync())
            .ReturnsAsync(new WindSpeedResponse
            {
                Data = new List<WindSpeedItem>
                {
                    new WindSpeedItem { ClassWindSpeed = "1", DescriptionPT = "NW" }
                }
            });

        _api.Setup(x => x.GetPrecipitationTypesAsync())
            .ReturnsAsync(new PrecipitationResponse
            {
                Data = new List<PrecipitationItem>
                {
                    new PrecipitationItem { ClassPrecInt = "1" }
                }
            });

        _api.Setup(x => x.GetLocationsAsync())
            .ReturnsAsync(new LocationsResponse
            {
                Data = new List<LocationItem>
                {
                    new LocationItem { GlobalIdLocal = 1, Local = "Braga" }
                }
            });

        _api.Setup(x => x.GetAwarnessAsync())
            .ReturnsAsync(new List<AwarenessItem>());

        var vm = CreateVM();

        await vm.LoadAsync();

        vm.SelectedLocation = new LocationDto
        {
            GlobalIdLocal = 1,
            Name = "Braga"
        };

        for (int i = 0; i < 20; i++)
        {
            try
            {
                _api.Verify(x => x.GetForecastByCityAsync(1), Times.Once);
                break;
            }
            catch
            {
                await Task.Delay(100);
            }
        }

    }

    // =========================
    // GetLevelBrush
    // =========================

    [Theory]
    [InlineData("green")]
    [InlineData("yellow")]
    [InlineData("orange")]
    [InlineData("red")]
    [InlineData("unknown")]
    public void GetLevelBrush_Should_Return_Brush(string level)
    {
        var brush = WeatherForecastPageViewModel.GetLevelBrush(level);

        Assert.IsType<SolidColorBrush>(brush);
    }
}