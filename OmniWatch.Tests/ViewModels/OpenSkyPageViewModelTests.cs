using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Models.OpenSky;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Tests.ViewModels;

public class OpenSkyPageViewModelTests
{
    private readonly Mock<IOpenSkyService> _api = new();
    private readonly Mock<IMessageService> _message = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IOpenSkyTokenManager> _token = new();
    private readonly Mock<IGlobalProgressService> _globalProgress = new();

    private OpenSkyPageViewModel CreateVM()
    {
        _settings.Setup(x => x.Load())
            .Returns(new AppSettings
            {
                RefreshInterval = 10,
                UseOpenSkyCredentials = false
            });

        return new OpenSkyPageViewModel(
            new ProgressControlViewModel(_globalProgress.Object),
            _api.Object,
            _message.Object,
            _settings.Object,
            _token.Object
        );
    }

    // =========================
    // LoadAsync
    // =========================

    [Fact]
    public async Task LoadAsync_Should_Call_API()
    {
        _api.Setup(x => x.GetAllFlightStatesAsync())
            .ReturnsAsync((
                Data: new OpenSkyRawResponse
                {
                    States = new List<StateVectorItem>()
                },
                RateLimit: new RateLimitInfo
                {
                    Remaining = 10,
                    Limit = 100,
                    ResetAt = DateTime.UtcNow.AddMinutes(1)
                }
            ));

        var vm = CreateVM();

        await vm.LoadAsync();

        _api.Verify(x => x.GetAllFlightStatesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadAsync_Should_Show_Error_On_Exception()
    {
        _api.Setup(x => x.GetAllFlightStatesAsync())
            .ThrowsAsync(new Exception("boom"));

        var vm = CreateVM();

        await vm.LoadAsync();

        _message.Verify(x => x.ShowAsync(
            It.Is<string>(s => s.Contains("boom")),
            MessageDialogType.Error),
            Times.Once);
    }

    // =========================
    // UseRealData toggle
    // =========================

    [Fact]
    public async Task UseRealData_Should_Trigger_Reload()
    {
        _api.Setup(x => x.GetAllFlightStatesAsync())
            .ReturnsAsync((
                Data: new OpenSkyRawResponse
                {
                    States = new List<StateVectorItem>()
                },
                RateLimit: new RateLimitInfo
                {
                    Remaining = 10,
                    Limit = 100,
                    ResetAt = DateTime.UtcNow.AddMinutes(1)
                }
            ));

        var vm = CreateVM();

        await vm.LoadAsync(); // garante estado inicial

        vm.UseRealData = true;

        await Task.Delay(50); // apenas buffer mínimo

        _api.Verify(x => x.GetAllFlightStatesAsync(), Times.AtLeastOnce);
    }

    // =========================
    // Rate limit logic
    // =========================

    [Fact]
    public async Task LoadAllFlightStates_Should_Set_RateLimit()
    {
        _api.Setup(x => x.GetAllFlightStatesAsync())
            .ReturnsAsync((
                Data: new OpenSkyRawResponse
                {
                    States = new List<StateVectorItem>()
                },
                RateLimit: new RateLimitInfo
                {
                    Remaining = 5,
                    Limit = 100,
                    ResetAt = DateTime.UtcNow.AddMinutes(5)
                }
            ));

        var vm = CreateVM();

        await vm.LoadAllFlightStatesAsync();

        Assert.Equal(5, vm.RateLimitRemaining);
        Assert.Equal(100, vm.RateLimitTotal);
    }

    // =========================
    // Dummy mode
    // =========================

    [Fact]
    public async Task ReloadAircraft_Should_Not_Fail_In_Dummy_Mode()
    {
        var vm = CreateVM();

        vm.UseRealData = false;

        await vm.LoadAsync();

        Assert.NotNull(vm.Map);
    }

    // =========================
    // Stop cancellation
    // =========================

    [Fact]
    public void Stop_Should_Not_Throw()
    {
        var vm = CreateVM();

        vm.Stop();

        Assert.True(true);
    }

    [Fact]
    public async Task UnloadAsync_Should_Cancel_And_Clear_CancellationToken()
    {
        var vm = CreateVM();

        // Força o início do loop
        vm.UseRealData = true;
        await vm.LoadAsync();

        // Act
        await vm.UnloadAsync();

        // Assert
        var cts = GetPrivateCts(vm);
        Assert.Null(cts);
    }

    private CancellationTokenSource? GetPrivateCts(OpenSkyPageViewModel vm)
    {
        var field = typeof(OpenSkyPageViewModel)
            .GetField("_cts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (CancellationTokenSource?)field?.GetValue(vm);
    }

}