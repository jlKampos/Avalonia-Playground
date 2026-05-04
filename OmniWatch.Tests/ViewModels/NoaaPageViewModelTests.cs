using Microsoft.Extensions.Logging;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Models.Noaa.ActiveStorms;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Tests.ViewModels
{
    public class NoaaPageViewModelTests
    {
        private readonly Mock<INoaaService> _noaaServiceMock = new();
        private readonly Mock<IMessageService> _messageServiceMock = new();
        private readonly Mock<ILogger<NoaaPageViewModel>> _loggerMock = new();
        private readonly Mock<IGlobalProgressService> _progressServiceMock = new();

        private readonly ProgressControlViewModel _progressControl;
        private readonly NoaaPageViewModel _viewModel;

        public NoaaPageViewModelTests()
        {
            _progressControl = new ProgressControlViewModel(_progressServiceMock.Object);

            _viewModel = new NoaaPageViewModel(
                _noaaServiceMock.Object,
                _messageServiceMock.Object,
                _loggerMock.Object,
                _progressControl
            );
        }

        // =========================================================
        // LoadAsync - Success path
        // =========================================================
        [Fact]
        public async Task LoadAsync_ShouldPopulateHurricanes_WhenServiceReturnsData()
        {
            _noaaServiceMock
                .Setup(s => s.GetActiveStormTracksAsync())
                .ReturnsAsync(new NhcActiveStormResponse
                {
                    ActiveStorms = new List<ActiveStormItem>()
                });

            _noaaServiceMock
                .Setup(s => s.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>
                {
                    new() { Id = "AL012024", Name = "ALBERTO" }
                });

            await _viewModel.LoadAsync();

            Assert.NotNull(_viewModel.Hurricanes);
            Assert.Single(_viewModel.Hurricanes);
            Assert.Equal("ALBERTO", _viewModel.Hurricanes.First().Name);
        }

        // =========================================================
        // LoadAsync - Service failure
        // =========================================================
        [Fact]
        public async Task LoadAsync_ShouldShowError_WhenServiceThrowsException()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            _noaaServiceMock
                .Setup(s => s.GetActiveStormTracksAsync())
                .ThrowsAsync(new Exception("API Offline"));

            _noaaServiceMock
                .Setup(s => s.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>());

            _messageServiceMock
                .Setup(m => m.ShowAsync(
                    It.IsAny<string>(),
                    It.IsAny<MessageDialogBoxViewModel.MessageDialogType>()))
                .ReturnsAsync(MessageDialogResult.Ok);

            // Act
            await _viewModel.LoadAsync();

            // Assert
            _messageServiceMock.Verify(m => m.ShowAsync(
                It.Is<string>(msg => msg.Contains("API Offline")),
                It.IsAny<MessageDialogBoxViewModel.MessageDialogType>()),
                Times.Once);
        }

        // =========================================================
        // Empty historical data
        // =========================================================
        [Fact]
        public async Task LoadAsync_ShouldHandleEmptyHistoricalData()
        {
            _noaaServiceMock
                .Setup(s => s.GetActiveStormTracksAsync())
                .ReturnsAsync(new NhcActiveStormResponse
                {
                    ActiveStorms = new List<ActiveStormItem>()
                });

            _noaaServiceMock
                .Setup(s => s.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>());

            await _viewModel.LoadAsync();

            Assert.Null(_viewModel.Hurricanes);
        }

        // =========================================================
        // Progress updates
        // =========================================================
        [Fact]
        public void ProgressControl_ShouldUpdate_WhenEventIsRaised()
        {
            var message = "Downloading data...";

            _progressServiceMock.Raise(p => p.ProgressChanged += null, message);

            Assert.Equal(message, _progressControl.Message);
        }

        // =========================================================
        // Theme toggle
        // =========================================================
        [Fact]
        public void IsDarkTheme_ShouldChangeProperty()
        {
            var initial = _viewModel.IsDarkTheme;

            _viewModel.IsDarkTheme = !initial;

            Assert.Equal(!initial, _viewModel.IsDarkTheme);
        }

        // =========================================================
        // Unload safety
        // =========================================================
        [Fact]
        public async Task UnloadAsync_ShouldExecuteWithoutErrors()
        {
            await _viewModel.UnloadAsync();
        }

        // =========================================================
        // Year change safety
        // =========================================================
        [Fact]
        public async Task SelectedYearChange_ShouldNotThrow()
        {
            _noaaServiceMock
                .Setup(s => s.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>
                {
                    new() { Id = "TEST", Name = "TEST" }
                });

            await _viewModel.LoadAsync();

            _viewModel.SelectedYear = 2005;

            await Task.Delay(50);

            Assert.True(true);
        }

        // =========================================================
        // Reanimate setter
        // =========================================================
        [Fact]
        public void Reanimate_Setter_ShouldNotThrow()
        {
            _viewModel.Reanimate = true;

            Assert.True(_viewModel.Reanimate);
        }
    }
}