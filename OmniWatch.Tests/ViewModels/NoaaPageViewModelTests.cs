using Microsoft.Extensions.Logging;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels;
using OmniWatch.ViewModels.ProgressControl;

namespace OmniWatch.Tests.ViewModels
{
    public class NoaaPageViewModelTests
    {
        private readonly Mock<INoaaService> _noaaServiceMock;
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<ILogger<NoaaPageViewModel>> _loggerMock;
        private readonly Mock<IGlobalProgressService> _progressServiceMock;
        private readonly ProgressControlViewModel _progressControl;
        private readonly NoaaPageViewModel _viewModel;

        public NoaaPageViewModelTests()
        {
            _noaaServiceMock = new Mock<INoaaService>();
            _messageServiceMock = new Mock<IMessageService>();
            _loggerMock = new Mock<ILogger<NoaaPageViewModel>>();
            _progressServiceMock = new Mock<IGlobalProgressService>();

            _progressControl = new ProgressControlViewModel(_progressServiceMock.Object);

            _viewModel = new NoaaPageViewModel(
                _noaaServiceMock.Object,
                _messageServiceMock.Object,
                _loggerMock.Object,
                _progressControl
            );
        }

        [Fact]
        public async Task LoadAsync_ShouldPopulateHurricanes_WhenDataIsReceived()
        {
            // Arrange
            var year = 2024;
            _viewModel.SelectedYear = year;
            var mockData = new List<StormTrack>
            {
                new() { Id = "AL012024", Name = "ALBERTO" }
            };

            _noaaServiceMock.Setup(s => s.GetHistoricalStormTracksAsync(
                year,
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<string>>()))
                .ReturnsAsync(mockData);

            // Act
            await _viewModel.LoadAsync();

            // Assert
            Assert.NotNull(_viewModel.Hurricanes);
            Assert.Single(_viewModel.Hurricanes);
            Assert.Equal("ALBERTO", _viewModel.Hurricanes.First().Name);
        }

        [Fact]
        public void IsDarkTheme_Toggle_ShouldChangeBaseLayer()
        {
            // Arrange
            _viewModel.IsDarkTheme = true;

            // Act
            _viewModel.IsDarkTheme = false;

            // Assert
            var currentLayer = _viewModel.Map.Layers.ElementAt(0);
            Assert.NotNull(currentLayer);
            Assert.False(_viewModel.IsDarkTheme);
        }

        [Fact]
        public async Task LoadAsync_ShouldShowError_WhenServiceFails()
        {
            // Arrange
            var errorMessage = "API Offline";
            _noaaServiceMock.Setup(s => s.GetHistoricalStormTracksAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<string>>()))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            await _viewModel.LoadAsync();

            // Assert
            _messageServiceMock.Verify(m => m.ShowAsync(
                It.Is<string>(s => s.Contains(errorMessage)),
                It.Is<OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel.MessageDialogType>(
                    t => t == OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel.MessageDialogType.Error)),
                Times.Once);
        }

        [Fact]
        public void ProgressControl_ShouldUpdateMessage_WhenEventFires()
        {
            // Arrange
            var testMessage = "Downloading coordinates...";

            // Act
            _progressServiceMock.Raise(m => m.ProgressChanged += null, testMessage);

            // Assert
            Assert.Equal(testMessage, _progressControl.Message);
        }

        [Fact]
        public void UnloadAsync_ShouldStopAnimationAndClearLayers()
        {
            // Act
            _viewModel.UnloadAsync();

            // Assert
            var stormLayersCount = _viewModel.Map.Layers.Count(l => l.Name.StartsWith("Storm"));
            Assert.Equal(0, stormLayersCount);
        }
    }
}