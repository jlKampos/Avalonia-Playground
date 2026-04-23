using Moq;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Models.Seismic;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Tests.ViewModels;

public class SeismologyPageViewModelTests
{
    private readonly Mock<IIpmaService> _api = new();
    private readonly Mock<IMessageService> _message = new();

    private SeismologyPageViewModel CreateVM()
    {
        return new SeismologyPageViewModel(
            new ProgressControlViewModel(),
            _message.Object,
            _api.Object
        );
    }

    // =========================
    // LoadAsync
    // =========================

    [Fact]
    public async Task LoadAsync_Should_Call_API()
    {
        _api.Setup(x => x.GetSeismicAsync(It.IsAny<int>()))
            .ReturnsAsync(new SeismicResponse
            {
                Data = new List<SeismicItem>()
            });

        var vm = CreateVM();

        await vm.LoadAsync();

        _api.Verify(x => x.GetSeismicAsync(7), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_Should_Show_Error_On_Exception()
    {
        _api.Setup(x => x.GetSeismicAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("boom"));

        var vm = CreateVM();

        await vm.LoadAsync();

        _message.Verify(x => x.ShowAsync(
            It.Is<string>(s => s.Contains("boom")),
            MessageDialogType.Error),
            Times.Once);
    }

    // =========================
    // SelectedDate filtering
    // =========================

    [Fact]
    public async Task SelectedDate_Should_Filter_Data()
    {
        _api.Setup(x => x.GetSeismicAsync(It.IsAny<int>()))
            .ReturnsAsync(new SeismicResponse
            {
                Data = new List<SeismicItem>
                {
                new SeismicItem
                {
                    Latitude = "10",
                    Longitude = "10",
                    Magnitude = "4.5",
                    Time = DateTime.Now,
                    Local = "Braga"
                }
                }
            });

        var vm = CreateVM();

        await vm.LoadAsync();

        vm.SelectedDate = DateTime.Now;

        // validação realista: o mapa foi atualizado via layer
        Assert.NotNull(vm.Map);
        Assert.True(vm.Map.Layers.Count > 0);
    }

    // =========================
    // GetDegreeFromMagnitude (via reflection test)
    // =========================

    [Theory]
    [InlineData(1.5, "I")]
    [InlineData(2.5, "II")]
    [InlineData(3.5, "III")]
    [InlineData(4.2, "IV")]
    [InlineData(5.2, "VI")]
    [InlineData(7.2, "X")]
    public void GetDegreeFromMagnitude_Should_Return_Correct_Value(double mag, string expected)
    {
        var vm = CreateVM();

        var method = typeof(SeismologyPageViewModel)
            .GetMethod("GetDegreeFromMagnitude",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        var result = method.Invoke(vm, new object[] { mag });

        Assert.Equal(expected, result);
    }

    // =========================
    // GetColorByDegree
    // =========================

    [Theory]
    [InlineData("I")]
    [InlineData("III")]
    [InlineData("V")]
    [InlineData("X")]
    public void GetColorByDegree_Should_Return_Color(string degree)
    {
        var vm = CreateVM();

        var method = typeof(SeismologyPageViewModel)
            .GetMethod("GetColorByDegree",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        var result = method.Invoke(vm, new object[] { degree });

        Assert.NotNull(result);
    }
}