using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Integrations.Contracts.Weather;
using OmniWatch.Integrations.Contracts.Forecast;
using OmniWatch.Integrations.Contracts.Wind;
using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Integrations.Contracts.Awarness;
using OmniWatch.Integrations.Contracts.Precipitation;

namespace OmniWatch.Integrations.Tests.Services
{
    public class IpmaServiceTests
    {
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly IpmaService _service;

        public IpmaServiceTests()
        {
            _apiClientMock = new Mock<IApiClient>();
            _service = new IpmaService(_apiClientMock.Object);
        }

        [Fact]
        public async Task GetLocationsAsync_Should_Call_Correct_Endpoint()
        {
            _apiClientMock
                .Setup(x => x.GetAsync<LocationsResponse>("distrits-islands.json", ApiType.Ipma, null))
                .ReturnsAsync(new LocationsResponse());

            var result = await _service.GetLocationsAsync();

            Assert.NotNull(result);

            _apiClientMock.Verify(x =>
                x.GetAsync<LocationsResponse>("distrits-islands.json", ApiType.Ipma, null),
                Times.Once);
        }

        [Fact]
        public async Task GetForecastByCityAsync_Should_Call_Correct_Endpoint()
        {
            int cityId = 123;

            _apiClientMock
                .Setup(x => x.GetAsync<ForecastResponse>(
                    $"forecast/meteorology/cities/daily/{cityId}.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new ForecastResponse());

            var result = await _service.GetForecastByCityAsync(cityId);

            Assert.NotNull(result);

            _apiClientMock.Verify(x =>
                x.GetAsync<ForecastResponse>(
                    $"forecast/meteorology/cities/daily/{cityId}.json",
                    ApiType.Ipma,
                    null),
                Times.Once);
        }

        [Fact]
        public async Task GetForecastByDayAsync_Should_Call_Correct_Endpoint()
        {
            int day = 2;

            _apiClientMock
                .Setup(x => x.GetAsync<ForecastByDayResponse>(
                    $"forecast/meteorology/cities/daily/hp-daily-forecast-day{day}.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new ForecastByDayResponse());

            var result = await _service.GetForecastByDayAsync(day);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetWindAsync_Should_Call_Correct_Endpoint()
        {
            _apiClientMock
                .Setup(x => x.GetAsync<WindSpeedResponse>(
                    "wind-speed-daily-classe.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new WindSpeedResponse());

            var result = await _service.GetWindAsync();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetSeismicAsync_Should_Call_Correct_Endpoint()
        {
            int areaId = 5;

            _apiClientMock
                .Setup(x => x.GetAsync<SeismicResponse>(
                    $"observation/seismic/{areaId}.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new SeismicResponse());

            var result = await _service.GetSeismicAsync(areaId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAwarnessAsync_Should_Return_List()
        {
            _apiClientMock
                .Setup(x => x.GetAsync<List<AwarenessItem>>(
                    "forecast/warnings/warnings_www.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new List<AwarenessItem>());

            var result = await _service.GetAwarnessAsync();

            Assert.NotNull(result);
            Assert.IsType<List<AwarenessItem>>(result);
        }

        [Fact]
        public async Task GetPrecipitationTypesAsync_Should_Call_Correct_Endpoint()
        {
            _apiClientMock
                .Setup(x => x.GetAsync<PrecipitationResponse>(
                    "precipitation-classe.json",
                    ApiType.Ipma,
                    null))
                .ReturnsAsync(new PrecipitationResponse());

            var result = await _service.GetPrecipitationTypesAsync();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Should_Throw_ApiException_When_ApiClient_Fails()
        {
            _apiClientMock
                .Setup(x => x.GetAsync<LocationsResponse>(It.IsAny<string>(), ApiType.Ipma, null))
                .ThrowsAsync(new Exception("boom"));

            await Assert.ThrowsAsync<ApiException>(() => _service.GetLocationsAsync());
        }
    }
}