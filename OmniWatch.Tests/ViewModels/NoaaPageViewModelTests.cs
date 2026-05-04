using Microsoft.Extensions.Logging;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
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
        private readonly Mock<INoaaService> _noaa = new();
        private readonly Mock<IMessageService> _msg = new();
        private readonly Mock<ILogger<NoaaPageViewModel>> _logger = new();
        private readonly Mock<IGlobalProgressService> _progress = new();

        private readonly ProgressControlViewModel _progressVm;
        private readonly NoaaPageViewModel _vm;

        public NoaaPageViewModelTests()
        {
            _progressVm = new ProgressControlViewModel(_progress.Object);

            _vm = new NoaaPageViewModel(
                _noaa.Object,
                _msg.Object,
                _logger.Object,
                _progressVm
            );
        }

        // =========================
        // SUCCESS
        // =========================
        [Fact]
        public async Task LoadAsync_ShouldPopulateHurricanes_WhenServiceReturnsData()
        {
            _noaa
                .Setup(s => s.GetActiveStormTracksAsync())
                .ReturnsAsync(new NhcActiveStormResponse
                {
                    ActiveStorms = new List<ActiveStormItem>
                    {
                        new ActiveStormItem
                        {
                            Id = "AL012024",
                            Name = "ALBERTO"
                        }
                    }
                });

            _noaa
                .Setup(s => s.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>
                {
                    new StormTrack
                    {
                        Id = "AL012024",
                        Name = "ALBERTO",
                        Season = 2024,
                        Track = new List<StormTrackPointItem>
                        {
                            new StormTrackPointItem
                            {
                                Time = DateTime.UtcNow,
                                Latitude = 10,
                                Longitude = 20
                            }
                        }
                    }
                });

            await _vm.LoadAsync();

            Assert.NotNull(_vm.Hurricanes);
            Assert.Single(_vm.Hurricanes);
            Assert.Equal("ALBERTO", _vm.Hurricanes.First().Name);
        }

        // =========================
        // ERROR
        // =========================
        [Fact]
        public async Task LoadAsync_ShouldShowError_WhenServiceFails()
        {
            _noaa
                .Setup(x => x.GetActiveStormTracksAsync())
                .ThrowsAsync(new Exception("API Offline"));

            _noaa
                .Setup(x => x.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>());

            _msg
                .Setup(x => x.ShowAsync(
                    It.IsAny<string>(),
                    It.IsAny<MessageDialogBoxViewModel.MessageDialogType>()))
                .ReturnsAsync(MessageDialogResult.Ok);

            await _vm.LoadAsync();

            _msg.Verify(x => x.ShowAsync(
                It.Is<string>(m => m.Contains("API Offline")),
                It.IsAny<MessageDialogBoxViewModel.MessageDialogType>()),
                Times.Once);
        }

        // =========================
        // EMPTY
        // =========================
        [Fact]
        public async Task LoadAsync_ShouldHandleEmptyData()
        {
            _noaa
                .Setup(x => x.GetActiveStormTracksAsync())
                .ReturnsAsync(new NhcActiveStormResponse());

            _noaa
                .Setup(x => x.GetHistoricalStormTracksAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StormTrack>());

            await _vm.LoadAsync();

            Assert.Null(_vm.Hurricanes);
        }

        // =========================
        // PROGRESS
        // =========================
        [Fact]
        public void ProgressControl_ShouldUpdate_WhenServiceRaisesEvent()
        {
            var service = new Mock<IGlobalProgressService>();
            var vm = new ProgressControlViewModel(service.Object);

            var message = "Downloading data...";

            service.Raise(s => s.ProgressChanged += null, message);

            Assert.Equal(message, vm.Message);
        }

        // =========================
        // UI SAFETY
        // =========================
        [Fact]
        public async Task Unload_ShouldNotThrow()
        {
            await _vm.UnloadAsync();
        }

        [Fact]
        public void Theme_ShouldToggle()
        {
            var old = _vm.IsDarkTheme;

            _vm.IsDarkTheme = !old;

            Assert.Equal(!old, _vm.IsDarkTheme);
        }

        [Fact]
        public void Reanimate_ShouldNotCrash()
        {
            _vm.Reanimate = true;

            Assert.True(_vm.Reanimate);
        }
    }
}