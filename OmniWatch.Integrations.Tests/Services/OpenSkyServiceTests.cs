using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Tests.Handlers;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Integrations.Tests.Services
{
    public class OpenSkyServiceTests
    {
        private readonly Mock<IHttpClientFactory> _factoryMock;
        private readonly Mock<IOpenSkyTokenManager> _tokenMock;
        private readonly Mock<ISettingsService> _settingsMock;

        public OpenSkyServiceTests()
        {
            _factoryMock = new Mock<IHttpClientFactory>();
            _tokenMock = new Mock<IOpenSkyTokenManager>();
            _settingsMock = new Mock<ISettingsService>();
        }

        private OpenSkyService CreateService(HttpClient client)
        {
            _factoryMock
                .Setup(f => f.CreateClient(ApiType.OpenSky.ToString()))
                .Returns(client);

            return new OpenSkyService(
                _factoryMock.Object,
                _tokenMock.Object,
                _settingsMock.Object);
        }

        // =========================
        // PUBLIC MODE
        // =========================
        [Fact]
        public async Task Should_Call_Public_Endpoint_Without_Token()
        {
            var json = "{\"time\":123,\"states\":[]}";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            _settingsMock.Setup(s => s.Load())
                .Returns(new AppSettings { UseOpenSkyCredentials = false });

            var service = CreateService(client);

            var result = await service.GetAllFlightStatesAsync();

            Assert.NotNull(result.Data);
            Assert.Null(result.RateLimit);
            Assert.Null(handler.LastRequest.Headers.Authorization);
        }

        // =========================
        // AUTH MODE
        // =========================
        [Fact]
        public async Task Should_Use_Token_When_Auth_Mode()
        {
            var json = "{\"time\":123,\"states\":[]}";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            _settingsMock.Setup(s => s.Load())
                .Returns(new AppSettings { UseOpenSkyCredentials = true });

            _tokenMock.Setup(t => t.GetTokenAsync())
                .ReturnsAsync("abc123");

            _tokenMock.Setup(t => t.GetRoleAsync())
                .ReturnsAsync("basic");

            var service = CreateService(client);

            var result = await service.GetAllFlightStatesAsync();

            Assert.NotNull(handler.LastRequest.Headers.Authorization);
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
            Assert.Equal("abc123", handler.LastRequest.Headers.Authorization.Parameter);

            Assert.NotNull(result.RateLimit);
        }

        // =========================
        // ERROR HANDLING
        // =========================
        [Fact]
        public async Task Should_Throw_ApiException_On_Http_Error()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            _settingsMock.Setup(s => s.Load())
                .Returns(new AppSettings { UseOpenSkyCredentials = false });

            var service = CreateService(client);

            await Assert.ThrowsAsync<ApiException>(() =>
                service.GetAllFlightStatesAsync());
        }

        // =========================
        // PARSING
        // =========================
        [Fact]
        public async Task Should_Parse_Response_Correctly()
        {
            var json = @"{
                ""time"": 123,
                ""states"": [
                    [
                        ""abc"",
                        ""CALL"",
                        ""PT"",
                        1,
                        2,
                        3.5,
                        4.5,
                        5.0,
                        false,
                        0,
                        null,
                        null,
                        null,
                        null,
                        false
                    ]
                ]
            }";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            _settingsMock.Setup(s => s.Load())
                .Returns(new AppSettings { UseOpenSkyCredentials = false });

            var service = CreateService(client);

            var result = await service.GetAllFlightStatesAsync();

            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.Time);
            Assert.NotEmpty(result.Data.States);
        }
    }
}